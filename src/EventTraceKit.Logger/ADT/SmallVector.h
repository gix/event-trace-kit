#pragma once
#include <boost/container/small_vector.hpp>

namespace etk
{

template<typename T, size_t N, typename Allocator = boost::container::new_allocator<T>>
using SmallVector = boost::container::small_vector<T, N, Allocator>;

template<typename T, typename Allocator = boost::container::new_allocator<T>>
using SmallVectorBase = boost::container::small_vector_base<T, Allocator>;

} // namespace etk
