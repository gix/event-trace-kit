#pragma once
#include "ffmf/Common/Support/NoOverflow.h"
#include <type_traits>

namespace ffmf
{

template<typename T>
struct treat_as_floating_point : std::is_floating_point<T>
{
};

template<typename FromRep, typename ToRep>
using enable_if_convertible_rep_t =
    std::enable_if_t<
        std::is_convertible<FromRep, ToRep>::value &&
        (treat_as_floating_point<ToRep>::value ||
        !treat_as_floating_point<FromRep>::value)
    >;

template<typename Rep, typename FromScale, typename ToScale>
using enable_if_convertible_scale_t =
    std::enable_if_t<
        NoOverflow<FromScale, ToScale>::value && (
        treat_as_floating_point<Rep>::value ||
        (NoOverflow<FromScale, ToScale>::type::den == 1 &&
        !treat_as_floating_point<FromScale>::value))
    >;

} // namespace ffmf
