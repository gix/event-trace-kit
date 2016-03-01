#pragma once
#include <cstring>
#include <type_traits>

namespace ffmf
{

/// <summary>
///   Copies the source bits into a value of the target type (both types have
///   to have equal sizes). This essentially does an aliasing-safe reinterpret_cast.
/// </summary>
/// <remarks>
///   Using reinterpet_cast to copy the bits between types is unsafe because
///   of the type-based aliasing rules. An exception to this rule are char*
///   pointers which are here used via memcpy. Note: Most compilers completely
///   optimize this away.
/// </remarks>
template<typename Target, typename Source>
Target bit_cast(Source source)
{
    static_assert(sizeof(Source) == sizeof(Target), "Source and Target type must have equal size.");
    static_assert(std::is_trivially_copyable<Target>::value, "Target type must be trivially copyable.");
    static_assert(std::is_trivially_copyable<Source>::value, "Source type must be trivially copyable.");

    Target target;
    std::memcpy(&target, &source, sizeof(target));
    return target;
}

} // namespace ffmf
