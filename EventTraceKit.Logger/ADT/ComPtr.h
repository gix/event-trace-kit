#pragma once
#include "ffmf/Common/ADT/AddRefed.h"
#include "ffmf/Common/Debug.h"
#include "ffmf/Common/Details/HideIUnknown.h"

#include <new>
#include <type_traits>
#include <utility>
#include <Unknwn.h>

namespace ffmf
{

#define COMPTR_PPV_ARGS(ptrRef) __uuidof(**(ptrRef)), (ptrRef)

template<typename T>
class ComPtrRef;

/// <summary>
///   COM reference-counting smart pointer.
/// </summary>
template<typename T>
class ComPtr
{
    template<typename TSource>
    using EnableConversion = std::enable_if_t<
        std::is_convertible<TSource*, T*>::value>;

public:
    typedef T InterfaceType;

    ComPtr() noexcept
        : ptr(nullptr)
    {
        Invariant();
    }

    /*implicit*/ ComPtr(ComPtr const& source) noexcept
        : ptr(source.Get())
    {
        Invariant();
        SafeAddRef();
    }

    /*implicit*/ ComPtr(ComPtr&& source) noexcept
        : ptr(source.Get())
    {
        Invariant();
        source.ptr = nullptr;
    }

    template<typename TSource, typename = EnableConversion<TSource>>
    /*implicit*/ ComPtr(ComPtr<TSource> const& source) noexcept
        : ptr(source.Get())
    {
        Invariant();
        SafeAddRef();
    }

    template<typename TSource, typename = EnableConversion<TSource>>
    /*implicit*/ ComPtr(ComPtr<TSource>&& source) noexcept
        : ptr(source.Get())
    {
        Invariant();
        source.ptr = nullptr;
    }

    /*implicit*/ ComPtr(AddRefed<T>&& source) noexcept
        : ptr(source.Detach())
    {
        Invariant();
        // No AddRef
    }

    template<typename TSource,
             typename std::enable_if_t<
               std::is_convertible<TSource*, T*>::value>* = nullptr>
    /*implicit*/ ComPtr(AddRefed<TSource>&& source) noexcept
        : ptr(source.Detach())
    {
        Invariant();
        // No AddRef
    }

    /*implicit*/ ComPtr(T* source, bool addRef = true) noexcept
        : ptr(source)
    {
        Invariant();
        if (addRef)
            SafeAddRef();
    }

    template<typename TSource, typename = EnableConversion<TSource>>
    /*implicit*/ ComPtr(TSource* source, bool addRef = true) noexcept
        : ptr(source)
    {
        Invariant();
        if (addRef)
            SafeAddRef();
    }

    ~ComPtr() noexcept
    {
        SafeRelease();
    }

    ComPtr& operator =(ComPtr const& source) noexcept
    {
        if (ptr != source.ptr)
            ComPtr(source).Swap(*this);
        return *this;
    }

    template<typename TSource, typename = EnableConversion<TSource>>
    ComPtr& operator =(ComPtr<TSource> const& source) noexcept
    {
        ComPtr(source).Swap(*this);
        return *this;
    }

    ComPtr& operator =(ComPtr&& source) noexcept
    {
        ComPtr(std::forward<ComPtr>(source)).Swap(*this);
        return *this;
    }

    template<typename TSource, typename = EnableConversion<TSource>>
    ComPtr& operator =(ComPtr<TSource>&& source) noexcept
    {
        ComPtr(std::forward<ComPtr<TSource>>(source)).Swap(*this);
        return *this;
    }

    ComPtr& operator =(AddRefed<T>&& source) noexcept
    {
        ComPtr(std::forward<AddRefed<T>>(source)).Swap(*this);
        return *this;
    }

    template<typename TSource, typename = EnableConversion<TSource>>
    ComPtr& operator =(AddRefed<TSource>&& source) noexcept
    {
        ComPtr(std::forward<AddRefed<TSource>>(source)).Swap(*this);
        return *this;
    }

