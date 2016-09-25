#pragma once
#include "ADT/StringView.h"
#include <string>
#include <vector>

namespace etk
{

template<typename CharT, typename Traits>
size_t ZStringByteCount(std::basic_string<CharT, Traits> const& str)
{
    return (str.length() + 1) * sizeof(str[0]);
}

template<typename CharT, typename Traits>
size_t ZStringByteCount(basic_string_view<CharT, Traits> const& str)
{
    return (str.length() + 1) * sizeof(str[0]);
}

template<typename T, typename Allocator>
size_t ByteCount(std::vector<T, Allocator> const& container)
{
    return container.size() * sizeof(container.front());
}

} // namespace etk
