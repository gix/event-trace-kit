#include "TdhMessageFormatter.h"

#include "ADT/Span.h"
#include "Support/ErrorHandling.h"
#include <evntcons.h>
#include <in6addr.h>
#include <strsafe.h>
#include <tdh.h>

namespace etk
{

namespace
{

template<typename T, typename U>
ETK_ALWAYS_INLINE T GetAt(U* ptr, size_t offset)
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
ULONG GetProperty(EVENT_RECORD const* record, PROPERTY_DATA_DESCRIPTOR& pdd, T& value)
{
    return TdhGetProperty(const_cast<EVENT_RECORD*>(record), 0, nullptr, 1, &pdd,
                          sizeof(value), reinterpret_cast<PBYTE>(&value));
}

ULONG GetProperty(EventInfo info, EVENT_PROPERTY_INFO const& property, USHORT& value)
{
    PROPERTY_DATA_DESCRIPTOR pdd = {};
    if (!info.TryGetAt(property.NameOffset, pdd.PropertyName))
        return ERROR_EVT_INVALID_EVENT_DATA;
    pdd.ArrayIndex = ULONG_MAX;

    DWORD count = 0; // Expects the count to be defined by a UINT16 or UINT32
    ULONG ec = GetProperty(info.Record(), pdd, count);
    if (ec != ERROR_SUCCESS) {
        value = 0;
        return ec;
    }

    value = static_cast<USHORT>(count);
    return ec;
}

// Get the length of the property data. For MOF-based events, the size is
// inferred from the data type of the property. For manifest-based events, the
// property can specify the size of the property value using the length
// attribute. The length attribute can specify the size directly or specify the
// name of another property in the event data that contains the size. If the
// property does not include the length attribute, the size is inferred from the
// data type. The length will be zero for variable length, null-terminated
// strings and structures.
ULONG GetPropertyLength(EventInfo info, EVENT_PROPERTY_INFO const& propInfo,
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
                propInfo.nonStructType.InType, propInfo.nonStructType.OutType);

        return ERROR_EVT_INVALID_EVENT_DATA;
    }

    return ERROR_SUCCESS;
}

// Gets the size of the array. For MOF-based events, the size is specified in
// the declaration or using the MAX qualifier. For manifest-based events, the
// property can specify the size of the array using the count attribute. The
// count attribute can specify the size directly or specify the name of another
// property in the event data that contains the size.
ULONG GetArraySize(EventInfo info, EVENT_PROPERTY_INFO const& propInfo, USHORT* arraySize)
{
    if ((propInfo.Flags & PropertyParamCount) == 0) {
        *arraySize = propInfo.count;
        return ERROR_SUCCESS;
    }

    EVENT_PROPERTY_INFO const& paramInfo =
        info->EventPropertyInfoArray[propInfo.countPropertyIndex];
    return GetProperty(info, paramInfo, *arraySize);
}

// Both MOF-based events and manifest-based events can specify name/value maps.
// The map values can be integer values or bit values. If the property specifies
// a value map, get the map.
ULONG GetEventMapInfo(EVENT_RECORD const* event, wchar_t const* mapName,
                      DWORD decodingSource, EVENT_MAP_INFO*& mapInfo,
                      std::vector<BYTE>& mapBuffer)
{
    mapInfo = new (mapBuffer.data()) EVENT_MAP_INFO;

    // Retrieve the required buffer size for the map info.
    DWORD bufferSize = static_cast<DWORD>(mapBuffer.size());
    ULONG ec = TdhGetEventMapInformation(const_cast<EVENT_RECORD*>(event),
                                         const_cast<wchar_t*>(mapName),
                                         bufferSize ? mapInfo : nullptr, &bufferSize);

    if (ec == ERROR_INSUFFICIENT_BUFFER) {
        mapBuffer.resize(bufferSize);
        mapInfo = new (mapBuffer.data()) EVENT_MAP_INFO;
        ec = TdhGetEventMapInformation(const_cast<EVENT_RECORD*>(event),
                                       const_cast<wchar_t*>(mapName), mapInfo,
                                       &bufferSize);
    }

    if (ec == ERROR_SUCCESS) {
        if (decodingSource == DecodingSourceXMLFile)
            RemoveTrailingSpace(mapInfo);
    } else if (ec == ERROR_NOT_FOUND) {
        ec = ERROR_SUCCESS; // This case is okay.
    }

    return ec;
}

ULONG GetEventMapInfo(EventInfo info, EVENT_PROPERTY_INFO const& propertyInfo,
                      EVENT_MAP_INFO*& mapInfo, std::vector<BYTE>& mapBuffer)
{
    if (propertyInfo.nonStructType.MapNameOffset == 0)
        return ERROR_SUCCESS;

    wchar_t const* mapName;
    if (!info.TryGetAt(propertyInfo.nonStructType.MapNameOffset, mapName))
        return ERROR_EVT_INVALID_EVENT_DATA;

    ULONG ec =
        GetEventMapInfo(info.Record(), mapName, info->DecodingSource, mapInfo, mapBuffer);
    if (ec != ERROR_SUCCESS)
        return ec;

    return ERROR_SUCCESS;
}

