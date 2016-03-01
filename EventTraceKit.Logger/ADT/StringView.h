#pragma once
#include "Support/CompilerSupport.h"
#include "Support/Debug.h"

#include <algorithm>
#include <cassert>
#include <limits>
#include <string>

namespace etk
{

//! Partial implementation of basic_string_view according to N3921.
template<typename CharT, typename Traits = std::char_traits<CharT>>
class basic_string_view
{
public:
    typedef Traits traits_type;
    typedef CharT value_type;
    typedef CharT* pointer;
    typedef CharT const* const_pointer;
    typedef CharT& reference;
    typedef CharT const& const_reference;
    typedef const_pointer const_iterator;
    typedef const_iterator iterator;
    typedef std::reverse_iterator<const_iterator> const_reverse_iterator;
    typedef const_reverse_iterator reverse_iterator;
    typedef size_t size_type;
    typedef ptrdiff_t difference_type;
    static constexpr size_type const npos = size_type(-1);

    // [string.view.cons], construct/copy

    ETK_ALWAYS_INLINE
    constexpr basic_string_view() noexcept
        : data_(nullptr), length_(0) {}

    ETK_ALWAYS_INLINE
    constexpr basic_string_view(basic_string_view const& source) noexcept = default;

    ETK_ALWAYS_INLINE
    basic_string_view& operator=(basic_string_view const& source) noexcept = default;

    template<typename Allocator>
    ETK_ALWAYS_INLINE
    basic_string_view(std::basic_string<CharT, Traits, Allocator> const& str) noexcept
        : data_(str.data()), length_(str.length())
    {
    }

    ETK_ALWAYS_INLINE
    constexpr basic_string_view(CharT const* str)
        : data_(str), length_(traits_type::length(str))
    {
    }

    ETK_ALWAYS_INLINE
    constexpr basic_string_view(CharT const* str, size_type length)
        : data_(str), length_(length)
    {
    }

    // [string.view.iterators], iterators
    ETK_ALWAYS_INLINE constexpr const_iterator begin()  const noexcept { return cbegin(); }
    ETK_ALWAYS_INLINE constexpr const_iterator end()    const noexcept { return cend(); }
    ETK_ALWAYS_INLINE constexpr const_iterator cbegin() const noexcept { return data_; }
    ETK_ALWAYS_INLINE constexpr const_iterator cend()   const noexcept { return data_ + length_; }

    ETK_ALWAYS_INLINE const_reverse_iterator rbegin()  const noexcept { return const_reverse_iterator(cend()); }
    ETK_ALWAYS_INLINE const_reverse_iterator rend()    const noexcept { return const_reverse_iterator(cbegin()); }
    ETK_ALWAYS_INLINE const_reverse_iterator crbegin() const noexcept { return const_reverse_iterator(cend()); }
    ETK_ALWAYS_INLINE const_reverse_iterator crend()   const noexcept { return const_reverse_iterator(cbegin()); }

    // [string.view.capacity], capacity
    ETK_ALWAYS_INLINE constexpr size_type size()   const noexcept { return length_; }
    ETK_ALWAYS_INLINE constexpr size_type length() const noexcept { return length_; }

    ETK_ALWAYS_INLINE constexpr size_type max_size() const noexcept
    {
        return std::numeric_limits<size_type>::max();
    }

    ETK_ALWAYS_INLINE constexpr bool empty() const noexcept { return length_ == 0; }

    // [string.view.access], element access

    ETK_ALWAYS_INLINE
    constexpr const_reference operator [](size_type pos) const
    {
        return ETK_ASSERT_MSG(pos < length(), "Index out or range"), data_[pos];
    }

