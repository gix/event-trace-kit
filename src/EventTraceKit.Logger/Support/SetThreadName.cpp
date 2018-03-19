#include "SetThreadName.h"
#include <windows.h>

namespace etk
{

namespace
{

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

} // namespace

void SetThreadName(unsigned long threadId, char const* threadName)
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

void SetCurrentThreadName(char const* threadName)
{
    SetThreadName(GetCurrentThreadId(), threadName);
}

} // namespace etk
