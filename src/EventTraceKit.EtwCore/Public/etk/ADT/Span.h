#pragma once
#include "etk/Support/CompilerSupport.h"
#include "etk/Support/Debug.h"
#include "etk/Support/IsComplete.h"

#include <array>
#include <cstddef>
#include <iterator>
#include <utility>

namespace etk
{

inline constexpr std::size_t dynamic_extent = std::numeric_limits<std::size_t>::max();

template<typename ElementType, std::size_t Extent = dynamic_extent>
class span;

namespace details
{

template<typename T>
struct IsSpan : std::false_type
{};

template<typename T, std::size_t E>
struct IsSpan<span<T, E>> : std::true_type
{};

template<typename T>
struct IsStdArray : std::false_type
{};

template<typename T, std::size_t N>
struct IsStdArray<std::array<T, N>> : std::true_type
{};

template<typename T, typename _ = void>
struct IsContainer : std::false_type
{};

template<typename T>
struct IsContainer<T,
                   std::conditional_t<false,
                                      std::void_t<decltype(std::data(std::declval<T>())),
                                                  decltype(std::size(std::declval<T>()))>,
                                      void>> : std::true_type
{};

template<typename Container, typename ElementType, typename = void>
struct IsContainerElementTypeCompatible : std::false_type
{};

template<typename Container, typename ElementType>
struct IsContainerElementTypeCompatible<
    Container, ElementType, std::void_t<decltype(std::data(std::declval<Container>()))>>
    : std::is_convertible<
          std::remove_pointer_t<decltype(std::data(std::declval<Container>()))> (*)[],
          ElementType (*)[]>
{};

template<typename ContainerRef, typename Container, typename ElementType>
constexpr bool IsContainerCompatibleImpl =
    !IsSpan<Container>::value && !IsStdArray<Container>::value &&
    !std::is_array_v<Container> && IsContainer<Container>::value &&
    IsContainerElementTypeCompatible<ContainerRef, ElementType>::value;

template<typename ContainerRef, typename ElementType>
using IsContainerCompatible = std::enable_if_t<IsContainerCompatibleImpl<
    ContainerRef, std::remove_cv_t<std::remove_reference_t<ContainerRef>>, ElementType>>;

} // namespace details

/// <summary>
///   The class template <c>span</c> describes an object that can refer to a
///   contiguous sequence of objects with the first element of the sequence at
///   position zero. A span can either have a static extent, in which case the
///   number of elements in the sequence is known and encoded in the type, or a
///   dynamic extent.
/// </summary>
/// <devdoc>
///   Supported since C++20. Not yet provided by the STL. Implementation based
///   on the latest C++20 working draft N4800 (2019-01-21) including changes
///   from the adopted P1227R1 (2019-01-21) and P1024R2 (2019-01-20) proposals.
/// </devdoc>
template<typename ElementType, std::size_t Extent>
class span
{
public:
    using element_type = ElementType;
    using value_type = std::remove_cv_t<ElementType>;
    using index_type = std::size_t;
    using difference_type = std::size_t;
    using pointer = element_type*;
    using reference = element_type&;
    using iterator = element_type*;
    using const_iterator = element_type const*;
    using reverse_iterator = std::reverse_iterator<iterator>;
    using const_reverse_iterator = std::reverse_iterator<const_iterator>;
    static constexpr index_type extent = Extent;

    // [ISO] 21.7.3.1, Overview

    static_assert(Extent == dynamic_extent || Extent >= 0,
                  "A span must have an extent >= 0, or a dynamic extent");
    static_assert(std::is_object<ElementType>::value,
                  "span's ElementType must be an object type");
    static_assert(is_complete<ElementType>::value,
                  "span's ElementType must be a complete type");
    static_assert(!std::is_abstract<ElementType>::value,
                  "span's ElementType cannot be an abstract class type");

    // 21.7.3.2, constructors, copy, and assignment

    template<std::size_t E = Extent, typename = std::enable_if_t<E <= 0>>
    constexpr span() noexcept
    {}

    constexpr span(pointer ptr, index_type count)
        : data_(ptr)
    {
        ETK_ASSERT(count == Extent);
    }

    constexpr span(pointer first, pointer last)
        : data_(first)
    {
        ETK_ASSERT(last - first == Extent);
    }

    template<std::size_t E = Extent, typename = std::enable_if_t<E == 1>>
    constexpr span(element_type& elem)
        : data_(&elem)
    {}

    template<std::size_t E = Extent, typename = std::enable_if_t<E >= Extent>>
    constexpr span(element_type (&arr)[E]) noexcept
        : data_(arr)
    {}

