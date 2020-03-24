#pragma once
#include <cstdarg>
#include <string>

namespace etk
{

inline bool vsprintf(std::wstring& sink, size_t expectedLength, wchar_t const* format, va_list args)
{
    size_t currSize = sink.size();

    bool measured = false;
retry:
    if (expectedLength == 0) {
        va_list a;
        va_copy(a, args);
        int ret = _scwprintf(format, a);
        va_end(a);
        if (ret < 0)
            return false;
        if (ret == 0)
            return true;
        measured = true;
        expectedLength = static_cast<size_t>(ret);
    }

    if (expectedLength > 0)
        sink.resize(currSize + expectedLength);

    int ret = vswprintf(&sink[currSize], expectedLength + 1, format, args);
    if (ret < 0) {
        if (measured)
            return false;
        expectedLength = 0;
        goto retry;
    }

    size_t written = static_cast<unsigned>(ret);
    sink.resize(currSize + written);

    return written <= expectedLength;
}

inline bool vsprintf(std::wstring& sink, wchar_t const* format, va_list args)
{
    return vsprintf(sink, 0, format, args);
}

inline bool sprintf(std::wstring& sink, wchar_t const* format, ...)
{
    va_list args;
    va_start(args, format);
    bool ret = vsprintf(sink, 0, format, args);
    va_end(args);
    return ret;
}

inline bool sprintf(std::wstring& sink, size_t expectedLength, wchar_t const* format, ...)
{
    va_list args;
    va_start(args, format);
    bool ret = vsprintf(sink, expectedLength, format, args);
    va_end(args);
    return ret;
}

} // namespace etk
