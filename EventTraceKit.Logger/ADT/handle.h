#pragma once
#include "Support/Debug.h"
#include "Support/CompilerSupport.h"
#include <windows.h>

namespace etk
{

template<typename Traits>
class Handle
{
    typedef typename Traits::HandleType HandleType;

public:
    explicit Handle(HandleType handle = Traits::InvalidHandle()) noexcept
        : handle(handle)
    {
    }

    ~Handle() noexcept
    {
        Close();
    }

    Handle(Handle&& source) noexcept
        : handle(source.Detach())
    {
    }

    Handle& operator =(Handle&& source) noexcept
    {
        ETK_ASSERT(this != &source);
        Reset(source.Detach());
        return *this;
    }

    Handle(Handle const&) = delete;
    Handle& operator =(Handle const&) = delete;

    static HandleType InvalidHandle() noexcept
    {
        return Traits::InvalidHandle();
    }

    bool IsValid() const noexcept
    {
        return Traits::IsValid(handle);
    }

    void Close() noexcept
    {
        if (Traits::IsValid(handle)) {
            Traits::Close(handle);
            handle = Traits::InvalidHandle();
        }
    }

    bool Reset(HandleType handle = Traits::InvalidHandle()) noexcept
    {
        if (this->handle != handle) {
            Close();
            this->handle = handle;
        }

        return IsValid();
    }

    HandleType Detach() noexcept
    {
        HandleType h = handle;
        handle = Traits::InvalidHandle();
        return h;
    }

    Handle& operator =(HandleType handle)
    {
        Reset(handle);
        return *this;
    }

    explicit operator bool() const noexcept
    {
        return Traits::IsValid(handle);
    }

    HandleType Get() const noexcept
    {
        return handle;
    }

    operator HandleType() const noexcept
    {
        return handle;
    }

    HandleType* CloseAndGetAddressOf() noexcept
    {
        Close();
        handle = Traits::InvalidHandle();
        return &handle;
    }

private:
    HandleType handle;
};

struct HandleTraits
{
    typedef HANDLE HandleType;

    static HandleType InvalidHandle() noexcept { return INVALID_HANDLE_VALUE; }
    static bool IsValid(HandleType h) noexcept { return h != InvalidHandle(); }
    static void Close(HandleType h) noexcept   { ::CloseHandle(h); }
};

struct NullIsInvalidHandleTraits : HandleTraits
{
    static HandleType InvalidHandle() { return NULL; }
    static bool IsValid(HandleType h) noexcept { return h != InvalidHandle(); }
};

struct FileHandleTraits : HandleTraits {};
struct ProcessHandleTraits : NullIsInvalidHandleTraits {};
struct ThreadHandleTraits : NullIsInvalidHandleTraits {};
struct TimerHandleTraits : NullIsInvalidHandleTraits {};
struct TokenHandleTraits : NullIsInvalidHandleTraits {};

typedef Handle<FileHandleTraits> FileHandle;
typedef Handle<ProcessHandleTraits> ProcessHandle;
typedef Handle<ThreadHandleTraits> ThreadHandle;
typedef Handle<TimerHandleTraits> TimerHandle;
typedef Handle<TokenHandleTraits> TokenHandle;

/// Prevent accidentally calling CloseHandle(HANDLE) with a scoped handle.
/// Scoped handles must be closed by calling Close().
template<typename T>
void CloseHandle(Handle<T> const&) = delete;

} // namespace etk
