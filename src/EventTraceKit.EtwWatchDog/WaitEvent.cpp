#include "WaitEvent.h"

#include "Win32Exception.h"
#include "etk/Support/ErrorHandling.h"

#include <exception>
#include <string_view>

namespace etk
{
std::wstring const WaitEvent::Empty;

class Exception : public std::exception
{
public:
    Exception(char const* msg)
        : msg(msg)
    {
    }

    virtual char const* what() const noexcept override { return msg; }

private:
    char const* msg;
};

class ArgumentException : public Exception
{
public:
    ArgumentException(char const* msg)
        : Exception(msg)
    {
    }
};

class ArgumentNullException : public ArgumentException
{
public:
    ArgumentNullException(char const* msg)
        : ArgumentException(msg)
    {
    }
};

class NotSupportedException : public Exception
{
public:
    NotSupportedException(char const* msg)
        : Exception(msg)
    {
    }
};

class AbandonedMutexException : public Exception
{
public:
    AbandonedMutexException()
        : Exception("Abandoned mutex.")
    {
    }
};

WaitEvent::WaitEvent(bool initialState, bool manualReset)
{
    WaitHandle handle(::CreateEventW(nullptr, manualReset ? TRUE : FALSE,
                                     initialState ? TRUE : FALSE, nullptr));

    if (!handle.IsValid())
        throw Win32Exception(::GetLastError());

    this->handle = std::move(handle);
}

WaitEvent::WaitEvent(bool initialState, bool manualReset, std::wstring const& name)
{
    if (name.size() > MAX_PATH)
        throw ArgumentException("WaitHandleName is too long.");

    WaitHandle handle(::CreateEventW(nullptr, manualReset ? TRUE : FALSE,
                                     initialState ? TRUE : FALSE, name.c_str()));

    if (!handle.IsValid()) {
        DWORD const ec = ::GetLastError();
        if (!name.empty() && ec == ERROR_INVALID_HANDLE)
            throw Exception("Wait-handle cannot be opened.");
        throw Win32Exception(ec);
    }

    this->handle = std::move(handle);
}

WaitEvent::WaitEvent(HRESULT& hr, bool initialState, bool manualReset)
{
    WaitHandle handle(::CreateEventW(nullptr, manualReset ? TRUE : FALSE,
                                     initialState ? TRUE : FALSE, nullptr));

    if (!handle.IsValid()) {
        hr = GetLastErrorAsHResult();
        return;
    }

    this->handle = std::move(handle);
    hr = S_OK;
}

WaitEvent::WaitEvent(HRESULT& hr, bool initialState, bool manualReset,
                     std::wstring const& name)
{
    WaitHandle handle(::CreateEventW(nullptr, manualReset ? TRUE : FALSE,
                                     initialState ? TRUE : FALSE, name.c_str()));

    if (!handle.IsValid()) {
        hr = GetLastErrorAsHResult();
        return;
    }

    this->handle = std::move(handle);
    hr = S_OK;
}

WaitEvent WaitEvent::Open(std::wstring const& name)
{
    return Open(name, EVENT_MODIFY_STATE | SYNCHRONIZE);
}

WaitEvent WaitEvent::Open(std::wstring const& name, unsigned desiredAccess)
{
    if (name.length() == 0)
        throw ArgumentNullException("name");
    if (name.length() > MAX_PATH)
        throw ArgumentException("Wait-handle name too long.");

    WaitHandle handle(::OpenEventW(desiredAccess, false, name.c_str()));
    if (!handle.IsValid()) {
        DWORD const ec = ::GetLastError();
        if (ec == ERROR_FILE_NOT_FOUND || ec == ERROR_INVALID_NAME)
            throw Exception("Invalid wait-handle name.");
        if (name.length() > 0 && ec == ERROR_INVALID_HANDLE)
            throw Exception("Wait-handle cannot be opened.");
        throw Win32Exception(ec);
    }

    return WaitEvent(std::move(handle));
}

WaitEvent WaitEvent::OpenOrCreate(bool initialState, bool manualReset,
                                  std::wstring const& name, bool* created /*= nullptr*/)
{
    if (name.length() == 0)
        throw ArgumentNullException("name");
    if (name.length() > MAX_PATH)
        throw ArgumentException("Wait-handle name too long.");

    if (created != nullptr)
        *created = false;

    WaitHandle handle(
        ::OpenEventW(EVENT_MODIFY_STATE | SYNCHRONIZE, false, name.c_str()));
    if (handle.IsValid())
        return WaitEvent(std::move(handle));

    DWORD ec = ::GetLastError();
    if (ec == ERROR_INVALID_NAME)
        throw Exception("Invalid wait-handle name.");
    if (name.length() > 0 && ec == ERROR_INVALID_HANDLE)
        throw Exception("Wait-handle cannot be opened.");
    if (ec != ERROR_FILE_NOT_FOUND)
        throw Win32Exception(ec);

    handle = WaitHandle(::CreateEventW(nullptr, manualReset ? TRUE : FALSE,
                                       initialState ? TRUE : FALSE, name.c_str()));
    if (!handle.IsValid()) {
        ec = ::GetLastError();
        if (!name.empty() && ec == ERROR_INVALID_HANDLE)
            throw Exception("Wait-handle cannot be opened.");
        throw Win32Exception(ec);
    }

    if (created != nullptr)
        *created = true;

    return WaitEvent(std::move(handle));
}

HRESULT WaitEvent::Set()
{
    if (!::SetEvent(handle))
        return GetLastErrorAsHResult();
    return S_OK;
}

HRESULT WaitEvent::Reset()
{
    if (!::ResetEvent(handle))
        return GetLastErrorAsHResult();
    return S_OK;
}

HRESULT WaitEvent::Wait(WaitTimeoutDuration timeout /*= InfiniteWaitTimeout*/) const
{
    DWORD const result = ::WaitForSingleObject(handle, timeout.count());
    switch (result) {
    case WAIT_OBJECT_0:
        return S_OK;
    case WAIT_ABANDONED:
        return HResultFromWin32(WAIT_ABANDONED);
    case WAIT_TIMEOUT:
        return E_PENDING;
    default:
        return GetLastErrorAsHResult();
    }
}

unsigned details::WaitFor(WaitTimeoutDuration const& timeout, cspan<HANDLE> handles,
                          bool waitAll)
{
    if (handles.size() > MAXIMUM_WAIT_OBJECTS)
        throw NotSupportedException(
            "The number of WaitEvent handles must be less than or equal to 64.");

    DWORD const result =
        ::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(),
                                 waitAll ? TRUE : FALSE, timeout.count());
    if (result >= WAIT_ABANDONED && (result - WAIT_ABANDONED) < handles.size())
        throw AbandonedMutexException();

    static_assert(sizeof(DWORD) == sizeof(unsigned), "Invariant violated.");
    return static_cast<unsigned>(result);
}

} // namespace etk
