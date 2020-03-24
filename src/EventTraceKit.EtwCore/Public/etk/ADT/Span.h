#pragma once
#include "etk/Support/CompilerSupport.h"
#include "etk/Support/Debug.h"
#include "etk/Support/IsComplete.h"

#include <array>
#include <cstddef>
#include <iterator>
#include <vector>

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

template<typename Container, typename ElementType, typename ContainerRef>
using IsContainerCompatible =
    std::enable_if_t<!IsSpan<Container>::value && !IsStdArray<Container>::value &&
                     IsContainer<Container>::value &&
                     IsContainerElementTypeCompatible<ContainerRef, ElementType>::value>;

} // namespace details

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

    // 26.7.3.1, Overview
    static_assert(Extent == dynamic_extent || Extent >= 0,
                  "A span must have an extent >= 0, or a dynamic extent");
    static_assert(std::is_object<ElementType>::value,
                  "span's ElementType must be an object type");
    static_assert(is_complete<ElementType>::value,
                  "span's ElementType must be a complete type");
    static_assert(!std::is_abstract<ElementType>::value,
                  "span's ElementType cannot be an abstract class type");

    // 26.7.3.2, constructors, copy, and assignment
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

    template<typename = std::enable_if_t<Extent == 1>>
    constexpr span(element_type& elem)
        : data_(&elem)
    {}

    constexpr span(element_type (&arr)[Extent]) noexcept
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

    template<typename Container, typename = details::IsContainerCompatible<
                                     Container, ElementType, Container&>>
    constexpr span(Container& container)
        : data_(std::data(container))
    {}

    template<typename Container, typename = details::IsContainerCompatible<
                                     Container, ElementType, Container const&>>
    constexpr span(Container const& container)
        : data_(std::data(container))
    {}

    constexpr span(span const& source) noexcept = default;
    constexpr span& operator=(span const& source) noexcept = default;

    ~span() noexcept = default;

    template<typename OtherElementType, std::size_t OtherExtent,
             typename = std::enable_if_t<
                 Extent == OtherExtent &&
                 std::is_convertible_v<OtherElementType (*)[], ElementType (*)[]>>>
    constexpr span(span<OtherElementType> const& s) noexcept
        : data_(s.data())
    {}

    // 26.7.3.3, subviews
    template<std::size_t Count>
    constexpr span<element_type, Count> first() const
    {
        ETK_ASSERT(Count < size_);
        return {data_, Count};
    }

    template<std::size_t Count>
    constexpr span<element_type, Count> last() const
    {
        ETK_ASSERT(Count < size_);
        return {data_, (size_ - Count)};
    }

    constexpr span<element_type> first(index_type count) const
    {
        ETK_ASSERT(count >= 0 && count < size_);
        return {data_, count};
    }

    constexpr span<element_type> last(index_type count) const
    {
        ETK_ASSERT(count >= 0 && count < size_);
        return {data_ + (size_ - count), count};
    }

    template<std::size_t Offset, std::size_t Count = dynamic_extent>
    constexpr span<element_type, Count> subspan() const
    {
        ETK_ASSERT_MSG(Offset + Count <= size_, "Sliced range not in array.");
        return {data_ + Offset, Count != dynamic_extent ? Count : size() - Offset};
    }

    constexpr span<element_type> subspan(index_type offset,
                                         index_type count = dynamic_extent) const
    {
        ETK_ASSERT_MSG(offset + count <= size_, "Sliced range not in array.");
        return {data_ + offset, count};
    }

    constexpr reference front() const
    {
        static_assert(!empty());
        return data_[0];
    }

    constexpr reference back() const
    {
        static_assert(!empty());
        return data_[size_ - 1];
    }

    constexpr span<ElementType> slice(index_type offset) const
    {
        ETK_ASSERT_MSG(offset <= size(), "Cannot slice past end of array.");
        return {data_ + offset, size_ - offset};
    }

    constexpr span<ElementType> slice(index_type offset, index_type count) const
    {
        ETK_ASSERT_MSG(offset + count <= size(), "Sliced range not in array.");
        return {data_ + offset, count};
    }

    // 26.7.3.4, observers

    constexpr index_type size() const noexcept { return size_; }

    constexpr index_type size_bytes() const noexcept
    {
        return size_ * static_cast<index_type>(sizeof(ElementType));
    }

    [[nodiscard]] constexpr bool empty() const noexcept { return size_ == 0; }

    // 26.7.3.5, element access

    constexpr reference operator[](index_type idx) const
    {
        ETK_ASSERT(idx < size() && "Index out or range");
        return data_[idx];
    }

    constexpr pointer data() const noexcept { return data_; }

    // 26.7.3.6, iterator support

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

    // 26.7.3.2, constructors, copy, and assignment
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