    template<typename = std::enable_if_t<
                 std::is_convertible_v<value_type (*)[], element_type (*)[]>>>
    constexpr span(std::array<value_type, Extent>& arr) noexcept
        : data_(arr.data())
    {}

    template<typename = std::enable_if_t<
                 std::is_convertible_v<value_type (*)[], element_type (*)[]>>>
    constexpr span(std::array<value_type, Extent> const& arr) noexcept
        : data_(arr.data())
    {}

    template<typename Container,
             typename = details::IsContainerCompatible<Container const&, ElementType>>
    constexpr span(Container const& container)
        : data_(std::data(container))
    {}

    template<typename Container,
             typename = details::IsContainerCompatible<Container&, ElementType>>
    constexpr span(Container& container)
        : data_(std::data(container))
    {}

    constexpr span(span const& source) noexcept = default;
    constexpr span& operator=(span const& source) noexcept = default;

    ~span() noexcept = default;

    template<typename OtherElementType, std::size_t OtherExtent,
             typename = std::enable_if_t<
                 Extent == OtherExtent &&
                 std::is_convertible_v<OtherElementType (*)[], ElementType (*)[]>>>
    constexpr span(span<OtherElementType, OtherExtent> const& s) noexcept
        : data_(s.data())
    {}

    // 21.7.3.3, subviews

    template<std::size_t Count>
    constexpr span<element_type, Count> first() const
    {
        static_assert(Count <= size_);
        return {data_, Count};
    }

    template<std::size_t Count>
    constexpr span<element_type, Count> last() const
    {
        static_assert(Count <= size_);
        return {data_ + (size_ - Count), Count};
    }

    constexpr span<element_type> first(index_type count) const
    {
        ETK_ASSERT(count <= size_);
        return {data_, count};
    }

    constexpr span<element_type> last(index_type count) const
    {
        ETK_ASSERT(count <= size_);
        return {data_ + (size_ - count), count};
    }

    template<std::size_t Offset, std::size_t Count = dynamic_extent>
    constexpr span<element_type, Count> subspan() const
    {
        static_assert(Offset <= size_ &&
                          (Count == dynamic_extent || Count <= size_ - Offset),
                      "Sliced range not in array.");
        return {data_ + Offset, Count == dynamic_extent ? size_ - Offset : Count};
    }

    constexpr span<element_type> subspan(index_type offset,
                                         index_type count = dynamic_extent) const
    {
        ETK_ASSERT_MSG(offset <= size_ &&
                           (count == dynamic_extent || offset + count <= size_),
                       "Sliced range not in array.");
        return {data_ + offset, count == dynamic_extent ? size_ - offset : count};
    }

    // 21.7.3.4, observers

    constexpr index_type size() const noexcept { return size_; }

    constexpr index_type size_bytes() const noexcept
    {
        return size_ * static_cast<index_type>(sizeof(element_type));
    }

    [[nodiscard]] constexpr bool empty() const noexcept { return size_ == 0; }

    // 21.7.3.5, element access

    constexpr reference operator[](index_type idx) const
    {
        ETK_ASSERT_MSG(idx < size(), "Index out or range");
        return data_[idx];
    }

    constexpr reference front() const
    {
        static_assert(size_ != 0);
        return data_[0];
    }

    constexpr reference back() const
    {
        static_assert(size_ != 0);
        return data_[size_ - 1];
    }

    constexpr pointer data() const noexcept { return data_; }

    // 21.7.3.6, iterator support

    constexpr iterator begin() const noexcept { return data_; }

    constexpr iterator end() const noexcept { return data_ + size_; }

    constexpr const_iterator cbegin() const noexcept { return data_; }

    constexpr const_iterator cend() const noexcept { return data_ + size_; }

    constexpr reverse_iterator rbegin() const noexcept
    {
        return reverse_iterator(cend());
    }

    constexpr reverse_iterator rend() const noexcept
    {
        return reverse_iterator(cbegin());
    }

    constexpr const_reverse_iterator crbegin() const noexcept
    {
        return const_reverse_iterator(cend());
    }

    constexpr const_reverse_iterator crend() const noexcept
    {
        return const_reverse_iterator(cbegin());
    }

private:
    pointer data_ = nullptr;
    static constexpr std::size_t size_ = Extent;
};

template<typename ElementType>
class span<ElementType, dynamic_extent>
{
public:
    using element_type = ElementType;
    using value_type = std::remove_cv_t<ElementType>;
    using index_type = std::size_t;
    using difference_type = std::size_t;
    using pointer = element_type*;
    using reference = element_type&;
    using iterator = element_type*;
    using const_iterator = element_type const*;
    using reverse_iterator = std::reverse_iterator<iterator>;
    using const_reverse_iterator = std::reverse_iterator<const_iterator>;
    static constexpr index_type extent = dynamic_extent;

