#pragma once
#include "Support/CompilerSupport.h"
#include "Support/Debug.h"

#include <vector>

namespace etk
{

template<typename T>
class ArrayRef
{
public:
    using value_type             = T;
    using pointer                = T*;
    using const_pointer          = T const*;
    using reference              = T&;
    using const_reference        = T const&;
    using const_iterator         = const_pointer;
    using iterator               = const_iterator;
    using const_reverse_iterator = std::reverse_iterator<const_iterator>;
    using reverse_iterator       = const_reverse_iterator;
    using size_type              = size_t;
    using difference_type        = ptrdiff_t;

    ETK_ALWAYS_INLINE
    constexpr ArrayRef() noexcept
        : data_(nullptr), length_(0) {}

    ETK_ALWAYS_INLINE
    constexpr ArrayRef(ArrayRef const& source) noexcept = default;

    ETK_ALWAYS_INLINE
    ArrayRef& operator =(ArrayRef const& source) noexcept = default;

    ETK_ALWAYS_INLINE
    constexpr ArrayRef(T const& elem)
        : data_(&elem), length_(1) {}

    ETK_ALWAYS_INLINE
    constexpr ArrayRef(T const* data, size_type length)
        : data_(data), length_(length) {}

    ETK_ALWAYS_INLINE
    constexpr ArrayRef(T const* begin, T const* end)
        : data_(begin), length_(static_cast<size_type>(end - begin)) {}

    template<size_t N>
    ETK_ALWAYS_INLINE
    constexpr ArrayRef(T const (&array)[N])
        : data_(array), length_(N) {}

    template<size_t N>
    ETK_ALWAYS_INLINE
    constexpr ArrayRef(std::array<T, N>& array)
        : data_(array.data()), length_(N) {}

    ETK_ALWAYS_INLINE
    ArrayRef(std::vector<T> const& vector)
        : data_(vector.data()), length_(vector.size()) {}

    ETK_ALWAYS_INLINE constexpr const_iterator begin()  const noexcept { return cbegin(); }
    ETK_ALWAYS_INLINE constexpr const_iterator end()    const noexcept { return cend(); }
    ETK_ALWAYS_INLINE constexpr const_iterator cbegin() const noexcept { return data_; }
    ETK_ALWAYS_INLINE constexpr const_iterator cend()   const noexcept { return data_ + length_; }

    ETK_ALWAYS_INLINE const_reverse_iterator rbegin()  const noexcept { return const_reverse_iterator(cend()); }
    ETK_ALWAYS_INLINE const_reverse_iterator rend()    const noexcept { return const_reverse_iterator(cbegin()); }
    ETK_ALWAYS_INLINE const_reverse_iterator crbegin() const noexcept { return const_reverse_iterator(cend()); }
    ETK_ALWAYS_INLINE const_reverse_iterator crend()   const noexcept { return const_reverse_iterator(cbegin()); }

    ETK_ALWAYS_INLINE constexpr size_type size()   const noexcept { return length_; }
    ETK_ALWAYS_INLINE constexpr size_type length() const noexcept { return length_; }

    ETK_ALWAYS_INLINE constexpr bool empty() const noexcept { return length_ == 0; }

    ETK_ALWAYS_INLINE
    constexpr const_reference operator [](size_type pos) const
    {
        return ETK_ASSERT_MSG(pos < length(), "Index out or range"), data_[pos];
    }

    ETK_ALWAYS_INLINE
    constexpr const_reference front() const
    {
        return ETK_ASSERT(!empty()), data_[0];
    }

    ETK_ALWAYS_INLINE
    constexpr const_reference back() const
    {
        return ETK_ASSERT(!empty()), data_[length_ - 1];
    }

    ETK_ALWAYS_INLINE
    constexpr const_pointer data() const noexcept { return data_; }

    ETK_ALWAYS_INLINE
    void clear() noexcept
    {
        data_ = nullptr;
        length_ = 0;
    }

    ETK_ALWAYS_INLINE
    void remove_prefix(size_type n)
    {
        ETK_ASSERT_MSG(n <= length(), "Cannot remove such a long prefix");
        length_ -= n;
        data_ += n;
    }

    ETK_ALWAYS_INLINE
    void remove_suffix(size_type n)
    {
        ETK_ASSERT_MSG(n <= length(), "Cannot remove such a long suffix");
        length_ -= n;
    }

