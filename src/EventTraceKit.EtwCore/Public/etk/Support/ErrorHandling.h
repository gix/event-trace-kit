#pragma once
#include "etk/Support/CompilerSupport.h"
#include <Windows.h>
#include <comdef.h>

namespace etk
{

ETK_ALWAYS_INLINE
constexpr HRESULT HResultFromWin32(unsigned long ec)
{
    return static_cast<HRESULT>(ec) <= 0
               ? static_cast<HRESULT>(ec)
               : static_cast<HRESULT>((ec & 0x0000FFFF) | (FACILITY_WIN32 << 16) |
                                      0x80000000);
}

/// <summary>Converts an <see cref="LSTATUS"/> code to an HRESULT.</summary>
ETK_ALWAYS_INLINE
constexpr HRESULT HResultFromLSTATUS(LSTATUS st)
{
    return HResultFromWin32(static_cast<unsigned long>(st));
}

ETK_ALWAYS_INLINE
HRESULT GetLastErrorAsHResult()
{
    return HResultFromWin32(GetLastError());
}

inline HRESULT MakeHResult(bool severity, unsigned short facility, unsigned short code,
                           bool customer)
{
    return ((severity ? 1 : 0) << 31) | ((customer ? 1 : 0) << 29) |
           ((facility & 0x7FF) << 16) | (code & 0xFFFF);
}

HRESULT TraceHResult(HRESULT result, char const* file, int lineNumber,
                     char const* function);

#ifdef _DEBUG
#define ETK_TRACE_HR(hr) ::etk::TraceHResult((hr), __FILE__, __LINE__, __FUNCTION__)
#else
#define ETK_TRACE_HR(hr) (hr)
#endif

ETK_ALWAYS_INLINE void ThrowOnFail(HRESULT hr)
{
    if (FAILED(hr)) {
        (void)ETK_TRACE_HR(hr);
        throw _com_error(hr);
    }
}

#define ETK_IF_FAILED_RETURN(hr, ret)                                                    \
    ETK_MULTILINE_MACRO_BEGIN                                                            \
    HRESULT hr_ = (hr);                                                                  \
    if (FAILED(hr_)) {                                                                   \
        (void)ETK_TRACE_HR(hr_);                                                         \
        return ret;                                                                      \
    }                                                                                    \
    ETK_MULTILINE_MACRO_END

#define ETK_IF_FAILED_RETURN_(expr, hr)                                                  \
    ETK_MULTILINE_MACRO_BEGIN                                                            \
    HRESULT hr_ = (expr);                                                                \
    if (FAILED(hr_)) {                                                                   \
        (void)ETK_TRACE_HR(hr);                                                          \
        return hr_;                                                                      \
    }                                                                                    \
    ETK_MULTILINE_MACRO_END

#define ETK_IF_FAILED_RETURN_EXCEPT(hrExcept, expr)                                      \
    ETK_MULTILINE_MACRO_BEGIN                                                            \
    HRESULT hr_ = (expr);                                                                \
    if (FAILED(hr_) && hr_ != hrExcept) {                                                \
        (void)ETK_TRACE_HR(hr_);                                                         \
        return hr_;                                                                      \
    }                                                                                    \
    ETK_MULTILINE_MACRO_END

#define ETK_IF_FAILED_RETURN_VOID(expr)                                                  \
    ETK_MULTILINE_MACRO_BEGIN                                                            \
    HRESULT hr_ = (expr);                                                                \
    if (FAILED(hr_)) {                                                                   \
        (void)ETK_TRACE_HR(hr_);                                                         \
        return;                                                                          \
    }                                                                                    \
    ETK_MULTILINE_MACRO_END

#define ETK_IF_FAILED_GOTO(expr, label)                                                  \
    ETK_MULTILINE_MACRO_BEGIN                                                            \
    HRESULT hr_ = (expr);                                                                \
    if (FAILED(hr_)) {                                                                   \
        (void)ETK_TRACE_HR(hr_);                                                         \
        goto label;                                                                      \
    }                                                                                    \
    ETK_MULTILINE_MACRO_END

#define ETK_IF_FAILED_GOTO_(expr, label, hr)                                             \
    ETK_MULTILINE_MACRO_BEGIN                                                            \
    HRESULT hr_ = (expr);                                                                \
    if (FAILED(hr_)) {                                                                   \
        (void)ETK_TRACE_HR(hr);                                                          \
        goto label;                                                                      \
    }                                                                                    \
    ETK_MULTILINE_MACRO_END

/// <summary>Checks the HRESULT-expression and returns it if failed.</summary>
#define HR(expr) ETK_IF_FAILED_RETURN(expr, hr_)

/// <summary>Checks the HRESULT-expression and returns an HRESULT if failed.</summary>
#define HR_(expr, hr) ETK_IF_FAILED_RETURN_(expr, hr)

/// <summary>Checks the HRESULT-expression and returns it if failed (ignores values of
/// hrExcept).</summary>
#define HRE(hrExcept, expr) ETK_IF_FAILED_RETURN_EXCEPT(hrExcept, expr)

/// <summary>Checks the HRESULT-expression and returns void if failed.</summary>
#define HRV(expr) ETK_IF_FAILED_RETURN_VOID(expr)

/// <summary>Checks the HRESULT-expression and returns 0 if failed.</summary>
#define HRZ(expr) ETK_IF_FAILED_RETURN(expr, 0)

/// <summary>Checks the HRESULT-expression and returns nullptr if failed.</summary>
#define HRN(expr) ETK_IF_FAILED_RETURN(expr, nullptr)

/// <summary>Checks the HRESULT-expression and returns a custom value if failed.</summary>
#define HRX(ret, expr) ETK_IF_FAILED_RETURN(expr, ret)

/// <summary>Checks the HRESULT and jumps to the "done:" label via goto.</summary>
#define HRD(expr) ETK_IF_FAILED_GOTO(expr, done)
#define HRD_(expr, hr) ETK_IF_FAILED_GOTO_(expr, done, hr)

/// <summary>Checks the HRESULT and throws an exception if failed.</summary>
#define HRT(hr) ::etk::ThrowOnFail(hr)

} // namespace etk
