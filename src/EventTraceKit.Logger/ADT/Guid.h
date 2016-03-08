#pragma once
#include "ADT/ArrayRef.h"
#include "ADT/StringView.h"
#include "Support/CompilerSupport.h"

#include <cstring>
#include <iosfwd>
#include <string>
#include <type_traits>
#include <guiddef.h>

namespace etk
{

namespace details
{
    void CreateGuid(uint64_t& hi, uint64_t& lo);
    void ParseGuid(std::wstring const& str, uint64_t& hi, uint64_t& lo);
    std::wstring ToString(uint64_t const& hi, uint64_t const& lo);
    std::wostream& StreamGuid(std::wostream& os, uint64_t const& hi, uint64_t const& lo);
}

struct Guid
{
    uint64_t hi;
    uint64_t lo;

    bool operator ==(Guid const& other) const { return hi == other.hi && lo == other.lo; }
    bool operator !=(Guid const& other) const { return hi != other.hi || lo != other.lo; }
    bool operator < (Guid const& other) const { return hi == other.hi ? lo < other.lo : hi < other.hi; }

    static Guid Create();
    static Guid Construct();
    static Guid Construct(uint64_t const& hi, uint64_t const& lo);
    static Guid Construct(GUID const& source);
    static GUID ToGUID(Guid const& source);
    static Guid Construct(std::wstring const& str);

    static std::wstring ToString(Guid const& guid);

    template<typename T1, typename T2>
    static int Compare(T1 const& guid1, T2 const& guid2);

    static Guid const Nil;

    // {00000000-0000-0000-0000-000000000000}
    static size_t const StringLength = 38;

    // {00000000-0000-0000-0000-000000000000}\0
    static size_t const StringBufSize = StringLength + 1;
};

inline Guid Guid::Create()
{
    Guid guid;
    details::CreateGuid(guid.hi, guid.lo);
    return guid;
}

inline Guid Guid::Construct()
{
    static_assert(std::is_pod<Guid>::value, "Guid must be a POD.");
    return Guid();
}

inline Guid Guid::Construct(uint64_t const& hi, uint64_t const& lo)
{
    Guid guid = { hi, lo };
    return guid;
}

inline Guid Guid::Construct(GUID const& source)
{
    Guid guid;
    static_assert(sizeof(source) == sizeof(guid), "Guid size mismatch.");
    std::memcpy((char*)&guid + 0, &source.Data3, sizeof(source.Data3));
    std::memcpy((char*)&guid + 2, &source.Data2, sizeof(source.Data2));
    std::memcpy((char*)&guid + 4, &source.Data1, sizeof(source.Data1));
    std::memcpy((char*)&guid + 8, source.Data4 + 7, sizeof(char));
    std::memcpy((char*)&guid + 9, source.Data4 + 6, sizeof(char));
    std::memcpy((char*)&guid + 10, source.Data4 + 5, sizeof(char));
    std::memcpy((char*)&guid + 11, source.Data4 + 4, sizeof(char));
    std::memcpy((char*)&guid + 12, source.Data4 + 3, sizeof(char));
    std::memcpy((char*)&guid + 13, source.Data4 + 2, sizeof(char));
    std::memcpy((char*)&guid + 14, source.Data4 + 1, sizeof(char));
    std::memcpy((char*)&guid + 15, source.Data4 + 0, sizeof(char));
    return guid;
}

inline GUID Guid::ToGUID(Guid const& source)
{
    GUID guid;
    static_assert(sizeof(source) == sizeof(guid), "Guid size mismatch.");
    std::memcpy(&guid, &source, sizeof(guid));
    return guid;
}

inline GUID ConstructGUID(uint64_t hi, uint64_t lo)
{
    GUID guid;
    static_assert(sizeof(hi) + sizeof(lo) == sizeof(guid), "Guid size mismatch.");
    std::memcpy(&guid, &hi, sizeof(hi));
    std::memcpy((char*)&guid + sizeof(hi), &lo, sizeof(lo));
    return guid;
}

inline Guid Guid::Construct(std::wstring const& str)
{
    Guid guid;
    details::ParseGuid(str, guid.hi, guid.lo);
    return guid;
}

template<typename T1, typename T2>
inline int Guid::Compare(T1 const& guid1, T2 const& guid2)
{
    if (guid1.hi < guid2.hi) return -1;
    if (guid1.hi > guid2.hi) return 1;
    if (guid1.lo < guid2.lo) return -1;
    if (guid1.lo > guid2.lo) return 1;
    return 0;
}

inline std::wstring Guid::ToString(Guid const& guid)
{
    return details::ToString(guid.hi, guid.lo);
}

template<typename Tag>
struct TaggedGuid
{
    uint64_t hi;
    uint64_t lo;

