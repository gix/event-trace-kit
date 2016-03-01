#pragma once
#include "ffmf/Common/Debug.h"
#include "ffmf/Common/Support/CompilerConfig.h"
#include <utility>

namespace ffmf
{

template<typename T>
class ComPtr;

/// <summary>
///   Indicates that a reference-counted pointer is a strong reference and
///   had its reference count increased for the caller. The caller must
///   release the pointer when done. To use the contained pointer, either
///   assign it to a proper ref-counted smart pointer (e.g.
///   <see cref="ComPtr"/>), or call the unsafe <see cref="Detach"/> method.
/// </summary>
/// <remarks>
///   Should be used as a return type from a function that produces an already
///   AddRef'ed pointer as a result.
/// </remarks>
template<typename T>
class AddRefed
{
public:
    explicit AddRefed(T* ptr) noexcept
        : ptr(ptr)
    {
    }

    ~AddRefed()
    {
        FFMF_ASSERT_MSG(ptr == nullptr, "Leaked reference counted object");
    }

    AddRefed(AddRefed&& source) noexcept
        : ptr(std::exchange(source.ptr, nullptr))
    {
    }

    AddRefed& operator =(AddRefed&& source) noexcept
    {
        if (this != source)
            ptr = std::exchange(source.ptr, nullptr);
        return *this;
    }

    AddRefed(AddRefed const& source) = delete;
    AddRefed& operator =(AddRefed const& source) = delete;

    /// <summary>
    ///   Automatic conversion to base class pointer.
    /// </summary>
    /// <remarks>
    ///   Allows to use a RefPtr of a derived type and return a RefPtr to
    ///   base type. Instead of writing:
    ///   <code>
    ///     AddRefed&lt;Base> Create() {
    ///         RefPtr&lt;BaseType> b = d.Detach();
    ///         // ...
    ///         return b.Detach();
    ///     }
    ///   </code>
    ///   the conversion allows the following:
    ///   <code>
    ///     AddRefed&lt;Base> Create() {
    ///         RefPtr&lt;Derived> b = d.Detach();
    ///         // ...
    ///         return x.Detach();
    ///     }
    ///   </code>
    /// </remarks>
    template<typename U>
    operator AddRefed<U>() const noexcept
    {
        return AddRefed<U>(std::exchange(ptr, nullptr));
    }

    T* Detach() const noexcept { return std::exchange(ptr, nullptr); }

private:
    mutable T* ptr;
};

template<typename T>
inline AddRefed<T> SkipAddRef(T* ptr) noexcept
{
    return AddRefed<T>(ptr);
}

template<typename T>
inline AddRefed<T> SkipAddRef(AddRefed<T> const addRefedPtr) noexcept
{
    return addRefedPtr;
}

} // namespace ffmf
