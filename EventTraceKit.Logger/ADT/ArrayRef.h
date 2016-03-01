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
    typedef T value_type;
    typedef T* pointer;
    typedef T const* const_pointer;
    typedef T& reference;
    typedef T const& const_reference;
    typedef const_pointer const_iterator;
    typedef const_iterator iterator;
    typedef std::reverse_iterator<const_iterator> const_reverse_iterator;
    typedef const_reverse_iterator reverse_iterator;
    typedef size_t size_type;
    typedef ptrdiff_t difference_type;

    ETK_ALWAYS_INLINE
    constexpr ArrayRef() noexcept
        : data_(nullptr), length_(0) {}

    ETK_ALWAYS_INLINE
    constexpr ArrayRef(ArrayRef const& source) noexcept = default;

    ETK_ALWAYS_INLINE
    ArrayRef& operator =(ArrayRef const& source) noexcept = default;

    ArrayRef(T const& elem)
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

    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
    void clear() noexcept
    {
        data_ = nullptr;
        length_ = 0;
    }

    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
    void remove_prefix(size_type n)
    {
        ETK_ASSERT_MSG(n <= length(), "Cannot remove such a long prefix");
        length_ -= n;
        data_ += n;
    }

    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
    void remove_suffix(size_type n)
    {
        ETK_ASSERT_MSG(n <= length(), "Cannot remove such a long suffix");
        length_ -= n;
    }

    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
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
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator ==(ArrayRef<T> x, ArrayRef<T> y)
{
    return x.length() == y.length() &&
           std::equal(x.begin(), x.end(), y.begin(), y.end());
}

// operator !=
template<typename T>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
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
    typedef typename ArrayRef<T>::value_type value_type;
    typedef typename ArrayRef<T>::const_pointer const_pointer;
    typedef typename ArrayRef<T>::const_reference const_reference;
    typedef typename ArrayRef<T>::const_iterator const_iterator;
    typedef value_type* pointer;
    typedef value_type& reference;
    typedef pointer iterator;
    //typedef ... const_reverse_iterator;
    //typedef const_reverse_iterator reverse_iterator;
    typedef typename ArrayRef<T>::size_type size_type;
    typedef typename ArrayRef<T>::difference_type difference_type;

    constexpr MutableArrayRef() noexcept : ArrayRef<T>() {}

    constexpr MutableArrayRef(MutableArrayRef const& source) noexcept /*= default*/
        : ArrayRef<T>(source)
    {}

    MutableArrayRef(value_type const& elem)
        : ArrayRef<T>(elem) {}

    constexpr MutableArrayRef(value_type* data, size_t length)
        : ArrayRef<T>(data, length) {}

    template<size_t N>
    constexpr MutableArrayRef(value_type (&array)[N])
        : ArrayRef<T>(array) {}

    MutableArrayRef(std::vector<T>& vector)
        : ArrayRef<T>(vector.data(), vector.size()) {}

    constexpr iterator begin() const noexcept { return data(); }
    constexpr iterator end()   const noexcept { return data() + this->length(); }

    constexpr pointer data() const noexcept
    {
      return const_cast<pointer>(ArrayRef<T>::data());
    }

    constexpr reference operator[](size_t pos) const
    {
        return ETK_ASSERT(pos < this->length() && "Index out or range"), data()[pos];
    }

    constexpr reference front() const
    {
        return ETK_ASSERT(!this->empty()), data()[0];
    }

    constexpr reference back() const
    {
        return ETK_ASSERT(!this->empty()), data()[this->length() - 1];
    }
};

typedef MutableArrayRef<unsigned char> BufferRef;

} // namespace etk
