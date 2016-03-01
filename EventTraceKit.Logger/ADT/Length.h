#pragma once
#include "ffmf/Common/ADT/Unit.h"
#include "ffmf/Common/Support/CompilerSupport.h"
#include "ffmf/Common/Support/NoOverflow.h"

#include <ratio>
#include <type_traits>

namespace ffmf
{

enum class LengthUnit
{
    Pixel,
    Point
};

template<LengthUnit Unit, typename Rep, typename Scale>
struct Area;

template<typename T>
struct is_area : public std::false_type { };

template<LengthUnit Unit, typename Rep, typename Scale>
struct is_area<Area<Unit, Rep, Scale> > : public std::true_type { };

template<typename To, LengthUnit Unit, typename Rep, typename Scale>
inline std::enable_if_t<is_area<To>::value, To>
area_cast(Area<Unit, Rep, Scale> const& area);


/// <summary>
///   A type that holds an area of a certain unit.
/// </summary>
/// <typeparam name="Unit">
///   The unit this area represents. Areas of different units are incompatible.
/// </typeparam>
/// <typeparam name="Rep">
///   The type that is used to hold the area amount.
/// </typeparam>
/// <typeparam name="Rep">
///   A ratio that describes the scaling of the area.
/// </typeparam>
template<LengthUnit Unit, typename Rep, typename Scale>
struct Area
{
    typedef Rep rep;
    typedef Scale frac;

    FFMF_ALWAYS_INLINE constexpr Area()
        : value()
    {
    }

    template<typename Rep2, typename = enable_if_convertible_rep_t<Rep2, Rep>>
    FFMF_ALWAYS_INLINE constexpr explicit Area(Rep2 const& value)
        : value(static_cast<Rep>(value))
    {
    }

    template<typename Rep2, typename Scale2, typename = enable_if_convertible_scale_t<Rep, Scale2, Scale>>
    FFMF_ALWAYS_INLINE constexpr Area(Area<Unit, Rep2, Scale2> const& area)
        : value(area_cast<Area>(area).amount())
    {
    }

    FFMF_ALWAYS_INLINE constexpr rep amount() const
    {
        return value;
    }

    FFMF_ALWAYS_INLINE Area operator +() const { return *this; }
    FFMF_ALWAYS_INLINE Area operator -() const { return Area(-value); }

    FFMF_ALWAYS_INLINE Area& operator ++()    { ++value; return *this; }
    FFMF_ALWAYS_INLINE Area  operator ++(int) { return Area(value++); }
    FFMF_ALWAYS_INLINE Area& operator --()    { --value; return *this; }
    FFMF_ALWAYS_INLINE Area  operator --(int) { return Area(value--); }

    FFMF_ALWAYS_INLINE Area& operator +=(Area const& rhs)
    {
        value += rhs.value;
        return *this;
    }

    FFMF_ALWAYS_INLINE Area& operator -=(Area const& rhs)
    {
        value -= rhs.value;
        return *this;
    }

    FFMF_ALWAYS_INLINE Area& operator *=(rep const& rhs)
    {
        value *= rhs;
        return *this;
    }

    FFMF_ALWAYS_INLINE Area& operator /=(rep const& rhs)
    {
        value /= rhs;
        return *this;
    }

    FFMF_ALWAYS_INLINE Area& operator %=(rep const& rhs)
    {
        value %= rhs;
        return *this;
    }

