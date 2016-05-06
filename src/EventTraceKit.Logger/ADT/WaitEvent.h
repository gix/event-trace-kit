#pragma once
#include "ADT/ArrayRef.h"
#include "ADT/Handle.h"
#include <windows.h>

#include <algorithm>
#include <chrono>
#include <iterator>
#include <string>

namespace etk
{

using WaitHandle = Handle<NullIsInvalidHandleTraits>;

class WaitEvent
{
public:
    WaitEvent() {}

    WaitEvent(bool initialState, bool manualReset);
    WaitEvent(bool initialState, bool manualReset, std::wstring const& name);
    WaitEvent(HRESULT& hr, bool initialState, bool manualReset);
    WaitEvent(HRESULT& hr, bool initialState, bool manualReset, std::wstring const& name);

    WaitEvent(WaitEvent const&) = delete;
    WaitEvent& operator =(WaitEvent const&) = delete;

    static WaitEvent Open(std::wstring const& name);
    static WaitEvent Open(std::wstring const& name, unsigned desiredAccess);
    static WaitEvent OpenOrCreate(
        bool initialState, bool manualReset, std::wstring const& name,
        bool* created = nullptr);

    WaitEvent(WaitEvent&& source)
        : handle(std::move(source.handle))
    {
        source.handle = WaitHandle::InvalidHandle();
    }

    WaitEvent& operator =(WaitEvent&& source)
    {
        using std::swap;
        swap(handle, source.handle);
        return *this;
    }

    void Close()
    {
        handle.Close();
    }

    HRESULT Reset();
    HRESULT Set();
    HRESULT Wait(unsigned timeoutInMillis = INFINITE) const;

    HANDLE Handle() const { return handle.Get(); }

    template<typename Rep, typename Period>
    bool Wait(std::chrono::duration<Rep, Period> const& timeout)
    {
        return Wait(GetTimeout(timeout).count()) == S_OK;
    }

    static bool WaitAll(ArrayRef<WaitEvent> handles)
    {
        using namespace std::chrono;
        return DoWait(handles, duration<unsigned, std::milli>(INFINITE), true);
    }

    template<typename Rep, typename Period>
    static bool WaitAll(ArrayRef<WaitEvent> handles,
                        std::chrono::duration<Rep, Period> const& timeout)
    {
        return DoWait(handles, GetTimeout(timeout), true);
    }

    template<typename T>
    static bool WaitAny(WaitEvent const& e1, WaitEvent const& e2)
    {
        using namespace std::chrono;
        HANDLE handles[] = { e1.handle.Get(), e2.handle.Get() };
        return DoWait(handles, duration<unsigned, std::milli>(INFINITE), false);
    }

    static bool WaitAny(ArrayRef<WaitEvent> handles)
    {
        using namespace std::chrono;
        return DoWait(handles, duration<unsigned, std::milli>(INFINITE), false);
    }

    template<typename Rep, typename Period>
    static bool WaitAny(ArrayRef<WaitEvent> handles,
                        std::chrono::duration<Rep, Period> const& timeout)
    {
        return DoWait(handles, GetTimeout(timeout), false);
    }

protected:
    static std::wstring const Empty;

private:
    static bool DoWait(
        ArrayRef<WaitEvent> events, TimeoutDuration const& timeout, bool waitAll)
    {
        std::vector<HANDLE> handles;
        std::transform(events.begin(), events.end(), std::back_inserter(handles),
                       [](WaitEvent const& e) { return e.Handle(); });
        return DoWait(handles, GetTimeout(timeout), waitAll);
    }

    static bool DoWait(
        ArrayRef<HANDLE> handles, TimeoutDuration const& timeout, bool waitAll);

    WaitEvent(WaitHandle handle)
        : handle(std::move(handle))
    {
    }

    template<typename Rep, typename Period>
    static TimeoutDuration GetTimeout(
        std::chrono::duration<Rep, Period> const& timeout)
    {
        TimeoutDuration t = std::chrono::duration_cast<TimeoutDuration>(timeout);
        // When converting from a smaller to a larger unit (e.g. 100 micros
        // to millis), precision decreases. We add one to ensure a timeout
        // that is equal or larger than the requested one.
        if (t < timeout)
            ++t;
        return t;
    }

    WaitHandle handle;
};

class AutoResetEvent
    : public WaitEvent
{
public:
    AutoResetEvent(bool initialState = false, std::wstring const& name = Empty)
        : WaitEvent(initialState, false, name) { }

    AutoResetEvent(AutoResetEvent&& source)
        : WaitEvent(std::move(source))
    {
    }

    AutoResetEvent& operator =(AutoResetEvent&& source)
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

    ManualResetEvent(ManualResetEvent&& source)
        : WaitEvent(std::move(source))
    {
    }

    ManualResetEvent& operator =(ManualResetEvent&& source)
    {
        WaitEvent::operator =(std::move(source));
        return *this;
    }

    ManualResetEvent(ManualResetEvent const&) = delete;
    ManualResetEvent& operator =(ManualResetEvent const&) = delete;
};

} // namespace etk
