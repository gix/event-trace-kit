#pragma once
#include "Support/Debug.h"
#include "Support/CompilerSupport.h"

#include <cstddef>
#include <memory>
#include <type_traits>
#include <boost/compressed_pair.hpp>
#include <objbase.h>
#include <windows.h>


namespace etk
{

namespace Details
{

namespace has_pointer_type_imp
{
    struct two { char a, b; };
    template<typename T> static two test(...);
    template<typename T> static char test(typename T::pointer* = nullptr);
}

template<typename T>
struct has_pointer_type
    : public std::integral_constant<bool, sizeof(has_pointer_type_imp::test<T>(0)) == 1>
{
};

namespace pointer_type_imp
{
    template<typename T, typename D, bool = has_pointer_type<D>::value>
    struct pointer_type
    {
        typedef typename D::pointer type;
    };

    template<typename T, typename D>
    struct pointer_type<T, D, false>
    {
        typedef T* type;
    };
} // namespace pointer_type_imp

template<typename T, typename D>
using pointer_type =
    typename pointer_type_imp::pointer_type<T, std::remove_reference_t<D>>::type;

} // namespace Details


template<typename T>
struct ResPtrDeleter
    : public std::default_delete<T>
{
};

template<typename T, typename Traits>
class ResPtrRef;

template<typename T, typename D = ResPtrDeleter<T>>
class ResPtr
{
public:
    typedef T element_type;
    typedef D deleter_type;
    typedef Details::pointer_type<T, D> pointer;

    typedef std::remove_reference_t<deleter_type> deleter_nonref_type;

    typedef ResPtrRef<T, D> reference_type;
    typedef ResPtrRef<T const, D> const_reference_type;

    ETK_ALWAYS_INLINE
    constexpr ResPtr() noexcept
        : ptr(pointer())
    {
        static_assert(!std::is_pointer<deleter_type>::value,
                      "ResPtr constructed with null function pointer deleter");
    }

    explicit ResPtr(pointer ptr) noexcept
        : ptr(ptr)
    {
        static_assert(!std::is_pointer<deleter_type>::value,
                      "ResPtr constructed with null function pointer deleter");
    }

    ETK_ALWAYS_INLINE
    constexpr ResPtr(std::nullptr_t) noexcept
        : ptr(pointer())
    {
        static_assert(!std::is_pointer<deleter_type>::value,
                      "ResPtr constructed with null function deleter pointer");
    }

    ResPtr(pointer ptr,
           std::conditional_t<
               std::is_reference<deleter_type>::value,
               deleter_type,
               std::add_lvalue_reference_t<deleter_type const>
           > deleter) noexcept
        : ptr(ptr, deleter)
    {
    }

    ResPtr(pointer ptr, std::remove_reference_t<deleter_type>&& deleter) noexcept
        : ptr(ptr, std::move(deleter))
    {
    }

    ~ResPtr() noexcept
    {
        Delete();
    }

    ResPtr(ResPtr const&) = delete;
    ResPtr& operator =(ResPtr const&) = delete;

    ResPtr(ResPtr&& source) noexcept
        : ptr(source.Release(), std::forward<deleter_type>(source.GetDeleter()))
    {
    }

    template<typename U, typename E>
    ResPtr(ResPtr<U, E>&& source,
           std::enable_if_t<
               !std::is_array<U>::value &&
                 std::is_convertible<typename ResPtr<U, E>::pointer, pointer>::value &&
                 ((std::is_reference<D>::value && std::is_same<D, E>::value) ||
                 (!std::is_reference<D>::value && std::is_convertible<E, D>::value)),
               void
           >** = nullptr) noexcept
        : ptr(source.Release(), std::forward<E>(source.GetDeleter()))
    {
    }

    template<typename U, typename E>
    std::enable_if_t<
        !std::is_array<U>::value &&
          std::is_convertible<typename ResPtr<U, E>::pointer, pointer>::value,
        ResPtr&
    >
      operator =(ResPtr<U, E>&& source) noexcept
    {
        Reset(source.Release());
        this->GetDeleter() = std::forward<E>(source.GetDeleter());
        return *this;
    }

    ResPtr& operator =(ResPtr&& source) noexcept
    {
        if (this != &source) {
            Reset(source.Release());
            this->GetDeleter() = std::forward<D>(source.GetDeleter());
        }
        return *this;
    }

    ResPtr& operator =(std::nullptr_t) noexcept
    {
        Reset();
        return *this;
    }

    bool operator ==(std::nullptr_t) const noexcept
    {
        return ptr.first() == nullptr;
    }

    bool operator !=(std::nullptr_t) const noexcept
    {
        return ptr.first() != nullptr;
    }

    pointer Get() const noexcept
    {
        return ptr.first();
    }

    std::add_lvalue_reference_t<T> operator *() const
    {
        ETK_ASSERT(ptr.first() != nullptr);
        return *(ptr.first());
    }