ULONG FormatProperty(TRACE_EVENT_INFO const& tei, EVENT_MAP_INFO const& emi,
                     unsigned pointerSize, EVENT_PROPERTY_INFO const& pi,
                     USHORT propertyLength, cspan<std::byte> userData,
                     ULONG* bufferSize, wchar_t* buffer, USHORT* userDataConsumed)
{
    return TdhFormatProperty(
        const_cast<TRACE_EVENT_INFO*>(&tei), const_cast<EVENT_MAP_INFO*>(&emi),
        static_cast<ULONG>(pointerSize), pi.nonStructType.InType,
        pi.nonStructType.OutType, propertyLength, static_cast<USHORT>(userData.size()),
        const_cast<BYTE*>(reinterpret_cast<BYTE const*>(userData.data())), bufferSize,
        buffer, userDataConsumed);
}

ULONG FormatProperty(EventInfo info, EVENT_PROPERTY_INFO const& propInfo,
                     size_t pointerSize, cspan<std::byte>& userData,
                     std::wstring& sink, std::vector<wchar_t>& buffer,
                     std::vector<BYTE>& mapBuffer)
{
    ULONG ec;

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
                ec = FormatProperty(info, pi, pointerSize, userData, sink, buffer,
                                    mapBuffer);
                if (ec != ERROR_SUCCESS)
                    return ec;
            }

            continue;
        }

        // Get the name/value mapping if the property specifies a value map.
        EVENT_MAP_INFO* mapInfo = nullptr;
        if (propInfo.nonStructType.MapNameOffset != 0) {
            ec = GetEventMapInfo(info, propInfo, mapInfo, mapBuffer);
            if (ec != ERROR_SUCCESS)
                return ec;
        }

        DWORD bufferSize = 0;
        USHORT userDataConsumed = 0;

        bufferSize = static_cast<DWORD>(buffer.size() * sizeof(wchar_t));
        ec = FormatProperty(*info.Info(), *mapInfo, pointerSize, propInfo, propertyLength,
                            userData, &bufferSize, buffer.data(), &userDataConsumed);

        if (ec == ERROR_INSUFFICIENT_BUFFER) {
            buffer.resize(bufferSize / sizeof(wchar_t));
            ec = FormatProperty(*info.Info(), *mapInfo, pointerSize, propInfo,
                                propertyLength, userData, &bufferSize, buffer.data(),
                                &userDataConsumed);
        }

        if (ec != ERROR_SUCCESS)
            return ec;

        buffer.resize(bufferSize / sizeof(wchar_t));
        userData.remove_prefix(userDataConsumed);
        sink.append(buffer.data(), buffer.size() - 1); // buffer is null-terminated.
    }

    return ec;
}

} // namespace

bool TdhMessageFormatter::FormatEventMessage(EventInfo const info,
                                             size_t const pointerSize,
                                             wchar_t* const buffer,
                                             size_t const bufferSize)
{
    if (!info)
        return false;

    cspan<std::byte> userData = info.UserData();
    if (info.IsStringOnly()) {
        (void)StringCchCopyNW(buffer, bufferSize,
                              reinterpret_cast<wchar_t const*>(userData.data()),
                              userData.size() / sizeof(wchar_t));
        return true;
    }

    formattedProperties.clear();
    formattedPropertiesOffsets.clear();
    formattedPropertiesPointers.clear();

    wchar_t const* const message = info.EventMessage();
    if (!message)
        return FormatMofEvent(info, pointerSize, buffer, bufferSize);

    for (ULONG i = 0; i < info->TopLevelPropertyCount; ++i) {
        auto const& pi = info->EventPropertyInfoArray[i];

        size_t begin = formattedProperties.size();
        DWORD const ec = FormatProperty(info, pi, pointerSize, userData,
                                        formattedProperties, propertyBuffer, mapBuffer);
        if (ec != ERROR_SUCCESS)
            return false;

        formattedProperties.push_back(0);
        formattedPropertiesOffsets.push_back(begin);
    }

    for (auto const& begin : formattedPropertiesOffsets)
        formattedPropertiesPointers.push_back(
            reinterpret_cast<DWORD_PTR>(&formattedProperties[begin]));

    auto const formatFlags = FORMAT_MESSAGE_FROM_STRING | FORMAT_MESSAGE_ARGUMENT_ARRAY;
    DWORD const numWritten =
        FormatMessageW(formatFlags, message, 0, 0, buffer, bufferSize,
                       reinterpret_cast<va_list*>(formattedPropertiesPointers.data()));

    return numWritten != 0;

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

bool TdhMessageFormatter::FormatMofEvent(EventInfo const& info, size_t const pointerSize,
                                         wchar_t* const buffer, size_t const bufferSize)
{
    cspan<std::byte> userData = info.UserData();

    for (ULONG i = 0; i < info->TopLevelPropertyCount; ++i) {
        auto const& pi = info->EventPropertyInfoArray[i];

        if (i > 0)
            formattedProperties.append(L"; ");

        if (wchar_t const* propertyName = info.GetStringAt(pi.NameOffset)) {
            formattedProperties.append(propertyName);
            formattedProperties.append(L": ");
        }

        DWORD const ec = FormatProperty(info, pi, pointerSize, userData,
                                        formattedProperties, propertyBuffer, mapBuffer);
        if (ec != ERROR_SUCCESS)
            return false;
    }

    (void)StringCchCopyNW(buffer, bufferSize, formattedProperties.data(),
                          formattedProperties.length());
    return true;
}

} // namespace etk
