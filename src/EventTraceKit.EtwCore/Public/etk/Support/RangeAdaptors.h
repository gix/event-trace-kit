#pragma once
#include <algorithm>
#include <iterator>
#include <utility>

namespace etk
{

template<typename SinglePassRange, typename UnaryPredicate>
inline typename SinglePassRange::difference_type count_if(SinglePassRange& container,
                                                          UnaryPredicate pred)
{
    return std::count_if(std::begin(container), std::end(container),
                         std::forward<UnaryPredicate>(pred));
}

template<typename SinglePassRange, typename UnaryPredicate>
inline typename SinglePassRange::difference_type count_if(
    SinglePassRange const& container, UnaryPredicate pred)
{
    return std::count_if(std::begin(container), std::end(container),
                         std::forward<UnaryPredicate>(pred));
}

template<typename SinglePassRange, typename UnaryPredicate>
inline typename SinglePassRange::iterator find_if(SinglePassRange& container,
                                                  UnaryPredicate pred)
{
    return std::find_if(std::begin(container), std::end(container),
                        std::forward<UnaryPredicate>(pred));
}

template<typename SinglePassRange, typename UnaryPredicate>
inline typename SinglePassRange::const_iterator find_if(SinglePassRange const& container,
                                                        UnaryPredicate pred)
{
    return std::find_if(std::begin(container), std::end(container),
                        std::forward<UnaryPredicate>(pred));
}

template<typename SinglePassRange, typename T>
inline typename SinglePassRange::iterator find(SinglePassRange& container, T const& value)
{
    return std::find(std::begin(container), std::end(container), value);
}

template<typename SinglePassRange, typename T>
inline typename SinglePassRange::const_iterator find(SinglePassRange const& container,
                                                     T const& value)
{
    return std::find(std::begin(container), std::end(container), value);
}

template<typename SinglePassRange, typename T>
inline bool contains(SinglePassRange& container, T const& value)
{
    auto it = std::find(std::begin(container), std::end(container), value);
    return it != std::end(container);
}

template<typename InputIterator, typename Diff, typename T>
inline bool contains_n(InputIterator first, Diff count, T const& value)
{
    auto last = first + count;
    auto it = std::find(first, last, value);
    return it != last;
}

} // namespace etk
