#define WIN32_LEAN_AND_MEAN
#include <cstdio>
#include <Windows.h>
#include <evntprov.h>

static GUID const ProviderId = {0x5AB0948E, 0xC045, 0x411A, {0xAC, 0x12, 0xAC, 0x45, 0x5A, 0xFA, 0x8D, 0xF2}};

int main(int argc, char** argv)
{
    REGHANDLE traceHandle;
    EventRegister(&ProviderId, nullptr, nullptr, &traceHandle);

    for (int i = 0; i < 5; ++i) {
        EVENT_DESCRIPTOR eventDesc = {};
        eventDesc.Id = i;
        EventWrite(traceHandle, &eventDesc, 0, nullptr);
        printf("NativeConsoleApp %d\n", i);
        Sleep(1000);
    }

    EventUnregister(traceHandle);
    return 0;
}