    FFMF_ALWAYS_INLINE Area& operator %=(Area const& rhs)
    {
        value %= rhs.amount();
        return *this;
    }

private:
    rep value;
};



template<LengthUnit Unit, typename Rep, typename Scale = std::ratio<1>>
struct Length;

template<typename T>
struct is_length : public std::false_type { };

template<LengthUnit Unit, typename Rep, typename Scale>
struct is_length<Length<Unit, Rep, Scale> > : public std::true_type { };

template<typename To, LengthUnit Unit, typename Rep, typename Scale>
inline std::enable_if_t<is_length<To>::value, To>
length_cast(Length<Unit, Rep, Scale> const& length);


/// <summary>
///   A type that holds a length of a certain unit.
/// </summary>
/// <typeparam name="Unit">
///   The unit this length represents. Lengths of different units are incompatible.
/// </typeparam>
/// <typeparam name="Rep">
///   The type that is used to hold the length amount.
/// </typeparam>
/// <typeparam name="Rep">
///   A ratio that describes the scaling of the length.
/// </typeparam>
template<LengthUnit Unit, typename Rep, typename Scale>
struct Length
{
    typedef Rep rep;
    typedef Scale frac;
    typedef Area<Unit, Rep, Scale> area;

    FFMF_ALWAYS_INLINE constexpr Length()
        : value()
    {
    }

    template<typename Rep2, typename = enable_if_convertible_rep_t<Rep2, Rep>>
    FFMF_ALWAYS_INLINE constexpr explicit Length(Rep2 const& value)
        : value(static_cast<Rep>(value))
    {
    }

    template<typename Rep2, typename Scale2, typename = enable_if_convertible_scale_t<rep, Scale2, Scale>>
    FFMF_ALWAYS_INLINE constexpr Length(Length<Unit, Rep2, Scale2> const& length)
        : value(length_cast<Length>(length).amount())
    {
    }

    FFMF_ALWAYS_INLINE constexpr rep amount() const
    {
        return value;
    }

    FFMF_ALWAYS_INLINE Length operator +() const { return *this; }
    FFMF_ALWAYS_INLINE Length operator -() const { return Length(-value); }

    FFMF_ALWAYS_INLINE Length& operator ++()    { ++value; return *this; }
    FFMF_ALWAYS_INLINE Length  operator ++(int) { return Length(value++); }
    FFMF_ALWAYS_INLINE Length& operator --()    { --value; return *this; }
    FFMF_ALWAYS_INLINE Length  operator --(int) { return Length(value--); }

    FFMF_ALWAYS_INLINE Length& operator +=(Length const& rhs)
    {
        value += rhs.value;
        return *this;
    }

    FFMF_ALWAYS_INLINE Length& operator -=(Length const& rhs)
    {
        value -= rhs.value;
        return *this;
    }

    FFMF_ALWAYS_INLINE area& operator *=(Length const& rhs)
    {
        return area(value * rhs.value);
    }

    FFMF_ALWAYS_INLINE Length& operator *=(rep const& rhs)
    {
        value *= rhs;
        return *this;
    }

    FFMF_ALWAYS_INLINE Length& operator /=(rep const& rhs)
    {
        value /= rhs;
        return *this;
    }

    FFMF_ALWAYS_INLINE Length& operator %=(rep const& rhs)
    {
        value %= rhs;
        return *this;
    }

