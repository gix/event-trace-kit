#pragma once
#include <string>

namespace etk
{

template<typename CharT, typename Traits>
size_t ZStringByteCount(std::basic_string<CharT, Traits> const& str)
{
    return (str.length() + 1) * sizeof(str[0]);
}

template<typename Container>
size_t ByteCount(Container const& container)
{
    return container.size() * sizeof(container.front());
}

} // namespace etk
