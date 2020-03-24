#pragma once
#include <algorithm>

namespace etk
{

template<typename ForwardIterator, typename T, typename Predicate>
ForwardIterator binary_find(ForwardIterator first, ForwardIterator last, T const& value,
                            Predicate pred)
{
    auto it = std::lower_bound(first, last, value, pred);
    return (it != last && !pred(value, *it)) ? it : last;
}

template<typename ForwardIterator, typename T>
ForwardIterator binary_find(ForwardIterator first, ForwardIterator last, T const& value)
{
    return binary_find(first, last, value, std::less<>());
}

template<typename ForwardRange, typename T, typename Predicate>
auto binary_find(ForwardRange& range, T const& value, Predicate pred)
    -> decltype(std::begin(range))
{
    return binary_find(std::begin(range), std::end(range), value, pred);
}

template<typename ForwardRange, typename T>
auto binary_find(ForwardRange& range, T const& value) -> decltype(std::begin(range))
{
    return binary_find(std::begin(range), std::end(range), value);
}

} // namespace etk
