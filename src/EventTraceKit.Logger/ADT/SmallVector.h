#pragma once
ETK_DIAGNOSTIC_PUSH()
ETK_DIAGNOSTIC_DISABLE_MSVC(4996)
#include <absl/container/inlined_vector.h>
ETK_DIAGNOSTIC_POP()

namespace etk
{

template<typename T, size_t N, typename Allocator = std::allocator<T>>
using SmallVector = absl::InlinedVector<T, N, Allocator>;

} // namespace etk