    // 21.7.3.2, constructors, copy, and assignment

    constexpr span() noexcept = default;

    constexpr span(pointer ptr, index_type count)
        : data_(ptr)
        , size_(count)
    {}

    constexpr span(pointer first, pointer last)
        : data_(first)
        , size_(last - first)
    {}

    constexpr span(element_type& elem)
        : data_(&elem)
        , size_(1)
    {}

    template<std::size_t N>
    constexpr span(element_type (&arr)[N]) noexcept
        : data_(arr)
        , size_(N)
    {}

    template<std::size_t N, typename = std::enable_if_t<std::is_convertible_v<
                                value_type (*)[], element_type (*)[]>>>
    constexpr span(std::array<value_type, N>& arr) noexcept
        : data_(arr.data())
        , size_(N)
    {}

    template<std::size_t N, typename = std::enable_if_t<std::is_convertible_v<
                                value_type (*)[], element_type (*)[]>>>
    constexpr span(std::array<value_type, N> const& arr) noexcept
        : data_(arr.data())
        , size_(N)
    {}

    template<typename Container,
             typename = details::IsContainerCompatible<Container const&, ElementType>>
    constexpr span(Container const& container)
        : data_(std::data(container))
        , size_(std::size(container))
    {}

    template<typename Container,
             typename = details::IsContainerCompatible<Container&, ElementType>>
    constexpr span(Container& container)
        : data_(std::data(container))
        , size_(std::size(container))
    {}

    constexpr span(span const& source) noexcept = default;
    constexpr span& operator=(span const& source) noexcept = default;

    ~span() noexcept = default;

    template<typename OtherElementType, std::size_t OtherExtent,
             typename = std::enable_if_t<
                 std::is_convertible_v<OtherElementType (*)[], ElementType (*)[]>>>
    constexpr span(span<OtherElementType, OtherExtent> const& s) noexcept
        : data_(s.data())
        , size_(s.size())
    {}

    // 21.7.3.3, subviews

    template<std::size_t Count>
    constexpr span<element_type, Count> first() const
    {
        ETK_ASSERT(Count <= size_);
        return {data_, Count};
    }

    template<std::size_t Count>
    constexpr span<element_type, Count> last() const
    {
        ETK_ASSERT(Count <= size_);
        return {data_ + (size_ - Count), Count};
    }

    constexpr span<element_type> first(index_type count) const
    {
        ETK_ASSERT(count <= size_);
        return {data_, count};
    }

    constexpr span<element_type> last(index_type count) const
    {
        ETK_ASSERT(count <= size_);
        return {data_ + (size_ - count), count};
    }

    template<std::size_t Offset, std::size_t Count = dynamic_extent>
    constexpr span<element_type, Count> subspan() const
    {
        ETK_ASSERT_MSG(Offset <= size_ &&
                           (Count == dynamic_extent || Count <= size_ - Offset),
                       "Sliced range not in array.");
        return {data_ + Offset, Count == dynamic_extent ? size_ - Offset : Count};
    }

    constexpr span<element_type> subspan(index_type offset) const
    {
        ETK_ASSERT_MSG(offset <= size_, "Cannot slice past end of array.");
        return {data_ + offset, size_ - offset};
    }

    constexpr span<element_type> subspan(index_type offset, index_type count) const
    {
        ETK_ASSERT_MSG(offset <= size_ &&
                           (count == dynamic_extent || offset + count <= size_),
                       "Sliced range not in array.");
        return {data_ + offset, count == dynamic_extent ? size_ - offset : count};
    }

    constexpr void remove_prefix(index_type n)
    {
        ETK_ASSERT_MSG(n <= size_, "Cannot remove such a long prefix");
        data_ += n;
        size_ -= n;
    }

    constexpr void remove_suffix(index_type n)
    {
        ETK_ASSERT_MSG(n <= size_, "Cannot remove such a long suffix");
        size_ -= n;
    }

    // 21.7.3.4, observers

    constexpr index_type size() const noexcept { return size_; }

    constexpr index_type size_bytes() const noexcept
    {
        return size_ * static_cast<index_type>(sizeof(element_type));
    }

    [[nodiscard]] constexpr bool empty() const noexcept { return size_ == 0; }

    // 21.7.3.5, element access

    constexpr reference operator[](index_type idx) const
    {
        ETK_ASSERT_MSG(idx < size(), "Index out or range");
        return data_[idx];
    }

    constexpr reference front() const
    {
        ETK_ASSERT(!empty());
        return data_[0];
    }

    constexpr reference back() const
    {
        ETK_ASSERT(!empty());
        return data_[size_ - 1];
    }

