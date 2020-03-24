#pragma once
#include "etk/Support/CompilerSupport.h"

namespace etk
{

template<typename T, size_t N>
ETK_ALWAYS_INLINE
  constexpr size_t countof(T (&/*array*/)[N])
{
    return N;
}

template<size_t N>
ETK_ALWAYS_INLINE
  constexpr size_t lengthof(char (&/*array*/)[N])
{
    return N - 1;
}

template<size_t N>
ETK_ALWAYS_INLINE
  constexpr size_t lengthof(char const (&/*array*/)[N])
{
    return N - 1;
}

template<size_t N>
ETK_ALWAYS_INLINE
  constexpr size_t lengthof(wchar_t (&/*array*/)[N])
{
    return N - 1;
}

template<size_t N>
ETK_ALWAYS_INLINE
  constexpr size_t lengthof(wchar_t const (&/*array*/)[N])
{
    return N - 1;
}

template<size_t N>
ETK_ALWAYS_INLINE
constexpr size_t lengthof(char16_t (&/*array*/)[N])
{
    return N - 1;
}

template<size_t N>
ETK_ALWAYS_INLINE
constexpr size_t lengthof(char16_t const (&/*array*/)[N])
{
    return N - 1;
}

template<size_t N>
ETK_ALWAYS_INLINE
constexpr size_t lengthof(char32_t(&/*array*/)[N])
{
    return N - 1;
}

template<size_t N>
ETK_ALWAYS_INLINE
constexpr size_t lengthof(char32_t const (&/*array*/)[N])
{
    return N - 1;
}

} // namespace etk