    ComPtr& operator =(T* source) noexcept
    {
        ComPtr(source).Swap(*this);
        return *this;
    }

    T* Get() const noexcept
    {
        return ptr;
    }

    T& operator *() const
    {
        FFMF_ASSERT(ptr != nullptr);
        return *ptr;
    }

    typename Details::HideIUnknown<T>::Type* operator ->() const noexcept
    {
        FFMF_ASSERT(ptr != nullptr);
        return static_cast<typename Details::HideIUnknown<T>::Type*>(ptr);
    }

    operator T*() const noexcept
    {
        return ptr;
    }

    explicit operator bool() const noexcept
    {
        return ptr != nullptr;
    }

    ComPtrRef<T> operator &() noexcept
    {
        return ComPtrRef<T>(this);
    }

    ComPtrRef<T> const operator &() const noexcept
    {
        return ComPtrRef<T>(const_cast<ComPtr* const>(this));
    }

    /// <summary>
    ///   Releases the managed object by decrementing its reference count
    ///   and returns a pointer to the internal (now null'ed) pointer which
    ///   can be used to store a new object. Intended to be used when calling
    ///   functions that return ref-counted objects via T** argument.
    /// </summary>
    T** ReleaseAndGetAddressOf() noexcept
    {
        SafeRelease();
        ptr = nullptr;
        return &ptr;
    }

    /// <summary>
    ///   Releases any managed object and attaches the source object without
    ///   increasing its reference count. After attaching, the source object
    ///   is managed by this smart pointer.
    /// </summary>
    template<typename TSource>
    void Attach(TSource* source) noexcept
    {
        if (ptr != source) {
            SafeRelease();
            ptr = source;
        }
    }

    /// <summary>
    ///   Detaches the managed object without decreasing its reference count.
    ///   After detaching, the object is no longer managed by this smart
    ///   pointer.
    /// </summary>
    AddRefed<T> Detach() noexcept
    {
        return AddRefed<T>(std::exchange(ptr, nullptr));
    }

    /// <summary>
    ///   Detaches the managed object without decreasing its reference count.
    ///   After detaching, the object is no longer managed by this smart
    ///   pointer.
    /// </summary>
    T* UnsafeDetach() noexcept
    {
        return std::exchange(ptr, nullptr);
    }

    void Reset() noexcept
    {
        ComPtr().Swap(*this);
    }

    void Reset(T* source) noexcept
    {
        ComPtr(source).Swap(*this);
    }

    void Swap(ComPtr& source) noexcept
    {
        T* tmp = ptr;
        ptr = source.ptr;
        source.ptr = tmp;
    }

    /// <summary>
    ///   Increases the ref-count and returns the managed object.
    /// </summary>
    AddRefed<T> Copy() noexcept
    {
        SafeAddRef();
        return AddRefed<T>(ptr);
    }

    T* UnsafeCopy() noexcept
    {
        SafeAddRef();
        return ptr;
    }

    void CopyTo(T** target) const noexcept
    {
        FFMF_ASSERT(target != nullptr);
        SafeAddRef();
        *target = ptr;
    }

    template<typename TTarget>
    void MoveTo(TTarget** target) noexcept
    {
        FFMF_ASSERT(target != nullptr);
        *target = ptr;
        ptr = nullptr;
    }

    template<typename TBase>
    TBase* AsBase() const noexcept
    {
        static_assert(std::is_base_of<TBase, T>::value,
                      "AsBase() requires a base class of T.");
        return ptr;
    }

    template<typename TInterface>
    _Check_return_ HRESULT CopyTo(TInterface** target) const noexcept
    {
        return ptr->QueryInterface(
            __uuidof(TInterface),
            reinterpret_cast<void**>(target));
    }

    _Check_return_ HRESULT CopyTo(REFIID iid, void** target) const noexcept
    {
        return ptr->QueryInterface(iid, target);
    }

