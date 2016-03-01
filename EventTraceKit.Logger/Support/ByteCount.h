#pragma once
#include <string>
#include <vector>

namespace etk
{

template<typename CharT, typename Traits>
inline size_t ByteCount(std::basic_string<CharT, Traits> const& str)
{
    return (str.length() + 1) * sizeof(str[0]);
}

template<typename T, typename Allocator>
inline size_t ByteCount(std::vector<T, Allocator> const& container)
{
    return container.size() * sizeof(container.front());
}

} // namespace etk
