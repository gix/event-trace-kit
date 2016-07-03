#include "EtwMessageFormatter.h"

#include "ADT/ArrayRef.h"
#include <evntcons.h>
#include <Tdh.h>
#include <in6addr.h>

namespace etk
{

namespace
{

template<typename T, typename U>
ETK_ALWAYS_INLINE
T GetAt(U* ptr, size_t offset)
{
    return reinterpret_cast<T>(reinterpret_cast<uint8_t*>(ptr) + offset);
}

// The mapped string values defined in a manifest will contain a trailing space
// in the EVENT_MAP_ENTRY structure. Replace the trailing space with a null-
// terminating character, so that the bit mapped strings are correctly formatted.
void RemoveTrailingSpace(EVENT_MAP_INFO* mapInfo)
{
    for (ULONG i = 0; i < mapInfo->EntryCount; ++i) {
        EVENT_MAP_ENTRY const& entry = mapInfo->MapEntryArray[i];
        wchar_t* str = GetAt<wchar_t*>(mapInfo, entry.OutputOffset);
        str[wcslen(str) - 1] = L'\0';
    }
}

template<typename T>
DWORD GetProperty(EVENT_RECORD* record, PROPERTY_DATA_DESCRIPTOR& pdd, T& value)
{
    DWORD propertySize = 0;
    DWORD ec = TdhGetPropertySize(record, 0, nullptr, 1, &pdd, &propertySize);
    if (ec != ERROR_SUCCESS)
        return ec;

    if (propertySize > sizeof(value))
        return ERROR_INSUFFICIENT_BUFFER;

    return TdhGetProperty(record, 0, nullptr, 1, &pdd, sizeof(value),
                          reinterpret_cast<PBYTE>(&value));
}

DWORD GetProperty(EventInfo info, EVENT_PROPERTY_INFO const& property, USHORT& value)
{
    PROPERTY_DATA_DESCRIPTOR pdd = {};
    if (!info.TryGetAt(property.NameOffset, pdd.PropertyName))
        return ERROR_EVT_INVALID_EVENT_DATA;
    pdd.ArrayIndex = ULONG_MAX;

    DWORD count = 0; // Expects the count to be defined by a UINT16 or UINT32
    DWORD ec = GetProperty(info.Record(), pdd, count);
    if (ec != ERROR_SUCCESS) {
        value = 0;
        return ec;
    }

    value = static_cast<USHORT>(count);
    return ec;
}

// Get the size of the array. For MOF-based events, the size is specified in the declaration or using
// the MAX qualifier. For manifest-based events, the property can specify the size of the array
// using the count attribute. The count attribute can specify the size directly or specify the name
// of another property in the event data that contains the size.
DWORD GetArraySize(EventInfo info, EVENT_PROPERTY_INFO const& propInfo,
                   USHORT* arraySize)
{
    if ((propInfo.Flags & PropertyParamCount) == 0) {
        *arraySize = propInfo.count;
        return ERROR_SUCCESS;
    }

    EVENT_PROPERTY_INFO const& paramInfo =
        info->EventPropertyInfoArray[propInfo.countPropertyIndex];
    return GetProperty(info, paramInfo, *arraySize);

    PROPERTY_DATA_DESCRIPTOR pdd = {};
    if (!info.TryGetAt(paramInfo.NameOffset, pdd.PropertyName))
        return ERROR_EVT_INVALID_EVENT_DATA;
    pdd.ArrayIndex = ULONG_MAX;

    DWORD count = 0; // Expects the count to be defined by a UINT16 or UINT32
    DWORD ec = GetProperty(info.Record(), pdd, count);
    if (ec != ERROR_SUCCESS) {
        *arraySize = 0;
        return ec;
    }

    *arraySize = static_cast<USHORT>(count);

    return ec;
}

// Both MOF-based events and manifest-based events can specify name/value maps. The
// map values can be integer values or bit values. If the property specifies a value
// map, get the map.
DWORD GetEventMapInfo(EVENT_RECORD* event, LPWSTR mapName, DWORD decodingSource,
                      vstruct_ptr<EVENT_MAP_INFO>& mapInfo)
{
    // Retrieve the required buffer size for the map info.
    DWORD bufferSize = 0;
    DWORD ec = TdhGetEventMapInformation(event, mapName, nullptr, &bufferSize);

    if (ec == ERROR_INSUFFICIENT_BUFFER) {
        mapInfo = make_vstruct<EVENT_MAP_INFO>(bufferSize);
        ec = TdhGetEventMapInformation(event, mapName, mapInfo.get(), &bufferSize);
    }

    if (ec == ERROR_SUCCESS) {
        if (decodingSource == DecodingSourceXMLFile)
            RemoveTrailingSpace(mapInfo.get());
    } else if (ec == ERROR_NOT_FOUND) {
        ec = ERROR_SUCCESS; // This case is okay.
    }

    return ec;
}

// Get the length of the property data. For MOF-based events, the size is inferred from the data type
// of the property. For manifest-based events, the property can specify the size of the property value
// using the length attribute. The length attribute can specify the size directly or specify the name
// of another property in the event data that contains the size. If the property does not include the
// length attribute, the size is inferred from the data type. The length will be zero for variable
// length, null-terminated strings and structures.
DWORD GetPropertyLength(EventInfo info, EVENT_PROPERTY_INFO const& propInfo,
                        USHORT* propertyLength)
{
    // If the property is a binary blob and is defined in a manifest, the
    // property can specify the blob's size or it can point to another property
    // that defines the  blob's size. The PropertyParamLength flag indicates
    // where the blob's size is defined.
    if ((propInfo.Flags & PropertyParamLength) == PropertyParamLength) {
        auto const& lengthProperty =
            info->EventPropertyInfoArray[propInfo.lengthPropertyIndex];
        return GetProperty(info, lengthProperty, *propertyLength);

        PROPERTY_DATA_DESCRIPTOR pdd = {};
        if (!info.TryGetAt(lengthProperty.NameOffset, pdd.PropertyName))
            return ERROR_EVT_INVALID_EVENT_DATA;
        pdd.ArrayIndex = ULONG_MAX;

        DWORD length = 0; // Expects the length to be defined by a UINT16 or UINT32
        DWORD ec = GetProperty(info.Record(), pdd, length);
        if (ec == ERROR_SUCCESS)
            *propertyLength = static_cast<USHORT>(length);
        return ec;
    }

    if (propInfo.length > 0) {
        *propertyLength = propInfo.length;
        return ERROR_SUCCESS;
    }

    // If the property is a binary blob and is defined in a MOF class, the
    // extension qualifier is used to determine the size of the blob.
    // However, if the extension is IPAddrV6, we must determine the property
    // length because the EVENT_PROPERTY_INFO.length field will be zero.

    if (propInfo.nonStructType.InType == TDH_INTYPE_BINARY &&
        propInfo.nonStructType.OutType == TDH_OUTTYPE_IPV6) {
        *propertyLength = static_cast<USHORT>(sizeof(IN6_ADDR));
    } else if (propInfo.nonStructType.InType == TDH_INTYPE_UNICODESTRING ||
               propInfo.nonStructType.InType == TDH_INTYPE_ANSISTRING ||
               (propInfo.Flags & PropertyStruct) == PropertyStruct) {
        *propertyLength = propInfo.length;
    } else {
        wprintf(L"Unexpected length of 0 for intype %d and outtype %d\n",
                propInfo.nonStructType.InType,
                propInfo.nonStructType.OutType);

        return ERROR_EVT_INVALID_EVENT_DATA;
    }

    return ERROR_SUCCESS;
}

DWORD FormatProperty(
    EventInfo info, EVENT_PROPERTY_INFO const& propInfo,
    size_t pointerSize, ArrayRef<uint8_t>& userData, std::wstring& sink,
    std::vector<wchar_t>& buffer)
{
    DWORD ec = ERROR_SUCCESS;

    USHORT propertyLength = 0;
    ec = GetPropertyLength(info, propInfo, &propertyLength);
    if (ec != ERROR_SUCCESS)
        return ec;

    // Get the size of the array if the property is an array.
    USHORT arraySize = 0;
    ec = GetArraySize(info, propInfo, &arraySize);
    if (ec != ERROR_SUCCESS)
        return ec;

    for (USHORT k = 0; k < arraySize; ++k) {
        // If the property is a structure, print the members of the structure.
        if ((propInfo.Flags & PropertyStruct) == PropertyStruct) {
            DWORD lastMember = propInfo.structType.StructStartIndex +
                propInfo.structType.NumOfStructMembers;

            for (USHORT j = propInfo.structType.StructStartIndex; j < lastMember; ++j) {
                EVENT_PROPERTY_INFO const& pi = info->EventPropertyInfoArray[j];
                ec = FormatProperty(info, pi, pointerSize, userData, sink, buffer);
                if (ec != ERROR_SUCCESS)
                    return ec;
            }

            continue;
        }

        // Get the name/value mapping if the property specifies a value map.
        vstruct_ptr<EVENT_MAP_INFO> mapInfo;
        if (propInfo.nonStructType.MapNameOffset != 0) {
            ec = GetEventMapInfo(info.Record(),
                            GetAt<PWCHAR>(info.Info(), propInfo.nonStructType.MapNameOffset),
                            info->DecodingSource,
                            mapInfo);
            if (ec != ERROR_SUCCESS)
                return ec;
        }

        DWORD bufferSize = 0;
        USHORT userDataConsumed = 0;

        bufferSize = static_cast<DWORD>(buffer.size());
        ec = TdhFormatProperty(
            info.Info(),
            mapInfo.get(),
            static_cast<ULONG>(pointerSize),
            propInfo.nonStructType.InType,
            propInfo.nonStructType.OutType,
            propertyLength,
            static_cast<USHORT>(userData.size()),
            const_cast<BYTE*>(userData.data()),
            &bufferSize,
            buffer.data(),
            &userDataConsumed);

        if (ec == ERROR_INSUFFICIENT_BUFFER) {
            buffer.resize(bufferSize);
            bufferSize = static_cast<DWORD>(buffer.size());
            ec = TdhFormatProperty(
                info.Info(),
                mapInfo.get(),
                static_cast<ULONG>(pointerSize),
                propInfo.nonStructType.InType,
                propInfo.nonStructType.OutType,
                propertyLength,
                static_cast<USHORT>(userData.size()),
                const_cast<BYTE*>(userData.data()),
                &bufferSize,
                buffer.data(),
                &userDataConsumed);
        }

        if (ec != ERROR_SUCCESS)
            return ec;

        userData.remove_prefix(userDataConsumed);
        sink.append(buffer.data());
    }

    return ec;
}

} // namespace

bool EtwMessageFormatter::FormatEventMessage(
    EventInfo info, size_t pointerSize, wchar_t* buffer, size_t bufferSize)
{
    if (!info)
        return false;

    formattedProperties.clear();
    formattedPropertiesOffsets.clear();
    formattedPropertiesPointers.clear();

    ArrayRef<uint8_t> userData = info.UserData();
    DWORD ec;
    for (ULONG i = 0; i < info->TopLevelPropertyCount; ++i) {
        auto const& pi = info->EventPropertyInfoArray[i];
        size_t begin = formattedProperties.size();
        ec = FormatProperty(info, pi, pointerSize, userData, formattedProperties, propertyBuffer);
        if (ec != ERROR_SUCCESS)
            return false;

        formattedProperties.push_back(0);
        formattedPropertiesOffsets.push_back(begin);
    }

    for (auto const& begin : formattedPropertiesOffsets)
        formattedPropertiesPointers.push_back(
            reinterpret_cast<DWORD_PTR>(&formattedProperties[begin]));

    auto const Flags = FORMAT_MESSAGE_FROM_STRING | FORMAT_MESSAGE_ARGUMENT_ARRAY;
    DWORD numWritten = FormatMessageW(
        Flags, info.EventMessage(), 0, 0, buffer, bufferSize,
        reinterpret_cast<va_list*>(formattedPropertiesPointers.data()));

    if (numWritten == 0)
        return false;

    return true;

#if 0
    wchar_t const* ptr = info.EventMessage();
    while (ptr) {
        auto begin = ptr;
        while (*ptr && *ptr != L'%')
            ++ptr;
        if (ptr != begin)
            sink.append(begin, ptr - begin);

        if (!*ptr)
            break;

        ++ptr; // Skip %
        if (*ptr == L'n') {
            ++ptr;
            sink += L'\n';
            continue;
        }

        begin = ptr;
        int index = 0;
        while (*ptr && *ptr >= L'0' && *ptr <= L'9') {
            if (index >= 255)
                break; // FIXME
            index = (index * 10) + (*ptr - L'0');
            ++ptr;
        }

        if (ptr == begin) {
            // Invalid char after %, ignore.
            ++ptr;
            sink += L'%';
            sink += *ptr;
            continue;
        }

        if (index < 1 || static_cast<unsigned>(index) > info->TopLevelPropertyCount) {
            sink.append(begin, ptr - begin);
            continue;
        }

        sink.append(formattedProperties,
                    formattedPropertiesOffsets[index - 1],
                    formattedPropertiesOffsets[index] - formattedPropertiesOffsets[index - 1]);
    }

    return true;
#endif
}

} // namespace etk
