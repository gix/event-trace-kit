#pragma once
#include "Support/Debug.h"
#include "Support/CompilerSupport.h"
#include <utility>
#include <windows.h>

namespace etk
{

template<typename Traits>
class Handle
{
    using HandleType = typename Traits::HandleType;

public:
    constexpr Handle() noexcept
        : handle(Traits::InvalidHandle())
    {
    }

    constexpr explicit Handle(HandleType handle) noexcept
        : handle(handle)
    {
    }

    ~Handle() noexcept
    {
        Close();
    }

    constexpr Handle(Handle&& source) noexcept
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

    constexpr static HandleType InvalidHandle() noexcept
    {
        return Traits::InvalidHandle();
    }

    constexpr bool IsValid() const noexcept
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
        return std::exchange(handle, Traits::InvalidHandle());
    }

    Handle& operator =(HandleType source)
    {
        Reset(source);
        return *this;
    }

    constexpr explicit operator bool() const noexcept
    {
        return Traits::IsValid(handle);
    }

    constexpr HandleType Get() const noexcept { return handle; }

    constexpr operator HandleType() const noexcept { return handle; }

    HandleType* CloseAndGetAddressOf() noexcept
    {
        Close();
        handle = Traits::InvalidHandle();
        return &handle;
    }

private:
    HandleType handle;
};

/// Prevent accidentally calling CloseHandle(HANDLE) with a scoped handle.
/// Scoped handles must be closed by calling Close().
using ::CloseHandle;

template<typename Traits>
void CloseHandle(Handle<Traits> const&) = delete;


template<typename T = HANDLE>
struct MinusOneIsInvalidHandleTraits
{
    using HandleType = T;
    constexpr static HandleType InvalidHandle() noexcept { return static_cast<HandleType>(-1); }
    constexpr static bool IsValid(HandleType h) noexcept { return h != InvalidHandle(); }
    static void Close(HandleType h) noexcept { ::CloseHandle(h); }
};

template<typename T = HANDLE>
struct NullIsInvalidHandleTraits
{
    using HandleType = T;
    constexpr static HandleType InvalidHandle() noexcept { return nullptr; }
    constexpr static bool IsValid(HandleType h) noexcept { return h != InvalidHandle(); }
    static void Close(HandleType h) noexcept { ::CloseHandle(h); }
};


struct FileHandleTraits : MinusOneIsInvalidHandleTraits<> {};
using FileHandle = Handle<FileHandleTraits>;

struct ProcessHandleTraits : NullIsInvalidHandleTraits<> {};
using ProcessHandle = Handle<ProcessHandleTraits>;

struct ThreadHandleTraits : NullIsInvalidHandleTraits<> {};
using ThreadHandle = Handle<ThreadHandleTraits>;

struct TimerHandleTraits : NullIsInvalidHandleTraits<> {};
using TimerHandle = Handle<TimerHandleTraits>;

struct TokenHandleTraits : NullIsInvalidHandleTraits<> {};
using TokenHandle = Handle<TokenHandleTraits>;

} // namespace etk