#if 0
    // Crashes MSVC 19.14.26430 and earlier when the check for the presence of
    // std::data encounters a type with a "data[N]" member.

    template<typename Container, typename = details::IsContainerCompatible<
                                     Container, ElementType, Container&>>
    constexpr span(Container& container)
        : data_(std::data(container))
        , size_(std::size(container))
    {
    }

    template<typename Container, typename = details::IsContainerCompatible<
                                     Container, ElementType, Container const&>>
    constexpr span(Container const& container)
        : data_(std::data(container))
        , size_(std::size(container))
    {
    }
#endif

    constexpr span(std::vector<value_type>& container)
        : data_(std::data(container))
        , size_(std::size(container))
    {}

    constexpr span(std::vector<value_type> const& container)
        : data_(std::data(container))
        , size_(std::size(container))
    {}

    constexpr span(span const& source) noexcept = default;
    constexpr span& operator=(span const& source) noexcept = default;

    ~span() noexcept = default;

    template<typename OtherElementType, std::size_t OtherExtent,
             typename = std::enable_if_t<
                 std::is_convertible_v<OtherElementType (*)[], ElementType (*)[]>>>
    constexpr span(span<OtherElementType> const& s) noexcept
        : data_(s.data())
        , size_(s.size())
    {}

    // 26.7.3.3, subviews
    template<std::size_t Count>
    constexpr span<element_type, Count> first() const
    {
        ETK_ASSERT(Count >= 0 && Count < size_);
        return {data_, Count};
    }

    template<std::size_t Count>
    constexpr span<element_type, Count> last() const
    {
        ETK_ASSERT(Count >= 0 && Count < size_);
        return {data_, (size_ - Count)};
    }

    constexpr span<element_type> first(index_type count) const
    {
        ETK_ASSERT(count >= 0 && count < size_);
        return {data_, count};
    }

    constexpr span<element_type> last(index_type count) const
    {
        ETK_ASSERT(count >= 0 && count < size_);
        return {data_ + (size_ - count), count};
    }

    template<std::size_t Offset, std::size_t Count = dynamic_extent>
    constexpr span<element_type, Count> subspan() const
    {
        ETK_ASSERT_MSG(Offset + Count <= size_, "Sliced range not in array.");
        return {data_ + Offset, Count != dynamic_extent ? Count : size() - Offset};
    }

    constexpr span<element_type> subspan(index_type offset,
                                         index_type count = dynamic_extent) const
    {
        ETK_ASSERT_MSG(offset + count <= size_, "Sliced range not in array.");
        return {data_ + offset, count};
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

    constexpr span<ElementType> slice(index_type offset) const
    {
        ETK_ASSERT_MSG(offset <= size(), "Cannot slice past end of array.");
        return {data_ + offset, size_ - offset};
    }

    constexpr span<ElementType> slice(index_type offset, index_type count) const
    {
        ETK_ASSERT_MSG(offset + count <= size(), "Sliced range not in array.");
        return {data_ + offset, count};
    }

    // 26.7.3.4, observers

    constexpr index_type size() const noexcept { return size_; }

    constexpr index_type size_bytes() const noexcept
    {
        return size_ * sizeof(ElementType);
    }

    [[nodiscard]] constexpr bool empty() const noexcept { return size_ == 0; }

    // 26.7.3.5, element access

    constexpr reference operator[](index_type idx) const
    {
        ETK_ASSERT_MSG(idx < size(), "Index out or range");
        return data_[idx];
    }

    constexpr pointer data() const noexcept { return data_; }

    // 26.7.3.6, iterator support

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
span(std::array<T, N> const&)->span<const T, N>;

template<typename Container>
span(Container&)->span<typename Container::value_type>;

template<typename Container>
span(Container const&)->span<const typename Container::value_type>;

// 26.7.3.7, Comparison operators
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
