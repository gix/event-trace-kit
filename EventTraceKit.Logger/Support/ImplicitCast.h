#pragma once
#include "ffmf/Common/Support/Traits.h"

namespace ffmf
{

/// <summary>
///   Casts an expression to the specified type <typeparamref name="T"/>
///   using only implicit conversions.
/// </summary>
/// <devdoc>
///   identity_t is used to create a non-deduced context for <paramref name="expr"/>
///   which requires an explicit template-argument to be specified. Note that
///   the argument is already converted to <typeparamref name="T"/> at the
///   call-site thus providing better diagnostics.
/// </devdoc>
template<typename T>
T implicit_cast(identity_t<T> expr)
{
    return expr;
}

} // namespace ffmf