    template<typename TInterface>
    _Check_return_ HRESULT As(ComPtr<TInterface>* target) const noexcept
    {
        return ptr->QueryInterface(
            __uuidof(TInterface),
            reinterpret_cast<void**>(target->ReleaseAndGetAddressOf()));
    }

    template<typename TInterface>
    _Check_return_ HRESULT As(ComPtrRef<TInterface> target) const noexcept
    {
        return ptr->QueryInterface(__uuidof(TInterface), target);
    }

    /// Special version with explicit IID for exotic interfaces without
    /// attached UUID.
    template<typename TInterface>
    _Check_return_ HRESULT As(REFIID iid, ComPtrRef<TInterface> target) const noexcept
    {
        return ptr->QueryInterface(iid, target);
    }

    _Check_return_ HRESULT QueryFrom(_In_ IUnknown* object) noexcept
    {
        return object->QueryInterface(
            __uuidof(T),
            reinterpret_cast<void**>(ReleaseAndGetAddressOf()));
    }

    _Check_return_ HRESULT CreateInstance(
        _In_ REFCLSID clsid,
        _In_opt_ IUnknown* outer,
        _In_ DWORD clsContext = CLSCTX_INPROC_SERVER) noexcept
    {
        return CoCreateInstance(
            clsid, outer, clsContext, __uuidof(T),
            reinterpret_cast<void**>(ReleaseAndGetAddressOf()));
    }

    _Check_return_ HRESULT CreateInstance(
        _In_ REFCLSID clsid,
        _In_ DWORD clsContext = CLSCTX_INPROC_SERVER) noexcept
    {
        return CreateInstance(clsid, nullptr, clsContext);
    }

    template<typename TClass>
    _Check_return_ HRESULT CreateInstance(
        _In_opt_ IUnknown* outer,
        _In_ DWORD clsContext = CLSCTX_INPROC_SERVER) noexcept
    {
        return CreateInstance(__uuidof(TClass), outer, clsContext);
    }

    template<typename TClass>
    _Check_return_ HRESULT CreateInstance(
        _In_ DWORD clsContext = CLSCTX_INPROC_SERVER) noexcept
    {
        return CreateInstance(__uuidof(TClass), nullptr, clsContext);
    }

private:
    void SafeAddRef() const noexcept
    {
        if (ptr != nullptr)
            ptr->AddRef();
    }

    void SafeRelease() noexcept
    {
        if (ptr != nullptr) {
            FFMF_ASSERT_NO_EXCEPTION(ptr->Release());
            ptr = nullptr;
        }
    }

    static void Invariant() noexcept
    {
        static_assert(std::is_base_of<IUnknown, T>::value,
                      "T must be a COM type derived from IUnknown.");
    }

    T* ptr;

    // Make all ComPtr templates friends. This is needed for the template
    // move constructor.
    template<typename U>
    friend class ComPtr;

    friend class ComPtrRef<T>;
};


template<typename T, typename U>
bool operator ==(ComPtr<T> const& left, ComPtr<U> const& right) noexcept
{
    return left.Get() == right.Get();
}

template<typename T, typename U>
bool operator !=(ComPtr<T> const& left, ComPtr<U> const& right) noexcept
{
    return left.Get() != right.Get();
}

template<typename T, typename U>
bool operator ==(ComPtr<T> const& left, U* right) noexcept
{
    return left.Get() == right;
}

template<typename T, typename U>
bool operator !=(ComPtr<T> const& left, U* right) noexcept
{
    return left.Get() != right;
}

template<typename T, typename U>
bool operator ==(T* left, ComPtr<U> const& right) noexcept
{
    return left == right.Get();
}

template<typename T, typename U>
bool operator !=(T* left, ComPtr<U> const& right) noexcept
{
    return left != right.Get();
}

template<typename T>
void swap(ComPtr<T>& left, ComPtr<T>& right) noexcept
{
    left.Swap(right);
}


template<typename T>
class ComPtrRef
{
public:
    typedef T InterfaceType;