    bool operator ==(TaggedGuid const& other) const { return hi == other.hi && lo == other.lo; }
    bool operator !=(TaggedGuid const& other) const { return hi != other.hi || lo != other.lo; }
    bool operator < (TaggedGuid const& other) const { return hi == other.hi ? lo < other.lo : hi < other.hi; }

    static TaggedGuid Construct();
    static TaggedGuid Construct(uint64_t const& hi, uint64_t const& lo);
    static TaggedGuid Construct(Guid const& source);
    static TaggedGuid Construct(GUID const& source);
    static TaggedGuid Construct(std::wstring const& str);
    static TaggedGuid Create();
    static std::wstring ToString(TaggedGuid const& guid);

    inline Guid ToGuid() const
    {
        Guid guid = { hi, lo };
        return guid;
    }
};

template<typename T>
TaggedGuid<T> TaggedGuid<T>::Create()
{
    TaggedGuid guid;
    details::CreateGuid(guid.hi, guid.lo);
    return guid;
}

template<typename T>
TaggedGuid<T> TaggedGuid<T>::Construct()
{
    static_assert(std::is_pod<TaggedGuid>::value, "TaggedGuid must be a POD.");
    return TaggedGuid();
}

template<typename T>
TaggedGuid<T> TaggedGuid<T>::Construct(uint64_t const& hi, uint64_t const& lo)
{
    TaggedGuid guid;
    guid.hi = hi;
    guid.lo = lo;
    return guid;
}

template<typename T>
TaggedGuid<T> TaggedGuid<T>::Construct(Guid const& source)
{
    TaggedGuid guid = { source.hi, source.lo };
    return guid;
}

template<typename T>
TaggedGuid<T> TaggedGuid<T>::Construct(GUID const& source)
{
    TaggedGuid guid;
    static_assert(sizeof(source) == sizeof(guid), "Guid size mismatch.");
    std::memcpy(&guid, &source, sizeof(guid));
    return guid;
}

template<typename T>
TaggedGuid<T> TaggedGuid<T>::Construct(std::wstring const& str)
{
    TaggedGuid guid;
    details::ParseGuid(str, guid.hi, guid.lo);
    return guid;
}

template<typename T>
std::wstring TaggedGuid<T>::ToString(TaggedGuid<T> const& guid)
{
    return details::ToString(guid.hi, guid.lo);
}

inline std::wostream& operator <<(std::wostream& os, Guid const& guid)
{
    return details::StreamGuid(os, guid.hi, guid.lo);
}

template<typename T>
std::wostream& operator <<(std::wostream& os, TaggedGuid<T> const& guid)
{
    return details::StreamGuid(os, guid.hi, guid.lo);
}

bool GuidToString(GUID const& guid, MutableArrayRef<wchar_t> buffer);

template<size_t N>
ETK_ALWAYS_INLINE
void GuidToString(GUID const& guid, _Out_writes_z_(39) wchar_t (&buffer)[N])
{
    static_assert(N >= Guid::StringBufSize, "Buffer too small");
    (void)GuidToString(guid, MutableArrayRef<wchar_t>(buffer, N));
}

ETK_ALWAYS_INLINE
void GuidToString(GUID const& guid, std::wstring& sink)
{
    size_t s = sink.size();
    sink.resize(s + Guid::StringLength);
    (void)GuidToString(guid, MutableArrayRef<wchar_t>(&sink[s], Guid::StringLength + 1));
}

class GuidString
{
public:
    GuidString(GUID const& id) { GuidToString(id, buffer); }
    wchar_t const* get() const { return buffer; }
    operator wchar_t const*() const { return buffer; }
    operator wstring_view() const { return buffer; }
private:
    wchar_t buffer[Guid::StringBufSize];
};

} // namespace ffmf

#define ETK_MAKE_GUID(high, low) Guid::Construct((uint64_t)high##LL, (uint64_t)low##LL)
