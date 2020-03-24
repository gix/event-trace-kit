#pragma once
#include <type_traits>

namespace etk
{

template <typename, typename = std::size_t>
struct is_complete : std::false_type {};

template <typename T>
struct is_complete<T, decltype(sizeof(T))> : std::true_type {};

} // namespace etk