    pointer operator ->() const noexcept
    {
        ETK_ASSERT(ptr.first() != nullptr);
        return std::pointer_traits<pointer>::pointer_to(**this);
    }

    operator pointer() const noexcept
    {
        return ptr.first();
    }

    explicit operator bool() const noexcept
    {
        return ptr.first() != nullptr;
    }

    reference_type operator &() noexcept
    {
        return reference_type(this);
    }

    const_reference_type operator &() const noexcept
    {
        return const_reference_type(this);
    }

    pointer Release() noexcept
    {
        pointer ret = ptr.first();
        ptr.first() = pointer();
        return ret;
    }

    void Reset(pointer ptr = pointer()) noexcept
    {
        if (ptr != this->ptr.first()) {
            Delete();
            this->ptr.first() = ptr;
        }
    }

    pointer* UnsafeGetAddressOf()
    {
        return &(ptr.first());
    }

    pointer* ResetAndGetAddressOf()
    {
        Reset();
        return &(ptr.first());
    }

    void Swap(ResPtr& right) noexcept
    {
        using std::swap;
        swap(ptr.first(), right.ptr.first());
        swap(GetDeleter(), right.GetDeleter());
    }

    deleter_nonref_type& GetDeleter() noexcept
    {
        return ptr.second();
    }

    deleter_nonref_type const& GetDeleter() const noexcept
    {
        return ptr.second();
    }

private:
    void Delete()
    {
        if (ptr.first() != pointer())
            this->GetDeleter()(ptr.first());
    }

    boost::compressed_pair<pointer, deleter_type> ptr;
};


template<typename T, typename D>
class ResPtr<T[], D>
{
public:
    typedef T element_type;
    typedef D deleter_type;
    typedef Details::pointer_type<T, D> pointer;

    typedef std::remove_reference_t<deleter_type> deleter_nonref_type;

    typedef ResPtrRef<T[], D> reference_type;
    typedef ResPtrRef<T const[], D> const_reference_type;

    ETK_ALWAYS_INLINE
    constexpr ResPtr() noexcept
        : ptr(pointer())
    {
        static_assert(!std::is_pointer<deleter_type>::value,
                      "ResPtr constructed with null function pointer deleter");
    }

    explicit ResPtr(pointer ptr) noexcept
        : ptr(ptr)
    {
        static_assert(!std::is_pointer<deleter_type>::value,
                      "ResPtr constructed with null function pointer deleter");
    }

    ETK_ALWAYS_INLINE
    constexpr ResPtr(std::nullptr_t) noexcept
        : ptr(pointer())
    {
        static_assert(!std::is_pointer<deleter_type>::value,
                      "ResPtr constructed with null function pointer deleter");
    }

    ResPtr(pointer ptr,
           std::conditional_t<
               std::is_reference<deleter_type>::value,
               deleter_type,
               std::add_lvalue_reference_t<deleter_type> const&
           > deleter) noexcept
        : ptr(ptr, deleter)
    {
    }

    ResPtr(pointer ptr, std::remove_reference_t<deleter_type>&& deleter) noexcept
        : ptr(ptr, std::move(deleter))
    {
    }

    ~ResPtr() noexcept
    {
        Delete();
    }

    ResPtr(ResPtr&& source) noexcept
        : ptr(source.Release(), std::forward<deleter_type>(source.GetDeleter()))
    {
    }

    ResPtr& operator =(ResPtr&& source) noexcept
    {
        if (this != &source) {
            Reset(source.Release());
            GetDeleter() = std::move(source.GetDeleter());
        }
        return *this;
    }

    ResPtr& operator =(std::nullptr_t) noexcept
    {
        Reset();
        return *this;
    }

    bool operator ==(std::nullptr_t) const noexcept
    {
        return ptr.first() == nullptr;
    }

    bool operator !=(std::nullptr_t) const noexcept
    {
        return ptr.first() != nullptr;
    }

    pointer Get() const noexcept
    {
        return ptr.first();
    }

    std::add_lvalue_reference_t<T> operator [](size_t index) const
    {
        return ptr.first()[index];
    }

    explicit operator bool() const noexcept
    {
        return ptr.first() != nullptr;
    }

    reference_type operator &() noexcept
    {
        return reference_type(this);
    }

    const_reference_type const operator &() const noexcept
    {
        return const_reference_type(this);
    }

    pointer Release() noexcept
    {
        pointer ret = ptr.first();
        ptr.first() = pointer();
        return ret;
    }

    void Reset(pointer ptr = pointer()) noexcept
    {
        if (ptr != this->ptr.first()) {
            Delete();
            this->ptr.first() = ptr;
        }
    }

    void Reset(std::nullptr_t) noexcept
    {
        if (ptr.first() != nullptr) {
            Delete();
            ptr.first() = nullptr;
        }
    }

    pointer* UnsafeGetAddressOf()
    {
        return &(ptr.first());
    }

