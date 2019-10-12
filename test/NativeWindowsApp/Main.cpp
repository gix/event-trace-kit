#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <cstdio>
#include <evntprov.h>

static GUID const ProviderId = {
    0x5AB0948E, 0xC045, 0x411A, {0xAC, 0x12, 0xAC, 0x45, 0x5A, 0xFA, 0x8D, 0xF2}};

static void NTAPI EnableCallback(_In_ LPCGUID SourceId, _In_ ULONG IsEnabled,
    _In_ UCHAR Level, _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ PEVENT_FILTER_DESCRIPTOR FilterData,
    _Inout_opt_ PVOID CallbackContext)
{
}

int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, PWSTR pCmdLine,
    int nCmdShow)
{
    REGHANDLE traceHandle;
    EventRegister(&ProviderId, EnableCallback, nullptr, &traceHandle);

    for (int i = 0; i < 5; ++i) {
        EVENT_DESCRIPTOR eventDesc = {};
        eventDesc.Id = i;
        EventWrite(traceHandle, &eventDesc, 0, nullptr);
        printf("NativeWindowsApp %d\n", i);
        Sleep(1000);
    }

    EventUnregister(traceHandle);
    return 0;
}
