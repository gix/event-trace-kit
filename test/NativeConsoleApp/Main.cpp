#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <cstdio>

#include <TraceLoggingProvider.h>
#include <evntprov.h>
#include <evntrace.h>

static GUID const ProviderId = {
    0x5AB0948E, 0xC045, 0x411A, {0xAC, 0x12, 0xAC, 0x45, 0x5A, 0xFA, 0x8D, 0xF2}};

struct CustomFilter
{
    int value1;
    int value2;
};

static void NTAPI EnableCallback(_In_ LPCGUID SourceId, _In_ ULONG IsEnabled,
                                 _In_ UCHAR Level, _In_ ULONGLONG MatchAnyKeyword,
                                 _In_ ULONGLONG MatchAllKeyword,
                                 _In_opt_ PEVENT_FILTER_DESCRIPTOR FilterData,
                                 _Inout_opt_ PVOID CallbackContext)
{
    if (FilterData && FilterData->Size >= sizeof(CustomFilter)) {
        auto const& filter = *reinterpret_cast<CustomFilter const*>(FilterData->Ptr);
        int value = filter.value1;
    }
}

TRACELOGGING_DECLARE_PROVIDER(g_etkTlogSampleProvider);

// {363F6428-4C0A-4315-898A-A1F0434835E5}
TRACELOGGING_DEFINE_PROVIDER(g_etkTlogSampleProvider, "EtkTlogSampleProvider",
                             (0x363F6428, 0x4C0A, 0x4315, 0x89, 0x8A, 0xA1, 0xF0, 0x43,
                              0x48, 0x35, 0xE5),
                             // {C7B62508-0E4B-498F-8118-FD9641E1CFA5}
                             TraceLoggingOptionGroup(0xC7B62508, 0xE4B, 0x498F, 0x81,
                                                     0x18, 0xFD, 0x96, 0x41, 0xE1, 0xCF,
                                                     0xA5));

int main(int argc, char** argv)
{
    REGHANDLE traceHandle;
    EventRegister(&ProviderId, EnableCallback, nullptr, &traceHandle);
    TraceLoggingRegister(g_etkTlogSampleProvider);

    for (int i = 0; i < 5; ++i) {
        EVENT_DESCRIPTOR eventDesc = {};
        eventDesc.Id = i;
        EventWrite(traceHandle, &eventDesc, 0, nullptr);

        TraceLoggingWrite(g_etkTlogSampleProvider, "Tick",
                          TraceLoggingLevel(TRACE_LEVEL_VERBOSE), TraceLoggingInt32(i));

        _TlgWrite_imp(_TlgWrite, g_etkTlogSampleProvider, "Tick", (NULL, NULL),
                      TraceLoggingLevel(TRACE_LEVEL_VERBOSE), TraceLoggingInt32(i));

        TraceLoggingWrite(
            g_etkTlogSampleProvider, "Tick2", TraceLoggingLevel(TRACE_LEVEL_INFORMATION),
            TraceLoggingInt32(i, "iteration"), TraceLoggingHResult(E_INVALIDARG));

        printf("NativeConsoleApp %d\n", i);
        Sleep(1000);
    }

    TraceLoggingUnregister(g_etkTlogSampleProvider);
    EventUnregister(traceHandle);
    return 0;
}
