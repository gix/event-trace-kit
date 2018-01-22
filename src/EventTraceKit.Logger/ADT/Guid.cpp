#include "Guid.h"

#include "Win32Exception.h"
#include <windows.h>

#include <ostream>
#include <string>

#include <ObjBase.h>

ETK_DIAGNOSTIC_PUSH()
ETK_DIAGNOSTIC_DISABLE_CLANG("-Wdeprecated")
ETK_DIAGNOSTIC_DISABLE_CLANG("-Wsign-conversion")
ETK_DIAGNOSTIC_DISABLE_MSVC(4996)
// HACK: Boost relies on the fat windows.h for the cryptapi instead of including
// wincrypt.h directly as stated in the documentation. This fails with
// WIN32_LEAN_AND_MEAN.
#ifdef BOOST_USE_WINDOWS_H
#include <wincrypt.h>
#endif
#include <boost/uuid/random_generator.hpp>
#include <boost/uuid/uuid.hpp>
ETK_DIAGNOSTIC_POP()


namespace etk
{

namespace
{
wchar_t const* const UpperHex = L"0123456789ABCDEF";

template<typename T>
wchar_t* ToUpperHex(T value, wchar_t* buffer, size_t digits)
{
    wchar_t* ptr = buffer + digits;
    for (size_t i = 0; i < digits; ++i) {
        *(--ptr) = UpperHex[value & 0xF];
        value /= 16;
    }

    return buffer + digits;
}

template<typename T>
bool TryParseHex(wchar_t const*& buffer, size_t size, T& value)
{
    wchar_t const* ptr = buffer;

    value = static_cast<T>(0);
    for (size_t i = 0; i < size; ++i) {
        value *= 16;

        wchar_t c = *(ptr++);
        if (c >= L'0' && c <= L'9') {
            value += c - L'0';
        } else if (c >= L'A' && c <= L'F') {
            value += c - L'A' + 10;
        } else if (c >= L'a' && c <= L'f') {
            value += c - L'a' + 10;
        } else {
            value = static_cast<T>(0);
            return false;
        }
    }

    buffer += size;
    return true;
}
} // namespace

Guid const Guid::Nil = {};

void details::CreateGuid(uint64_t& hiref, uint64_t& loref)
{
    boost::uuids::random_generator gen;
    boost::uuids::uuid u = gen();

    Guid guid;
    static_assert(sizeof(u.data) == sizeof(guid), "Guid size mismatch.");
    std::memcpy(&guid, u.data, sizeof(guid));
    hiref = guid.hi;
    loref = guid.lo;
}

void details::ParseGuid(std::wstring const& str, uint64_t& hiref, uint64_t& loref)
{
    hiref = 0;
    loref = 0;

    size_t len = str.length();
    if (len == 0)
        return;

    if (len != 32 && // dddddddddddddddddddddddddddddddd
        len != 36 && // dddddddd-dddd-dddd-dddd-dddddddddddd
        len != 38)   // {dddddddd-dddd-dddd-dddd-dddddddddddd}
        return;

    uint64_t hi = 0;
    uint64_t lo = 0;

    wchar_t const* ptr = &str[0];

    // dddddddddddddddddddddddddddddddd
    if (len == 32) {
        if (!TryParseHex(ptr, 16, hi)) return;
        if (!TryParseHex(ptr, 16, lo)) return;
        hiref = hi;
        loref = lo;
        return;
    }

    // {dddddddd-dddd-dddd-dddd-dddddddddddd}
    if (len == 38) {
        // Check and just for enclosing braces.
        if (str[0] != L'{' || str[str.length() - 1] != L'}')
            return;
        ++ptr;
    }

    uint32_t data1;
    uint32_t data2;
    uint32_t data3;
    uint32_t data4;
    uint64_t data5;
    if (!TryParseHex(ptr, 8, data1)) return;
    if (*(ptr++) != L'-') return;
    if (!TryParseHex(ptr, 4, data2)) return;
    if (*(ptr++) != L'-') return;
    if (!TryParseHex(ptr, 4, data3)) return;
    if (*(ptr++) != L'-') return;
    if (!TryParseHex(ptr, 4, data4)) return;
    if (*(ptr++) != L'-') return;
    if (!TryParseHex(ptr, 12, data5)) return;

    hiref = (static_cast<uint64_t>(data1) << 32) | (static_cast<uint64_t>(data2) << 16) | data3;
    loref = (static_cast<uint64_t>(data4) << 48) | data5;
}

std::wstring details::ToString(uint64_t const& hi, uint64_t const& lo)
{
    // {12345678-1234-1234-1234-123456789012}
    std::wstring str(36, L'\0');

    wchar_t* ptr = &str[0];
    ptr = ToUpperHex(static_cast<uint32_t>((hi >> 32) & 0xFFFFFFFF), ptr, 8);
    *(ptr++) = L'-';
    ptr = ToUpperHex(static_cast<uint32_t>((hi >> 16) & 0xFFFF), ptr, 4);
    *(ptr++) = L'-';
    ptr = ToUpperHex(static_cast<uint32_t>(hi & 0xFFFF), ptr, 4);
    *(ptr++) = L'-';
    ptr = ToUpperHex(static_cast<uint32_t>((lo >> 48) & 0xFFFF), ptr, 4);
    *(ptr++) = L'-';
    ptr = ToUpperHex(lo & 0xFFFFFFFFFFFF, ptr, 12);

    return str;
}

std::wostream& details::StreamGuid(std::wostream& os, uint64_t const& hi, uint64_t const& lo)
{
    os << ToString(hi, lo);
    return os;
}

bool GuidToString(GUID const& guid, MutableArrayRef<wchar_t> buffer)
{
    return SUCCEEDED(StringFromGUID2(guid, buffer.data(),
                                     static_cast<int>(buffer.size())));
}

} // namespace etk
