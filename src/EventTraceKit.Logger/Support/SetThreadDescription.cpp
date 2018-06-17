#include "SetThreadDescription.h"

#include "StringConversions.h"
#include <Windows.h>

namespace etk
{

namespace
{

template<typename Function>
Function* GetModuleProc(wchar_t const* moduleName, char const* procName)
{
    auto const module = GetModuleHandleW(moduleName);
    if (module)
        return reinterpret_cast<Function*>(GetProcAddress(module, procName));
    return nullptr;
}

// Special native exception code for setting thread names according to MSDN.
// <https://docs.microsoft.com/en-us/visualstudio/debugger/how-to-set-a-thread-name-in-native-code>.
DWORD const MS_VC_EXCEPTION = 0x406D1388;

#pragma pack(push, 8)
struct THREADNAME_INFO
{
    DWORD dwType;     // Must be 0x1000.
    LPCSTR szName;    // Pointer to name (in user addr space).
    DWORD dwThreadID; // Thread ID (-1=caller thread).
    DWORD dwFlags;    // Reserved for future use, must be zero.
};
#pragma pack(pop)

void SetThreadNameMSVC(unsigned long threadId, char const* threadName)
{
    THREADNAME_INFO info;
    info.dwType = 0x1000;
    info.szName = threadName;
    info.dwThreadID = threadId;
    info.dwFlags = 0;

    __try {
        RaiseException(MS_VC_EXCEPTION, 0, sizeof(info) / sizeof(ULONG_PTR),
                       reinterpret_cast<ULONG_PTR*>(&info));
    } __except (EXCEPTION_EXECUTE_HANDLER) {
    }
}

void SetThreadDescriptionShim(HANDLE thread, wchar_t const* threadName)
{
    auto const pSetThreadDescription = GetModuleProc<decltype(::SetThreadDescription)>(
        L"kernel32.dll", "SetThreadDescription");

    if (pSetThreadDescription) {
        (void)pSetThreadDescription(thread, threadName);
        return;
    }

    // The debugger needs to be attached to catch the name of the exception.
    // Otherwise we are just needlessly throwing an exception.
    if (IsDebuggerPresent())
        SetThreadNameMSVC(GetThreadId(thread), U16To8(threadName).c_str());
}

} // namespace

void SetThreadDescription(void* thread, wchar_t const* threadName)
{
    SetThreadDescriptionShim(thread, threadName);
}

void SetCurrentThreadDescription(wchar_t const* threadName)
{
    SetThreadDescriptionShim(GetCurrentThread(), threadName);
}

} // namespace etk
