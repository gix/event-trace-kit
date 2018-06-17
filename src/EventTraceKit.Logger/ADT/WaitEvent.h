#pragma once
#include "ADT/Handle.h"
#include "ADT/Span.h"
#include <windows.h>

#include <algorithm>
#include <chrono>
#include <iterator>
#include <string>

namespace etk
{

using WaitHandle = Handle<NullIsInvalidHandleTraits<>>;

using WaitTimeoutDuration = std::chrono::duration<unsigned, std::milli>;
WaitTimeoutDuration const InfiniteWaitTimeout{ INFINITE };

class WaitEvent
{
public:
    WaitEvent() = default;

    WaitEvent(bool initialState, bool manualReset);
    WaitEvent(bool initialState, bool manualReset, std::wstring const& name);
    WaitEvent(HRESULT& hr, bool initialState, bool manualReset);
    WaitEvent(HRESULT& hr, bool initialState, bool manualReset, std::wstring const& name);

    static WaitEvent Open(std::wstring const& name);
    static WaitEvent Open(std::wstring const& name, unsigned desiredAccess);
    static WaitEvent OpenOrCreate(
        bool initialState, bool manualReset, std::wstring const& name,
        bool* created = nullptr);

    void Close()
    {
        handle.Close();
    }

    HRESULT Reset();
    HRESULT Set();
    HRESULT Wait(WaitTimeoutDuration timeout = InfiniteWaitTimeout) const;

    HANDLE Handle() const { return handle.Get(); }

    template<typename Rep, typename Period>
    bool Wait(std::chrono::duration<Rep, Period> const& timeout)
    {
        return Wait(GetTimeout(timeout).count()) == S_OK;
    }

protected:
    static std::wstring const Empty;

private:
    WaitEvent(WaitHandle handle)
        : handle(std::move(handle))
    {
    }

    WaitHandle handle;
};

namespace details
{

unsigned WaitFor(WaitTimeoutDuration const& timeout, cspan<HANDLE> handles, bool waitAll);

template<typename Rep, typename Period>
WaitTimeoutDuration GetWaitTimeout(std::chrono::duration<Rep, Period> const& timeout)
{
    WaitTimeoutDuration t = std::chrono::duration_cast<WaitTimeoutDuration>(timeout);
    // When converting from a smaller to a larger unit (e.g. 100 micros
    // to millis), precision decreases. We add one to ensure a timeout
    // that is equal or larger than the requested one.
    if (t < timeout)
        ++t;
    return t;
}

inline HANDLE GetWaitHandle(WaitEvent const& event)
{
    return event.Handle();
}

inline HANDLE GetWaitHandle(ProcessHandle const& process)
{
    return process.Get();
}

} // namespace details

enum WaitResult : unsigned
{
    Timeout = WAIT_TIMEOUT
};

template<typename Rep, typename Period, typename... Ts>
bool WaitForAll(std::chrono::duration<Rep, Period> const& timeout,
                Ts const&... events)
{
    HANDLE handles[] = { details::GetWaitHandle(events)... };
    return details::WaitFor(handles, GetWaitTimeout(timeout), true) != WAIT_TIMEOUT;
}

template<typename... Ts>
bool WaitForAll(Ts const&... events)
{
    HANDLE handles[] = { details::GetWaitHandle(events)... };
    return details::WaitFor(InfiniteWaitTimeout, handles, true) != WAIT_TIMEOUT;
}

template<typename Rep, typename Period, typename... Ts>
unsigned WaitForAny(std::chrono::duration<Rep, Period> const& timeout,
                    Ts const&... events)
{
    HANDLE handles[] = { details::GetWaitHandle(events)... };
    return details::WaitFor(GetWaitTimeout(timeout), handles, false);
}

template<typename... Ts>
unsigned WaitForAny(Ts const&... events)
{
    HANDLE handles[] = { details::GetWaitHandle(events)... };
    return details::WaitFor(InfiniteWaitTimeout, handles, false);
}


class AutoResetEvent
    : public WaitEvent
{
public:
    AutoResetEvent(bool initialState = false, std::wstring const& name = Empty)
        : WaitEvent(initialState, false, name) { }

    AutoResetEvent(AutoResetEvent&& source) noexcept
        : WaitEvent(std::move(source))
    {
    }

    AutoResetEvent& operator =(AutoResetEvent&& source) noexcept
    {
        WaitEvent::operator =(std::move(source));
        return *this;
    }

    AutoResetEvent(AutoResetEvent const&) = delete;
    AutoResetEvent& operator =(AutoResetEvent const&) = delete;
};

class ManualResetEvent
    : public WaitEvent
{
public:
    ManualResetEvent(bool initialState = false, std::wstring const& name = Empty)
        : WaitEvent(initialState, true, name) { }

    ManualResetEvent(ManualResetEvent&& source) noexcept
        : WaitEvent(std::move(source))
    {
    }

    ManualResetEvent& operator =(ManualResetEvent&& source) noexcept
    {
        WaitEvent::operator =(std::move(source));
        return *this;
    }

    ManualResetEvent(ManualResetEvent const&) = delete;
    ManualResetEvent& operator =(ManualResetEvent const&) = delete;
};

} // namespace etk
