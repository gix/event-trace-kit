#pragma once
#include "ffmf/Common/Debug.h"
#include <type_traits>

namespace ffmf
{

/// <summary>
///   Downcasts an expression of type <typeparamref name="Source"/> to the
///   specified <typeparamref name="Target"/> type. <typeparamref name="Target"/>
///   must be a sub-type of <typeparamref name="Source"/>.
/// </summary>
/// <param name="expr">
///   The pointer to downcast to <typeparamref name="Target"/>.
/// </param>
/// <returns>
///   <paramref name="expr"/> cast to <typeparamref name="Target"/> using
///   <c>static_cast</c>.
/// </returns>
/// <remarks>
///   Asserts that the cast will succeed for non-null values of
///   <paramref name="expr"/> using <c>dynamic_cast</c>. Therefore
///   <typeparamref name="Source"/> and <typeparamref name="Target"/> must
///   be polymorphic.
/// </remarks>
template<typename Target, typename Source>
Target down_cast(Source* expr)
{
    static_assert(
        (std::is_base_of<Source, std::remove_pointer_t<Target>>::value),
        "Illegal down_cast between unrelated types.");

    // Ensure correct cast using RTTI in debug builds.
    FFMF_ASSERT(expr == nullptr || dynamic_cast<Target>(expr) != nullptr);

    return static_cast<Target>(expr);
}

/// <summary>
///   Downcasts an expression of type <typeparamref name="Source"/> to the
///   specified <typeparamref name="Target"/> type. <typeparamref name="Target"/>
///   must be a sub-type of <typeparamref name="Source"/>.
/// </summary>
/// <param name="expr">
///   The reference to downcast to <typeparamref name="Target"/>.
/// </param>
/// <returns>
///   <paramref name="expr"/> cast to <typeparamref name="Target"/> using
///   <c>static_cast</c>.
/// </returns>
/// <remarks>
///   Asserts that the cast will succeed using <c>dynamic_cast</c>. Therefore
///   <typeparamref name="Source"/> and <typeparamref name="Target"/> must
///   be polymorphic.
/// </remarks>
template<typename Target, typename Source>
Target down_cast(Source& expr)
{
    static_assert(std::is_reference<Target>::value,
                  "Target type must be a reference.");

    typedef std::remove_reference_t<Target> NonRefTarget;
    static_assert(std::is_base_of<Source, NonRefTarget>::value,
                  "Illegal down_cast between unrelated types.");

    // Ensure correct cast using RTTI in debug builds.
    FFMF_ASSERT(dynamic_cast<NonRefTarget*>(&expr) != nullptr);

    return static_cast<Target>(expr);
}

} // namespace ffmf
