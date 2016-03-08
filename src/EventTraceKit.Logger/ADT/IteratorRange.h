#pragma once
#include <cstddef>
#include <utility>

namespace ffmf
{

template<typename T>
class iterator_range
{
public:
  iterator_range() {}

  iterator_range(T begin, T end)
      : begin_(std::move(begin))
      , end_(std::move(end)) {}

  T begin() const { return begin_; }
  T end() const { return end_; }

private:
  T begin_;
  T end_;
};

template<typename T>
inline iterator_range<T> make_range(T x, T y)
{
    return iterator_range<T>(std::move(x), std::move(y));
}

template<typename T>
inline iterator_range<T*> make_range(T* ptr, size_t count)
{
    return iterator_range<T*>(ptr, ptr + count);
}

} // namespace ffmf
