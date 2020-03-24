#pragma once
#include <cassert>

#define ETK_ASSERT(expr) assert(expr)

#define ETK_ASSERT_MSG(expr, message) assert((expr) && message)

#define ETK_Assume(expr) static_assert((expr), "Assumption broken: " #expr)