    FFMF_ALWAYS_INLINE Length& operator %=(Length const& rhs)
    {
        value %= rhs.amount();
        return *this;
    }

private:
    rep value;
};


namespace Details
{

template<typename L1, typename L2>
struct LengthEq
{
    FFMF_ALWAYS_INLINE
    bool operator ()(L1 const& lhs, L1 const& rhs) const
    {
        typedef typename std::common_type<L1, L2>::type CommonTy;
        return CommonTy(lhs).amount() == CommonTy(rhs).amount();
    }
};

template<typename L1>
struct LengthEq<L1, L1>
{
    FFMF_ALWAYS_INLINE
    bool operator ()(L1 const& lhs, L1 const& rhs) const
    {
        return lhs.amount() == rhs.amount();
    }
};

template<typename L1, typename L2>
struct LengthLt
{
    FFMF_ALWAYS_INLINE
    bool operator ()(L1 const& lhs, L2 const& rhs) const
    {
        typedef typename std::common_type<L1, L2>::type CommonTy;
        return CommonTy(lhs).amount() < CommonTy(rhs).amount();
    }
};

template<typename Length>
struct LengthLt<Length, Length>
{
    FFMF_ALWAYS_INLINE
    bool operator ()(Length const& lhs, const Length& rhs) const
    {
        return lhs.amount() < rhs.amount();
    }
};

} // Details


template<LengthUnit U1, typename R1, typename S1, LengthUnit U2, typename R2, typename S2>
FFMF_ALWAYS_INLINE
bool operator ==(Length<U1, R1, S1> const& lhs, Length<U2, R2, S2> const& rhs)
{
    return Details::LengthEq<Length<U1, R1, S1>, Length<U2, R2, S2>>()(lhs, rhs);
}

template<LengthUnit U1, typename R1, typename S1, LengthUnit U2, typename R2, typename S2>
FFMF_ALWAYS_INLINE
bool operator !=(Length<U1, R1, S1> const& lhs, Length<U2, R2, S2> const& rhs)
{
    return !(lhs == rhs);
}

template<LengthUnit U1, typename R1, typename S1, LengthUnit U2, typename R2, typename S2>
FFMF_ALWAYS_INLINE
bool operator <(Length<U1, R1, S1> const& lhs, Length<U2, R2, S2> const& rhs)
{
    return Details::LengthLt<Length<U1, R1, S1>, Length<U2, R2, S2> >()(lhs, rhs);
}

template<LengthUnit U1, typename R1, typename S1, LengthUnit U2, typename R2, typename S2>
FFMF_ALWAYS_INLINE
bool operator >(Length<U1, R1, S1> const& lhs, Length<U2, R2, S2> const& rhs)
{
    return rhs < lhs;
}

template<LengthUnit U1, typename R1, typename S1, LengthUnit U2, typename R2, typename S2>
FFMF_ALWAYS_INLINE
bool operator <=(Length<U1, R1, S1> const& lhs, Length<U2, R2, S2> const& rhs)
{
    return !(rhs < lhs);
}

template<LengthUnit U1, typename R1, typename S1, LengthUnit U2, typename R2, typename S2>
FFMF_ALWAYS_INLINE
bool operator >=(Length<U1, R1, S1> const& lhs, Length<U2, R2, S2> const& rhs)
{
    return !(lhs < rhs);
}


namespace Details
{

template<
    typename Length,
    typename R2,
    bool = std::is_convertible<
        R2,
        typename std::common_type<typename Length::rep, R2>::type
    >::value
>
struct LengthDivideImp
{
};

template<LengthUnit U1, typename R1, typename S1, typename R2>
struct LengthDivideImp<Length<U1, R1, S1>, R2, true>
{
    typedef Length<U1, typename std::common_type<R1, R2>::type, S1> type;
};

template<typename Length, typename Rep, bool = is_length<Rep>::value>
struct LengthDivideResultType
{
};

template<LengthUnit U1, typename R1, typename S1, typename R2>
struct LengthDivideResultType<Length<U1, R1, S1>, R2, false>
    : LengthDivideImp<Length<U1, R1, S1>, R2>
{
};

} // namespace Details


// Length + Length -> Length
template<LengthUnit U1, typename R1, typename S1, LengthUnit U2, typename R2, typename S2>
FFMF_ALWAYS_INLINE
constexpr
typename std::common_type<Length<U1, R1, S1>, Length<U2, R2, S2>>::type
  operator +(Length<U1, R1, S1> const& lhs, Length<U2, R2, S2> const& rhs)
{
    typedef typename std::common_type<Length<U1, R1, S1>, Length<U2, R2, S2>>::type CT;
    return CT(CT(lhs).amount() + CT(rhs).amount());
}

// Length - Length -> Length
template<LengthUnit U1, typename R1, typename S1, LengthUnit U2, typename R2, typename S2>
FFMF_ALWAYS_INLINE
constexpr
typename std::common_type<Length<U1, R1, S1>, Length<U2, R2, S2>>::type
  operator -(Length<U1, R1, S1> const& lhs, Length<U2, R2, S2> const& rhs)
{
    typedef typename std::common_type<Length<U1, R1, S1>, Length<U2, R2, S2>>::type CT;
    return CT(CT(lhs).amount() - CT(rhs).amount());
}

// Length * scalar -> Length
template<typename R1, typename S1, LengthUnit U1, typename R2>
FFMF_ALWAYS_INLINE
constexpr
std::enable_if_t<
    std::is_convertible<R2, typename std::common_type<R1, R2>::type>::value,
    Length<U1, typename std::common_type<R1, R2>::type, S1>
>
  operator *(Length<U1, R1, S1> const& size, R2 const& scalar)
{
    typedef typename std::common_type<R1, R2>::type CR;
    typedef Length<U1, CR, S1> CT;
    return CT(CT(size).amount() * static_cast<CR>(scalar));
}

// scalar * Length -> Length
template<typename R1, typename R2, typename S2, LengthUnit U2>
FFMF_ALWAYS_INLINE
constexpr
std::enable_if_t<
    std::is_convertible<R1, typename std::common_type<R1, R2>::type>::value,
    Length<U2, typename std::common_type<R1, R2>::type, S2>
>
  operator *(R1 const& scalar, Length<U2, R2, S2> const& size)
{
    return size * scalar;
}

// Length / scalar -> Length
template<typename R1, typename S1, LengthUnit U1, typename R2>
FFMF_ALWAYS_INLINE
constexpr
typename Details::LengthDivideResultType<Length<U1, R1, S1>, R2>::type
  operator /(Length<U1, R1, S1> const& size, R2 const& scalar)
{
    typedef typename std::common_type<R1, R2>::type CR;
    typedef Length<U1, CR, S1> CT;
    return CT(CT(size).amount() / static_cast<CR>(scalar));
}

template<LengthUnit U1, typename R1, typename S1, LengthUnit U2, typename R2, typename S2>
FFMF_ALWAYS_INLINE
constexpr
typename std::common_type<R1, R2>::type
  operator /(Length<U1, R1, S1> const& lhs, Length<U2, R2, S2> const& rhs)
{
    typedef typename std::common_type<Length<U1, R1, S1>, Length<U2, R2, S2>>::type CT;
    return CT(lhs).amount() / CT(rhs).amount();
}

template<LengthUnit U1, typename R1, typename S1, typename R2>
FFMF_ALWAYS_INLINE
constexpr
typename Details::LengthDivideResultType<Length<U1, R1, S1>, R2>::type
  operator %(Length<U1, R1, S1> const& size, R2 const& modulus)
{
    typedef typename std::common_type<R1, R2>::type CR;
    typedef Length<U1, CR, S1> CT;
    return CT(CT(size).amount() % static_cast<CR>(modulus));
}

template<LengthUnit U1, typename R1, typename S1, LengthUnit U2, typename R2, typename S2>
FFMF_ALWAYS_INLINE
constexpr
typename std::common_type<Length<U1, R1, S1>, Length<U2, R2, S2> >::type
  operator %(Length<U1, R1, S1> const& lhs, Length<U2, R2, S2> const& rhs)
{
    typedef typename std::common_type<R1, R2>::type CR;
    typedef typename std::common_type<Length<U1, R1, S1>, Length<U2, R2, S2> >::type CT;
    return CT(static_cast<CR>(CT(lhs).amount()) % static_cast<CR>(CT(rhs).amount()));
}

// lengths: Unit{pixels,point}, rep{signed, unsigned}
typedef Length<LengthUnit::Pixel, unsigned, std::ratio<1>> Pixels;
typedef Area<LengthUnit::Pixel, unsigned, std::ratio<1>> SquarePixels;

} // namespace ffmf