    ETK_ALWAYS_INLINE
    void swap(ArrayRef& a) noexcept
    {
        value_type const* d = data_;
        data_ = a.data_;
        a.data_ = d;

        size_type l = length_;
        length_ = a.length_;
        a.length_ = l;
    }

    ETK_ALWAYS_INLINE
    constexpr ArrayRef<T> slice(size_type offset) const
    {
      return ETK_ASSERT_MSG(offset <= size(), "Cannot slice past end of array."),
             ArrayRef<T>(data() + offset, size() - offset);
    }

    ETK_ALWAYS_INLINE
    constexpr ArrayRef<T> slice(size_type offset, size_type count) const
    {
      return ETK_ASSERT_MSG(offset + count <= size(), "Sliced range not in array."),
             ArrayRef<T>(data() + offset, count);
    }

private:
    const_pointer data_;
    size_type length_;
};


// operator ==
template<typename T>
ETK_ALWAYS_INLINE
bool operator ==(ArrayRef<T> x, ArrayRef<T> y)
{
    return x.length() == y.length() &&
           std::equal(x.begin(), x.end(), y.begin(), y.end());
}

// operator !=
template<typename T>
ETK_ALWAYS_INLINE
bool operator !=(ArrayRef<T> x, ArrayRef<T> y)
{
    return !(x == y);
}


template<typename T>
ETK_ALWAYS_INLINE
constexpr ArrayRef<T> MakeArrayRef(T const& elem)
{ return ArrayRef<T>(elem); }

template<typename T>
ETK_ALWAYS_INLINE
constexpr ArrayRef<T> MakeArrayRef(T const* data, typename ArrayRef<T>::size_type length)
{ return ArrayRef<T>(data, length); }

template<typename T>
ETK_ALWAYS_INLINE
constexpr ArrayRef<T> MakeArrayRef(T const* begin, T const* end)
{ return ArrayRef<T>(begin, end); }

template<typename T, size_t N>
constexpr ArrayRef<T> MakeArrayRef(T const (&array)[N])
{ return ArrayRef<T>(array); }


template<typename T>
class MutableArrayRef : public ArrayRef<T>
{
public:
    using value_type = typename ArrayRef<T>::value_type;
    using const_pointer = typename ArrayRef<T>::const_pointer;
    using const_reference = typename ArrayRef<T>::const_reference;
    using const_iterator = typename ArrayRef<T>::const_iterator;
    using pointer = value_type*;
    using reference = value_type&;
    using iterator = pointer;
    //using const_reverse_iterator = ...;
    //using reverse_iterator = const_reverse_iterator;
    using size_type = typename ArrayRef<T>::size_type;
    using difference_type = typename ArrayRef<T>::difference_type;

    using ArrayRef<T>::ArrayRef;

    ETK_ALWAYS_INLINE constexpr iterator begin() const noexcept { return data(); }
    ETK_ALWAYS_INLINE constexpr iterator end()   const noexcept { return data() + this->length(); }

    ETK_ALWAYS_INLINE
    constexpr pointer data() const noexcept
    {
      return const_cast<pointer>(ArrayRef<T>::data());
    }

    ETK_ALWAYS_INLINE
    constexpr reference operator[](size_t pos) const
    {
        ETK_ASSERT(pos < this->length() && "Index out or range");
        return data()[pos];
    }

    ETK_ALWAYS_INLINE
    constexpr reference front() const
    {
        ETK_ASSERT(!this->empty());
        return data()[0];
    }

    ETK_ALWAYS_INLINE
    constexpr reference back() const
    {
        ETK_ASSERT(!this->empty());
        return data()[this->length() - 1];
    }

    ETK_ALWAYS_INLINE
    constexpr MutableArrayRef<T> slice(size_type offset) const
    {
        ETK_ASSERT_MSG(offset <= this->size(), "Cannot slice past end of array.");
        return MutableArrayRef<T>(data() + offset, this->size() - offset);
    }

    ETK_ALWAYS_INLINE
    constexpr MutableArrayRef<T> slice(size_type offset, size_type count) const
    {
        ETK_ASSERT_MSG(offset + count <= this->size(), "Sliced range not in array.");
        return MutableArrayRef<T>(data() + offset, count);
    }
};

using BufferRef = MutableArrayRef<unsigned char>;

} // namespace etk