    constexpr const_reference at(size_type pos) const
    {
        return pos >= length()
            ? throw std::out_of_range("string_view::at: pos >= length")
            : data_[pos];
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

    // [string.view.modifiers], modifiers

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
    void swap(basic_string_view& s) noexcept
    {
        value_type const* d = data_;
        data_ = s.data_;
        s.data_ = d;

        size_type l = length_;
        length_ = s.length_;
        s.length_ = l;
    }

    // [string.view.ops], string operations

    template<typename Allocator>
    ETK_ALWAYS_INLINE
    explicit operator std::basic_string<CharT, Traits, Allocator>() const
    {
        return std::basic_string<CharT, Traits, Allocator>(begin(), end());
    }

    template<typename Allocator = std::allocator<CharT>>
    ETK_ALWAYS_INLINE
    std::basic_string<CharT, Traits, Allocator> to_string(Allocator const& a = Allocator()) const
    {
        return std::basic_string<CharT, Traits, Allocator>(begin(), end(), a);
    }

    size_type copy(CharT* s, size_type n, size_type pos = 0) const
    {
        if (pos > length())
            throw std::out_of_range("string_view::copy(): pos > length");
        size_type rlen = std::min(n, length() - pos);
        std::copy_n(begin() + pos, rlen, s);
        return rlen;
    }

    constexpr basic_string_view substr(size_type pos = 0, size_type n = npos) const
    {
        return pos > length()
            ? throw std::out_of_range("string_view::substr(): pos > length")
            : basic_string_view(data() + pos, std::min(n, length() - pos));
    }

    ETK_CXX14_CONSTEXPR int compare(basic_string_view s) const noexcept
    {
        size_type rlen = std::min(length(), s.length());
        int cmp = traits_type::compare(data(), s.data(), rlen);
        if (cmp == 0) {
            if (length() < s.length()) cmp = -1;
            else if (length() > s.length()) cmp = +1;
        }
        return cmp;
    }

    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
    int compare(size_type pos1, size_type n1, basic_string_view s) const
    {
        return substr(pos1, n1).compare(s);
    }

    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
    int compare(size_type pos1, size_type n1, basic_string_view s,
                size_type pos2, size_type n2) const
    {
        return substr(pos1, n1).compare(s.substr(pos2, n2));
    }

    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
    int compare(CharT const* s) const
    {
        return compare(basic_string_view(s));
    }

    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
    int compare(size_type pos1, size_type n1, CharT const* s) const
    {
        return substr(pos1, n1).compare(basic_string_view(s));
    }

    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
    int compare(size_type pos1, size_type n1, CharT const* s, size_type n2) const
    {
        return substr(pos1, n1).compare(basic_string_view(s, n2));
    }

    ETK_CXX14_CONSTEXPR
    size_type find(basic_string_view s, size_type pos = 0) const noexcept;

    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
    size_type find(CharT c, size_type pos = 0) const noexcept
    {
        return str_find(data(), length(), c, pos);
    }

    ETK_CXX14_CONSTEXPR size_type find(CharT const* s, size_type pos, size_type n) const;
    ETK_CXX14_CONSTEXPR size_type find(CharT const* s, size_type pos = 0) const;
    ETK_CXX14_CONSTEXPR size_type rfind(basic_string_view s, size_type pos = npos) const noexcept;

    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
    size_type rfind(CharT c, size_type pos = npos) const noexcept
    {
        return str_rfind(data(), length(), c, pos);
    }

    ETK_CXX14_CONSTEXPR size_type rfind(CharT const* s, size_type pos, size_type n) const;
    ETK_CXX14_CONSTEXPR size_type rfind(CharT const* s, size_type pos = npos) const;
    ETK_CXX14_CONSTEXPR size_type find_first_of(basic_string_view s, size_type pos = 0) const noexcept;
    ETK_CXX14_CONSTEXPR size_type find_first_of(CharT c, size_type pos = 0) const noexcept;
    ETK_CXX14_CONSTEXPR size_type find_first_of(CharT const* s, size_type pos, size_type n) const;
    ETK_CXX14_CONSTEXPR size_type find_first_of(CharT const* s, size_type pos = 0) const;
    ETK_CXX14_CONSTEXPR size_type find_last_of(basic_string_view s, size_type pos = npos) const noexcept;
    ETK_CXX14_CONSTEXPR size_type find_last_of(CharT c, size_type pos = npos) const noexcept;
    ETK_CXX14_CONSTEXPR size_type find_last_of(CharT const* s, size_type pos, size_type n) const;
    ETK_CXX14_CONSTEXPR size_type find_last_of(CharT const* s, size_type pos = npos) const;
    ETK_CXX14_CONSTEXPR size_type find_first_not_of(basic_string_view s, size_type pos = 0) const noexcept;
    ETK_CXX14_CONSTEXPR size_type find_first_not_of(CharT c, size_type pos = 0) const noexcept;
    ETK_CXX14_CONSTEXPR size_type find_first_not_of(CharT const* s, size_type pos, size_type n) const;
    ETK_CXX14_CONSTEXPR size_type find_first_not_of(CharT const* s, size_type pos = 0) const;
    ETK_CXX14_CONSTEXPR size_type find_last_not_of(basic_string_view s, size_type pos = npos) const noexcept;
    ETK_CXX14_CONSTEXPR size_type find_last_not_of(CharT c, size_type pos = npos) const noexcept;
    ETK_CXX14_CONSTEXPR size_type find_last_not_of(CharT const* s, size_type pos, size_type n) const;
    ETK_CXX14_CONSTEXPR size_type find_last_not_of(CharT const* s, size_type pos = npos) const;

private:
    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
    static size_type str_find(
        CharT const* ptr, size_type len, CharT c, size_type pos = 0) noexcept
    {
        if (pos >= len)
            return npos;

        CharT const* elem = traits_type::find(ptr + pos, len - pos, c);
        if (elem == nullptr)
            return npos;

        return static_cast<size_t>(elem - ptr);
    }

    ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
    static size_type str_rfind(
        CharT const* ptr, size_type len, CharT c, size_type pos) noexcept
    {
        if (len < 1)
            return npos;

        if (pos < len)
            ++pos;
        else
            pos = len;

        for (CharT const* ps = ptr + pos; ps != ptr;) {
            if (traits_type::eq(*--ps, c))
                return static_cast<size_type>(ps - ptr);
        }

        return npos;
    }

    const_pointer data_;
    size_type length_;
};


// [string.view.comparison]

// operator ==

template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator ==(basic_string_view<CharT, Traits> lhs,
                 basic_string_view<CharT, Traits> rhs) noexcept
{
    return lhs.length() == rhs.length() && lhs.compare(rhs) == 0;
}

#ifndef ETK_MSVC
template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator ==(basic_string_view<CharT, Traits> lhs,
                 std::common_type_t<basic_string_view<CharT, Traits>> rhs) noexcept
{
    return lhs.length() == rhs.length() && lhs.compare(rhs) == 0;
}

template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator ==(std::common_type_t<basic_string_view<CharT, Traits>> lhs,
                 basic_string_view<CharT, Traits> rhs) noexcept
{
    return lhs.length() == rhs.length() && lhs.compare(rhs) == 0;
}
#endif


// operator !=

template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator !=(basic_string_view<CharT, Traits> lhs,
                 basic_string_view<CharT, Traits> rhs) noexcept
{
    return !(lhs == rhs);
}

#ifndef ETK_MSVC
template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator !=(basic_string_view<CharT, Traits> lhs,
                 std::common_type_t<basic_string_view<CharT, Traits>> rhs) noexcept
{
    return lhs.length() != rhs.length() || lhs.compare(rhs) != 0;
}

template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator !=(std::common_type_t<basic_string_view<CharT, Traits>> lhs,
                 basic_string_view<CharT, Traits> rhs) noexcept
{
    return lhs.length() != rhs.length() || lhs.compare(rhs) != 0;
}
#endif


// operator <

template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator <(basic_string_view<CharT, Traits> lhs,
                basic_string_view<CharT, Traits> rhs) noexcept
{
    return lhs.compare(rhs) < 0;
}

#ifndef ETK_MSVC
template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator <(basic_string_view<CharT, Traits> lhs,
                std::common_type_t<basic_string_view<CharT, Traits>> rhs) noexcept
{
    return lhs.compare(rhs) < 0;
}

template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator <(std::common_type_t<basic_string_view<CharT, Traits>> lhs,
                basic_string_view<CharT, Traits> rhs) noexcept
{
    return lhs.compare(rhs) < 0;
}
#endif


// operator >

template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator >(basic_string_view<CharT, Traits> lhs,
                basic_string_view<CharT, Traits> rhs) noexcept
{
    return lhs.compare(rhs) > 0;
}

#ifndef ETK_MSVC
template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator >(basic_string_view<CharT, Traits> lhs,
                std::common_type_t<basic_string_view<CharT, Traits>> rhs) noexcept
{
    return lhs.compare(rhs) > 0;
}

template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator >(std::common_type_t<basic_string_view<CharT, Traits>> lhs,
                basic_string_view<CharT, Traits> rhs) noexcept
{
    return lhs.compare(rhs) > 0;
}
#endif


// operator <=

template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator <=(basic_string_view<CharT, Traits> lhs,
                 basic_string_view<CharT, Traits> rhs) noexcept
{
    return lhs.compare(rhs) <= 0;
}

#ifndef ETK_MSVC
template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator <=(basic_string_view<CharT, Traits> lhs,
                 std::common_type_t<basic_string_view<CharT, Traits>> rhs) noexcept
{
    return lhs.compare(rhs) <= 0;
}

template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator <=(std::common_type_t<basic_string_view<CharT, Traits>> lhs,
                 basic_string_view<CharT, Traits> rhs) noexcept
{
    return lhs.compare(rhs) <= 0;
}
#endif

// operator >=

template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator >=(basic_string_view<CharT, Traits> lhs,
                 basic_string_view<CharT, Traits> rhs) noexcept
{
    return lhs.compare(rhs) >= 0;
}

#ifndef ETK_MSVC
template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator >=(basic_string_view<CharT, Traits> lhs,
                 std::common_type_t<basic_string_view<CharT, Traits>> rhs) noexcept
{
    return lhs.compare(rhs) >= 0;
}

template<typename CharT, typename Traits>
ETK_CXX14_CONSTEXPR ETK_ALWAYS_INLINE
bool operator >=(std::common_type_t<basic_string_view<CharT, Traits>> lhs,
                 basic_string_view<CharT, Traits> rhs) noexcept
{
    return lhs.compare(rhs) >= 0;
}
#endif


typedef basic_string_view<char>     string_view;
typedef basic_string_view<char16_t> u16string_view;
typedef basic_string_view<char32_t> u32string_view;
typedef basic_string_view<wchar_t>  wstring_view;

} // namespace etk