    constexpr pointer data() const noexcept { return data_; }

    // 21.7.3.6, iterator support

    constexpr iterator begin() const noexcept { return data_; }

    constexpr iterator end() const noexcept { return data_ + size_; }

    constexpr const_iterator cbegin() const noexcept { return data_; }

    constexpr const_iterator cend() const noexcept { return data_ + size_; }

    constexpr reverse_iterator rbegin() const noexcept
    {
        return reverse_iterator(cend());
    }

    constexpr reverse_iterator rend() const noexcept
    {
        return reverse_iterator(cbegin());
    }

    constexpr const_reverse_iterator crbegin() const noexcept
    {
        return const_reverse_iterator(cend());
    }

    constexpr const_reverse_iterator crend() const noexcept
    {
        return const_reverse_iterator(cbegin());
    }

private:
    pointer data_ = nullptr;
    index_type size_ = 0;
};

template<typename T, std::size_t N>
span(T (&)[N])->span<T, N>;

template<typename T, std::size_t N>
span(std::array<T, N>&)->span<T, N>;

template<typename T, std::size_t N>
span(std::array<T, N> const&)->span<T const, N>;

template<typename Container>
span(Container&)->span<typename Container::value_type>;

template<typename Container>
span(Container const&)->span<typename Container::value_type const>;

// 21.7.3.7, Comparison operators
template<typename T, std::size_t X, typename U, std::size_t Y>
constexpr bool operator==(span<T, X> l, span<U, Y> r)
{
    return l.data() == r.data() && l.size() == r.size();
}

template<typename T, std::size_t X, typename U, std::size_t Y>
constexpr bool operator!=(span<T, X> l, span<U, Y> r)
{
    return !(l == r);
}

template<typename T, std::size_t X, typename U, std::size_t Y>
constexpr bool operator<(span<T, X> l, span<U, Y> r)
{
    return (l.data() < r.data()) || (!(r.data() < l.data()) && l.size() < r.size());
}

template<typename T, std::size_t X, typename U, std::size_t Y>
constexpr bool operator>(span<T, X> l, span<U, Y> r)
{
    return r < l;
}

template<typename T, std::size_t X, typename U, std::size_t Y>
constexpr bool operator<=(span<T, X> l, span<U, Y> r)
{
    return !(r < l);
}

template<typename T, std::size_t X, typename U, std::size_t Y>
constexpr bool operator>=(span<T, X> l, span<U, Y> r)
{
    return !(l < r);
}

template<typename ElementType, std::size_t Extent>
span<std::byte const,
     (Extent == dynamic_extent ? dynamic_extent : sizeof(ElementType) * Extent)>
as_bytes(span<ElementType, Extent> s) noexcept
{
    return {reinterpret_cast<std::byte const*>(s.data()), s.size_bytes()};
}

template<typename ElementType, std::size_t Extent>
span<std::byte,
     (Extent == dynamic_extent ? dynamic_extent : sizeof(ElementType) * Extent)>
as_writable_bytes(span<ElementType, Extent> s) noexcept
{
    return {reinterpret_cast<std::byte*>(s.data()), s.size_bytes()};
}

template<typename T, std::size_t Extent = dynamic_extent>
using cspan = span<T const, Extent>;

/// Helper because template deduction guides do not work yet with aliases.
template<typename T, std::size_t N>
constexpr cspan<T> make_cspan(T const (&array)[N])
{
    return cspan<T>(array);
}

} // namespace etk

namespace std
{

// [P1024r2] Tuple interface to span

template<typename ElementType, size_t Extent>
struct tuple_size<::etk::span<ElementType, Extent>> : integral_constant<size_t, Extent>
{};

template<typename ElementType>
struct tuple_size<::etk::span<ElementType, ::etk::dynamic_extent>>; // not defined

template<size_t Index, typename ElementType, size_t Extent>
struct tuple_element<Index, ::etk::span<ElementType, Extent>>
{
    static_assert(Index < Extent, "span index out of bounds");
    using type = ElementType;
};

template<size_t Index, typename ElementType>
struct tuple_element<Index,
                     ::etk::span<ElementType, ::etk::dynamic_extent>>; // not defined

template<size_t Index, typename ElementType, size_t Extent>
[[nodiscard]] constexpr ElementType& get(::etk::span<ElementType, Extent> span) noexcept
{
    static_assert(Index < Extent, "span index out of bounds");
    return span[Index];
}

template<size_t Index, typename ElementType>
[[nodiscard]] constexpr ElementType& get(
    ::etk::span<ElementType, ::etk::dynamic_extent> span) noexcept = delete;

} // namespace std
