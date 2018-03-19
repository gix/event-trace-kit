#pragma once
#if __cplusplus_cli
#include <algorithm>
#include <string_view>

#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>
#include <vcclr.h>

namespace msclr
{
namespace interop
{

template<>
inline System::DateTime marshal_as<System::DateTime, ::FILETIME>(::FILETIME const& time)
{
    uint64_t ticks = (uint64_t(time.dwHighDateTime) << 32) | time.dwLowDateTime;
    return System::DateTime::FromFileTime(ticks);
}

template<>
inline System::String^ marshal_as(std::wstring_view const& str)
{
    auto size = static_cast<int>(
        std::min(static_cast<size_t>(System::Int32::MaxValue), str.size()));
    return gcnew System::String(str.data(), 0, size);
}

template<>
inline System::Guid marshal_as(GUID const& guid)
{
    return System::Guid(
        guid.Data1, guid.Data2, guid.Data3,
        guid.Data4[0], guid.Data4[1], guid.Data4[2], guid.Data4[3],
        guid.Data4[4], guid.Data4[5], guid.Data4[6], guid.Data4[7]);
}

#pragma warning(push)
#pragma warning(disable: 4400)

template<>
inline GUID marshal_as(System::Guid const& guid)
{
    array<System::Byte>^ guidData = const_cast<System::Guid&>(guid).ToByteArray();
    pin_ptr<System::Byte> data = &(guidData[0]);
    return *reinterpret_cast<GUID*>(data);
}

#pragma warning(pop)

template<typename T>
inline std::vector<T> marshal_as_vector(System::Collections::Generic::List<T>^ list)
{
    if (list == nullptr)
        return std::vector<T>();

    std::vector<T> result;
    result.reserve(list->Count);
    for each (T item in list)
        result.push_back(item);
    return result;
}

} // namespace interop
} // namespace msclr

#endif // __cplusplus_cli
