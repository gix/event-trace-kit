#include "Win32Exception.h"

#include "etk/Support/ErrorHandling.h"

#include <cstdint>
#include <exception>
#include <memory>
#include <string>

#include <strsafe.h>
#include <windows.h>


namespace etk
{

Win32Exception::Win32Exception()
    : error(GetLastError())
{
    Format();
}

Win32Exception::Win32Exception(long error)
    : error(static_cast<unsigned long>(error))
{
    Format();
}

Win32Exception::Win32Exception(unsigned long error)
    : error(error)
{
    Format();
}

Win32Exception::Win32Exception(std::wstring const& message)
    : error(GetLastError())
    , message(message)
{
    Format();
}

Win32Exception::Win32Exception(long error, std::wstring const& message)
    : error(static_cast<unsigned long>(error))
    , message(message)
{
    Format();
}

Win32Exception::Win32Exception(unsigned long error, std::wstring const& message)
    : error(error)
    , message(message)
{
    Format();
}

char const* Win32Exception::what() const noexcept
{
    return ansiMessage.c_str();
}

void Win32Exception::Format()
{
    wchar_t* errMessageBuffer;
    DWORD result = FormatMessageW(
        FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        nullptr,
        error,
        MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        reinterpret_cast<LPWSTR>(&errMessageBuffer),
        0,
        nullptr);

    if (result == 0) {
        DWORD err = GetLastError();
        result = FormatMessageW(
            FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
            nullptr,
            err,
            MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
            reinterpret_cast<LPWSTR>(&errMessageBuffer),
            0,
            nullptr);

        if (result == 0) {
            message = L"Unknown error occurred. Additionally, an error occurred formatting the error code.";
        }
    }

    if (message.size() > 0)
        message.append(L"\n[%d] %ls");
    else
        message.append(L"[%d] %ls");

    std::wstring messageBuffer(wcslen(errMessageBuffer) + 40, 0);
    HRESULT hr = StringCchPrintfW(
        &messageBuffer[0],
        messageBuffer.length(),
        message.c_str(),
        error,
        errMessageBuffer);
    LocalFree(errMessageBuffer);

    if (FAILED(hr)) {
        message = L"Unknown error occurred. Additionally, an error occurred formatting the error code.";
        ansiMessage = "Unknown error occurred. Additionally, an error occurred formatting the error code.";
        return;
    }

    int cch = WideCharToMultiByte(CP_ACP, 0, messageBuffer.c_str(), -1, nullptr, 0, "?", nullptr);
    if (cch == 0) {
        message = L"Unknown error occurred. Additionally, an error occurred formatting the error code.";
        ansiMessage = "Unknown error occurred. Additionally, an error occurred formatting the error code.";
        return;
    }

    std::string ansiBuffer(static_cast<size_t>(cch), 0);
    WideCharToMultiByte(CP_ACP, 0, messageBuffer.c_str(), -1, &ansiBuffer[0],
                        static_cast<int>(ansiBuffer.length()), "?", nullptr);

    message = std::move(messageBuffer);
    ansiMessage = std::move(ansiBuffer);
}

} // namespace ffmf
