#pragma once
#include "ffmf/Common/Support/CompilerSupport.h"
#include "ffmf/Common/Support/NoOverflow.h"

#include <ratio>
#include <type_traits>

namespace ffmf
{

template<typename T>
struct is_ratio : public std::false_type { };

template<intmax_t N, intmax_t D>
struct is_ratio<std::ratio<N, D>> : public std::true_type { };

template<typename Rep, typename Scale = std::ratio<1>>
struct StorageSize;

template<typename T>
struct is_storage_size : public std::false_type { };

template<typename Rep, typename Scale>
struct is_storage_size<StorageSize<Rep, Scale> > : public std::true_type { };

template<typename To, typename Rep, typename Scale>
inline std::enable_if_t<is_storage_size<To>::value, To>
storage_size_cast(StorageSize<Rep, Scale> const& size);

template<typename Rep, typename Scale>
struct StorageSize
{
    typedef Rep rep;
    typedef Scale scale;

    static_assert(is_ratio<scale>::value, "StorageSize scale must be an instance of std::ratio");
    static_assert(Scale::num > 0, "StorageSize scale must be positive");

    FFMF_ALWAYS_INLINE constexpr StorageSize()
        : value()
    {
    }

    template<typename Rep2, typename = enable_if_convertible_rep_t<Rep2, Rep>>
    FFMF_ALWAYS_INLINE constexpr explicit StorageSize(Rep2 const& value)
        : value(static_cast<Rep>(value))
    {
    }

    template<typename Rep2, typename Scale2, typename = enable_if_convertible_scale_t<Rep, Scale2, Scale>>
    FFMF_ALWAYS_INLINE constexpr StorageSize(StorageSize<Rep2, Scale2> const& size)
        : value(storage_size_cast<StorageSize>(size).count())
    {
    }

    FFMF_ALWAYS_INLINE constexpr rep count() const
    {
        return value;
    }

    FFMF_ALWAYS_INLINE StorageSize operator +() const { return *this; }
    FFMF_ALWAYS_INLINE StorageSize operator -() const { return StorageSize(-value); }

    FFMF_ALWAYS_INLINE StorageSize& operator ++()    { ++value; return *this; }
    FFMF_ALWAYS_INLINE StorageSize  operator ++(int) { return StorageSize(value++); }
    FFMF_ALWAYS_INLINE StorageSize& operator --()    { --value; return *this; }
    FFMF_ALWAYS_INLINE StorageSize  operator --(int) { return StorageSize(value--); }

    FFMF_ALWAYS_INLINE StorageSize& operator +=(StorageSize const& rhs)
    {
        value += rhs.value;
        return *this;
    }

    FFMF_ALWAYS_INLINE StorageSize& operator -=(StorageSize const& rhs)
    {
        value -= rhs.value;
        return *this;
    }

    FFMF_ALWAYS_INLINE StorageSize& operator *=(rep const& rhs)
    {
        value *= rhs;
        return *this;
    }

    FFMF_ALWAYS_INLINE StorageSize& operator /=(rep const& rhs)
    {
        value /= rhs;
        return *this;
    }

    FFMF_ALWAYS_INLINE StorageSize& operator %=(rep const& rhs)
    {
        value %= rhs;
        return *this;
    }

