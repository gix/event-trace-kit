#include "ErrorHandling.h"

#include "Support/CountOf.h"

#include <cstdio>
#include <strsafe.h>

namespace etk
{

wchar_t const* StringFromError(wchar_t* buffer, size_t size, long ec)
{
    *buffer = 0;
    DWORD cb = FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM, nullptr, static_cast<DWORD>(ec),
                              0, buffer, static_cast<DWORD>(size), nullptr);
    wchar_t const unk[] = L"<unknown>";
    if (!cb && size > etk::lengthof(unk))
        (void)StringCchCopyW(buffer, size, unk);
    return buffer;
}

HRESULT TraceHResult(HRESULT result, char const* file, int lineNumber,
                     char const* function)
{
    if (FAILED(result)) {
        wchar_t errorMessage[128];
        StringFromError(errorMessage, etk::countof(errorMessage), result);
        fwprintf(stderr, L"%hs(%d): %hs: HResult=0x%x: %ls\n", file, lineNumber, function,
                 result, errorMessage);
    }

    return result;
}

} // namespace etk
