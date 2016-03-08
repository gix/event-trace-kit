#pragma once
#include "Support/CompilerSupport.h"
#include "Windows.h"
#include <comdef.h>

namespace etk
{

ETK_ALWAYS_INLINE
  HRESULT HResultFromWin32(unsigned long ec)
{
    return static_cast<HRESULT>(ec) <= 0
        ? static_cast<HRESULT>(ec)
        : static_cast<HRESULT>((ec & 0x0000FFFF) | (FACILITY_WIN32 << 16) | 0x80000000);
}

/// <summary>Converts an <see cref="LSTATUS"/> code to an HRESULT.</summary>
ETK_ALWAYS_INLINE
  HRESULT HResultFromLSTATUS(LSTATUS st)
{
    return HResultFromWin32(static_cast<unsigned long>(st));
}

ETK_ALWAYS_INLINE
  HRESULT GetLastErrorAsHResult()
{
    return HResultFromWin32(GetLastError());
}

#define ETK_MakeHResult(severity, facility, code, customer) \
    ((((severity) ? 1 : 0) << 31) | \
     (((customer) ? 1 : 0) << 29) | \
     (((facility) & 0x7FF) << 16) | \
     (int)(((code) & 0xFFFF)))

// FIXME: The MSVC static analyzer does not work using these.
_Success_(return)
ETK_ALWAYS_INLINE bool Succeeded(HRESULT hr) { return hr >= 0; }

// FIXME: The MSVC static analyzer does not work using these.
_Success_(return)
ETK_ALWAYS_INLINE bool Failed(HRESULT hr) { return hr < 0; }

ETK_ALWAYS_INLINE void ThrowOnFail(HRESULT hr)
{
    if (FAILED(hr)) {
        //(void)ETK_TRACE_HR(hr);
        throw _com_error(hr);
    }
}

#define ETK_IF_FAILED_GOTO(hr, label) \
    ETK_MULTILINE_MACRO_BEGIN         \
        HRESULT hr_ = (hr);            \
        if (FAILED(hr_)) {             \
            ETK_TRACE_HR(hr_);        \
            goto label;                \
        }                              \
    ETK_MULTILINE_MACRO_END

#define ETK_IF_FAILED_RETURN(hr)      \
    ETK_MULTILINE_MACRO_BEGIN         \
        HRESULT hr_ = (hr);            \
        if (FAILED(hr_)) {             \
            ETK_TRACE_HR(hr_);        \
            return hr_;                \
        }                              \
    ETK_MULTILINE_MACRO_END

#define ETK_IF_FAILED_RETURN_(hr)     \
    ETK_MULTILINE_MACRO_BEGIN         \
        HRESULT hr_ = (hr);            \
        if (FAILED(hr_)) {             \
            ETK_TRACE_HR(hr_);        \
            return;                    \
        }                              \
    ETK_MULTILINE_MACRO_END

/// <summary>Checks the HRESULT and returns it if failed.</summary>
#define ENSURE_HR(hr) ETK_IF_FAILED_RETURN(hr)

/// <summary>Checks the HRESULT and returns (without value) if failed.</summary>
#define ENSURE_HR_(hr) ETK_IF_FAILED_RETURN_(hr)

/// <summary>Checks the HRESULT and jumps to the "done:" label via goto.</summary>
#define CHECK_HR(hr) ETK_IF_FAILED_GOTO(hr, done)

/// <summary>Checks the HRESULT and throws an exception if failed.</summary>
#define THROW_HR(hr) ::etk::ThrowOnFail(hr)

/// <summary>Checks the system error code and throws an exception if failed.</summary>
#define THROW_EC(ec) ::etk::ThrowOnFail(::etk::HResultFromWin32(ec))

} // namespace etk