    ComPtrRef(ComPtr<InterfaceType>* ptr) noexcept
        : ptr(ptr)
    {
    }

    InterfaceType** ReleaseAndGetAddressOf() noexcept
    {
        return ptr->ReleaseAndGetAddressOf();
    }

    /// <devdoc>
    ///   Allows ComPtr to be used in place of an input array.
    ///   void Foo(T* const* values, size_t count);
    ///   ComPtr<T> p;
    ///   Foo(&p, 1);
    /// </devdoc>
    operator InterfaceType* const*() noexcept
    {
        return &(ptr->ptr);
    }

    operator ComPtr<InterfaceType>*() noexcept
    {
        // TODO: Why should this null the ComPtr?
        *ptr = nullptr;
        return ptr;
    }

    operator InterfaceType**() noexcept
    {
        return ptr->ReleaseAndGetAddressOf();
    }

    operator void**() noexcept
    {
        return reinterpret_cast<void**>(ptr->ReleaseAndGetAddressOf());
    }

    operator IUnknown**() const noexcept
    {
        static_assert(std::is_base_of<IUnknown, InterfaceType>::value,
                      "Invalid conversion: InterfaceType does not derive from IUnknown");
        return reinterpret_cast<IUnknown**>(ptr->ReleaseAndGetAddressOf());
    }

    /// <devdoc>
    ///   Allows ComPtr to be used in <c>__uuidof(**(ppType))</c>
    ///   expressions.
    /// </devdoc>
    InterfaceType* operator *() noexcept
    {
        return ptr->Get();
    }

private:
    ComPtr<InterfaceType>* ptr;
};

namespace Details
{
template<typename T, typename U = void>
using is_com_interface = std::enable_if_t<std::is_base_of<IUnknown, T>::value, U>;
} // namespace Details

template<typename From>
class ComPtrAutoQI
{
public:
    template<typename To>
    operator ComPtr<To>() noexcept
    {
        ComPtr<To> out;
        if (ptr)
            ptr->QueryInterface(__uuidof(To), &out);
        return out;
    }

    template<typename F>
    friend ComPtrAutoQI<F> qi_autocast(_In_ ComPtr<F>& ptr) noexcept;

    template<typename F>
    friend Details::is_com_interface<F, ComPtrAutoQI<F>>
    qi_autocast(_In_opt_ F* ptr) noexcept;

private:
    ComPtrAutoQI(_In_opt_ From* ptr)
        : ptr(ptr)
    {
    }

    From* ptr;
};

template<typename From>
inline ComPtrAutoQI<From> qi_autocast(_In_ ComPtr<From>& ptr) noexcept
{
    return { ptr.Get() };
}

template<typename From>
inline Details::is_com_interface<From, ComPtrAutoQI<From>>
qi_autocast(_In_opt_ From* ptr) noexcept
{
    return { ptr };
}

template<typename To, typename From>
inline ComPtr<To> qi_cast(_In_ ComPtr<From>& ptr) noexcept
{
    ComPtr<To> out;
    (void)ptr.As(&out);
    return out;
}

template<typename To, typename From,
         typename = std::enable_if_t<std::is_base_of<IUnknown, To>::value &&
                                     std::is_base_of<IUnknown, From>::value>>
inline ComPtr<To> qi_cast(_In_opt_ From* ptr) noexcept
{
    ComPtr<To> out;
    if (ptr)
        ptr->QueryInterface(__uuidof(To), &out);
    return out;
}

/// <summary>
///   Constructs a COM object of type <typeparamref name="T"/> and wraps it in a
///   <see cref="ComPtr"/>.
/// </summary>
/// <remarks>
///   <typeparamref name="T"/> must be constructed with a reference count of 1.
/// </remarks>
template<typename T, typename... Args>
inline std::enable_if_t<!std::is_array<T>::value, ComPtr<T>>
  make_com(Args&&... args) noexcept
{
    return ComPtr<T>(new(std::nothrow) T(std::forward<Args>(args)...), false);
}

} // namespace ffmf