    FFMF_ALWAYS_INLINE StorageSize& operator %=(StorageSize const& rhs)
    {
        value %= rhs.count();
        return *this;
    }

private:
    rep value;
};

namespace Details
{

template<
    typename StorageSize,
    typename R2,
    bool = std::is_convertible<
        R2,
        typename std::common_type<typename StorageSize::rep, R2>::type
    >::value
>
struct StorageSizeDivideImp
{
};

template<typename R1, typename S1, typename R2>
struct StorageSizeDivideImp<StorageSize<R1, S1>, R2, true>
{
    typedef StorageSize<typename std::common_type<R1, R2>::type, S1> type;
};

template<typename StorageSize, typename Rep, bool = is_storage_size<Rep>::value>
struct StorageSizeDivideResultType
{
};

template<typename R1, typename S1, typename R2>
struct StorageSizeDivideResultType<StorageSize<R1, S1>, R2, false>
    : StorageSizeDivideImp<StorageSize<R1, S1>, R2>
{
};
} // namespace Details

// StorageSize + StorageSize -> StorageSize
template<typename R1, typename S1, typename R2, typename S2>
FFMF_ALWAYS_INLINE
constexpr
typename std::common_type<StorageSize<R1, S1>, StorageSize<R2, S2>>::type
  operator +(StorageSize<R1, S1> const& lhs, StorageSize<R2, S2> const& rhs)
{
    typedef typename std::common_type<StorageSize<R1, S1>, StorageSize<R2, S2>>::type CT;
    return CT(CT(lhs).count() + CT(rhs).count());
}

// StorageSize - StorageSize -> StorageSize
template<typename R1, typename S1, typename R2, typename S2>
FFMF_ALWAYS_INLINE
constexpr
typename std::common_type<StorageSize<R1, S1>, StorageSize<R2, S2>>::type
  operator -(StorageSize<R1, S1> const& lhs, StorageSize<R2, S2> const& rhs)
{
    typedef typename std::common_type<StorageSize<R1, S1>, StorageSize<R2, S2>>::type CT;
    return CT(CT(lhs).count() - CT(rhs).count());
}

// StorageSize * scalar -> StorageSize
template<typename R1, typename S1, typename R2>
FFMF_ALWAYS_INLINE
constexpr
std::enable_if_t<
    std::is_convertible<R2, typename std::common_type<R1, R2>::type>::value,
    StorageSize<typename std::common_type<R1, R2>::type, S1>
>
  operator *(StorageSize<R1, S1> const& size, R2 const& scalar)
{
    typedef typename std::common_type<R1, R2>::type CR;
    typedef StorageSize<CR, S1> CT;
    return CT(CT(size).count() * static_cast<CR>(scalar));
}

// scalar * StorageSize -> StorageSize
template<typename R1, typename R2, typename S2>
FFMF_ALWAYS_INLINE
constexpr
std::enable_if_t<
    std::is_convertible<R1, typename std::common_type<R1, R2>::type>::value,
    StorageSize<typename std::common_type<R1, R2>::type, S2>
>
  operator *(R1 const& scalar, StorageSize<R2, S2> const& size)
{
    return size * scalar;
}

// StorageSize / scalar -> StorageSize
template<typename R1, typename S, typename R2>
FFMF_ALWAYS_INLINE
constexpr
typename Details::StorageSizeDivideResultType<StorageSize<R1, S>, R2>::type
  operator /(StorageSize<R1, S> const& size, R2 const& scalar)
{
    typedef typename std::common_type<R1, R2>::type CR;
    typedef StorageSize<CR, S> CT;
    return CT(CT(size).count() / static_cast<CR>(scalar));
}

// StorageSize / StorageSize -> scalar
template<typename R1, typename S1, typename R2, typename S2>
FFMF_ALWAYS_INLINE
constexpr
typename std::common_type<R1, R2>::type
  operator /(StorageSize<R1, S1> const& lhs, StorageSize<R2, S2> const& rhs)
{
    typedef typename std::common_type<StorageSize<R1, S1>, StorageSize<R2, S2>>::type CT;
    return CT(lhs).count() / CT(rhs).count();
}

// StorageSize % scalar -> StorageSize
template<typename R1, typename S1, typename R2>
FFMF_ALWAYS_INLINE
constexpr
typename Details::StorageSizeDivideResultType<StorageSize<R1, S1>, R2>::type
  operator %(StorageSize<R1, S1> const& size, R2 const& modulus)
{
    typedef typename std::common_type<R1, R2>::type CR;
    typedef StorageSize<CR, S1> CT;
    return CT(CT(size).count() % static_cast<CR>(modulus));
}

// StorageSize % StorageSize -> StorageSize
template<typename R1, typename S1, typename R2, typename S2>
FFMF_ALWAYS_INLINE
constexpr
typename std::common_type<StorageSize<R1, S1>, StorageSize<R2, S2> >::type
  operator %(StorageSize<R1, S1> const& lhs, StorageSize<R2, S2> const& rhs)
{
    typedef typename std::common_type<R1, R2>::type CR;
    typedef typename std::common_type<StorageSize<R1, S1>, StorageSize<R2, S2> >::type CT;
    return CT(static_cast<CR>(CT(lhs).count()) % static_cast<CR>(CT(rhs).count()));
}

namespace Details
{

template<
    typename From,
    typename To,
    typename Scale = std::ratio_divide<typename From::scale, typename To::scale>,
    bool = Scale::num == 1,
    bool = Scale::den == 1>
struct storage_size_cast_impl
{
    To operator ()(From const& from)
    {
        typedef typename std::common_type<
            typename To::rep, typename From::rep, intmax_t>::type CommonTy;

        return To(static_cast<typename To::rep>(
            static_cast<CommonTy>(from.count()) *
            static_cast<CommonTy>(Scale::num) /
            static_cast<CommonTy>(Scale::den)));
    }
};

template<typename From, typename To, typename Scale>
struct storage_size_cast_impl<From, To, Scale, true, true> // Scale<1, 1>
{
    To operator ()(From const& from)
    {
        return To(static_cast<typename To::rep>(from.count()));
    }
};

template<typename From, typename To, typename Scale>
struct storage_size_cast_impl<From, To, Scale, false, true> // Scale<N, 1>
{
    To operator ()(From const& from)
    {
        typedef typename std::common_type<
            typename To::rep, typename From::rep, intmax_t>::type CommonTy;

        return To(
            static_cast<typename To::rep>(static_cast<CommonTy>(from.count())) *
            static_cast<CommonTy>(Scale::num));
    }
};

template<typename From, typename To, typename Scale>
struct storage_size_cast_impl<From, To, Scale, true, false> // Scale<1, D>
{
    To operator ()(From const& from)
    {
        typedef typename std::common_type<
            typename To::rep, typename From::rep, intmax_t>::type CommonTy;

        return To(static_cast<typename To::rep>(
            static_cast<CommonTy>(from.count()) / static_cast<CommonTy>(Scale::den)));
    }
};

} // namespace Details

template<typename To, typename Rep, typename Scale>
inline
std::enable_if_t<is_storage_size<To>::value, To>
  storage_size_cast(StorageSize<Rep, Scale> const& size)
{
    return Details::storage_size_cast_impl<StorageSize<Rep, Scale>, To>()(size);
}

typedef std::ratio<1024LL, 1> kibi;
typedef std::ratio<1024LL*1024, 1> mebi;
typedef std::ratio<1024LL*1024*1024, 1> gibi;
typedef std::ratio<1024LL*1024*1024*1024, 1> tebi;
typedef std::ratio<1024LL*1024*1024*1024*1024, 1> pebi;
typedef std::ratio<1024LL*1024*1024*1024*1024*1024, 1> exbi;

typedef StorageSize<unsigned, std::ratio<1, 8>> Bits;
typedef StorageSize<unsigned, std::ratio<1>> Bytes;

typedef StorageSize<unsigned, std::kilo> KiloBytes;
typedef StorageSize<unsigned, std::mega> MegaBytes;
typedef StorageSize<unsigned, std::giga> GigaBytes;
typedef StorageSize<unsigned, std::tera> TeraBytes;
typedef StorageSize<unsigned, std::peta> PetaBytes;
typedef StorageSize<unsigned, std::exa> ExaBytes;

typedef StorageSize<unsigned, kibi> KibiBytes;
typedef StorageSize<unsigned, mebi> MebiBytes;
typedef StorageSize<unsigned, gibi> GibiBytes;
typedef StorageSize<unsigned, tebi> TebiBytes;
typedef StorageSize<unsigned, pebi> PebiBytes;
typedef StorageSize<unsigned, exbi> ExbiBytes;

} // namespace ffmf
