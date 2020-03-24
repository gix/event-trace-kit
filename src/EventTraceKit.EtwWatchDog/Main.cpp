#include "WaitEvent.h"

#include "etk/ADT/Handle.h"
#include "etk/Support/ByteCount.h"
#include "etk/Support/ErrorHandling.h"

#include <evntrace.h>
#include <string>
#include <vector>
#include <windows.h>

using namespace etk;

static void CloseTraceSession(std::wstring const& loggerName)
{
    size_t const bufferSize =
        sizeof(EVENT_TRACE_PROPERTIES) + ZStringByteCount(loggerName);
    std::vector<std::byte> buffer(bufferSize);

    auto const traceProperties = new (buffer.data()) EVENT_TRACE_PROPERTIES();
    traceProperties->Wnode.BufferSize = static_cast<ULONG>(bufferSize);
    traceProperties->LoggerNameOffset = sizeof(EVENT_TRACE_PROPERTIES);
    std::memcpy(buffer.data() + traceProperties->LoggerNameOffset, loggerName.data(),
                ZStringByteCount(loggerName));

    ULONG const ec =
        ControlTraceW(0, loggerName.c_str(), traceProperties, EVENT_TRACE_CONTROL_STOP);
    if (ec != ERROR_SUCCESS && ec != ERROR_WMI_INSTANCE_NOT_FOUND)
        fwprintf(stderr, L"Failed to close trace session (status=%X)\n", ec);
}

int wmain(int argc, wchar_t** argv)
{
    if (argc != 5) {
        fwprintf(stderr, L"Wrong number of arguments.\n");
        return -1;
    }

    try {
        DWORD const processId = std::stoul(argv[1]);
        std::wstring const loggerName(argv[2]);
        std::wstring const readyEventName(argv[3]);
        std::wstring const exitEventName(argv[4]);

        ProcessHandle process(OpenProcess(SYNCHRONIZE, FALSE, processId));
        if (!process) {
            HRESULT hr = GetLastErrorAsHResult();
            fwprintf(stderr, L"Failed to open process %lu to watch. (hr=0x%08X)\n",
                     processId, hr);
            return -1;
        }

        WaitEvent readyEvent =
            WaitEvent::Open(readyEventName, SYNCHRONIZE | EVENT_MODIFY_STATE);
        WaitEvent exitEvent = WaitEvent::Open(exitEventName, SYNCHRONIZE);

        HRESULT hr = readyEvent.Set();
        if (FAILED(hr))
            fwprintf(stderr, L"Failed to set ready event. (hr=0x%08X)\n", hr);

        if (WaitForAny(process, exitEvent) == 0)
            CloseTraceSession(loggerName);
    } catch (std::exception const& ex) {
        fwprintf(stderr, L"Caught exception: %hs\n", ex.what());
        return -1;
    } catch (...) {
        fwprintf(stderr, L"Caught unknown exception.\n");
        return -1;
    }

    return 0;
}