    pointer* ResetAndGetAddressOf()
    {
        Reset();
        return &(ptr.first());
    }

    void Swap(ResPtr& right) noexcept
    {
        using std::swap;
        swap(ptr.first(), right.ptr);
        swap(this->GetDeleter(), right.GetDeleter());
    }

    deleter_nonref_type& GetDeleter() noexcept
    {
        return ptr.second();
    }

    deleter_nonref_type const& GetDeleter() const noexcept
    {
        return ptr.second();
    }

    ResPtr(ResPtr const&) = delete;
    ResPtr& operator =(ResPtr const&) = delete;

    template<typename U>
    void Reset(U) = delete;

private:
    void Delete()
    {
        GetDeleter()(ptr.first());
    }

    boost::compressed_pair<pointer, deleter_type> ptr;
};

template<typename T, typename D>
void swap(ResPtr<T, D>& left, ResPtr<T, D>& right) noexcept
{
    left.Swap(right);
}

template<typename T1, typename D1, typename T2, typename D2>
bool operator ==(ResPtr<T1, D1> const& left, ResPtr<T2, T2> const& right)
{
    return left.get() == right.get();
}

template<typename T1, typename D1, typename T2, typename D2>
bool operator !=(ResPtr<T1, D1> const& left, ResPtr<T2, T2> const& right)
{
    return !(left == right);
}

template<typename T1, typename D1, typename T2, typename D2>
bool operator <(ResPtr<T1, D1> const& left, ResPtr<T2, T2> const& right)
{
    typedef typename ResPtr<T1, D1>::pointer P1;
    typedef typename ResPtr<T2, D2>::pointer P2;
    typedef typename std::common_type<P1, P2>::type C;
    return std::less<C>()(left.Get(), right.Get());
}

template<typename T1, typename D1, typename T2, typename D2>
  bool operator >=(ResPtr<T1, D1> const& left, ResPtr<T2, T2> const& right)
{
    return !(left < right);
}

template<typename T1, typename D1, typename T2, typename D2>
bool operator >(ResPtr<T1, D1> const& left, ResPtr<T2, T2> const& right)
{
    return right < left;
}

template<typename T1, typename D1, typename T2, typename D2>
bool operator <=(ResPtr<T1, D1> const& left, ResPtr<T2, T2> const& right)
{
    return !(right < left);
}


template<typename T, typename D>
class ResPtrRef
{
    typedef typename ResPtr<T, D>::pointer pointer;

public:
    ResPtrRef(ResPtr<T, D>* ptr) : ptr(ptr) { }

    pointer* ResetAndGetAddressOf()
    {
        return ptr->ResetAndGetAddressOf();
    }

    operator pointer*()
    {
        return ptr->ResetAndGetAddressOf();
    }

    operator void**()
    {
        return reinterpret_cast<void**>(ptr->ResetAndGetAddressOf());
    }

    pointer operator *() { return ptr->Get(); }
    operator ResPtr<T, D>*() { return ptr; }

private:
    ResPtr<T, D>* ptr;
};


template<typename T, typename D>
class ResPtrRef<T const, D>
{
public:
    ResPtrRef(ResPtr<T, D> const* ptr) : ptr(ptr) { }

    T* operator *() { return ptr->Get(); }
    operator ResPtr<T, D>*() { return ptr; }

private:
    ResPtr<T, D> const* ptr;
};


struct CoTaskMemFreeDeleter
{
    void operator ()(void* p) const { CoTaskMemFree(p); }
};

template<typename T>
class CoTaskMemPtr
    : public ResPtr<T, CoTaskMemFreeDeleter>
{
    typedef ResPtr<T, CoTaskMemFreeDeleter> base;
public:
    typedef typename base::element_type element_type;
    typedef typename base::pointer pointer;

    CoTaskMemPtr()
        : base() { }

    CoTaskMemPtr(pointer ptr)
        : base(ptr) { }

    CoTaskMemPtr(CoTaskMemPtr&& source) noexcept
        : base(std::move(source))
    {
    }

    CoTaskMemPtr& operator =(CoTaskMemPtr&& source) noexcept
    {
        base::operator=(std::move(source));
        return *this;
    }

    operator pointer() const noexcept
    {
        return this->Get();
    }

    template<typename I>
    std::add_lvalue_reference_t<element_type>& operator [](I index)
    {
        return this->Get()[index];
    }
};


struct LocalFreeDeleter
{
    void operator ()(void* p) const
    {
        LocalFree(reinterpret_cast<HLOCAL>(p));
    }
};

template<typename T>
class LocalMemPtr
    : public ResPtr<T, LocalFreeDeleter>
{
    typedef ResPtr<T, LocalFreeDeleter> base;
public:
    typedef typename base::pointer pointer;

    LocalMemPtr(pointer ptr = nullptr)
        : base(ptr) { }
};

} // namespace etk
