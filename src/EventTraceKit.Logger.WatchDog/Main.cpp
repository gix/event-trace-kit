#include "ADT/Handle.h"
#include "ADT/WaitEvent.h"
#include "ADT/WaitEvent.cpp"
#include "Support/ByteCount.h"
#include "Win32Exception.cpp"
#include <boost/lexical_cast.hpp>
#include <windows.h>
#include <evntrace.h>

using namespace etk;

static void CloseTraceSession(std::wstring const& loggerName)
{
    size_t const bufferSize = sizeof(EVENT_TRACE_PROPERTIES) + ByteCount(loggerName);
    std::vector<char> buffer(bufferSize);

    auto traceProperties = new(buffer.data()) EVENT_TRACE_PROPERTIES();
    //traceProperties->Wnode.Guid = properties.Id;
    traceProperties->Wnode.BufferSize = static_cast<ULONG>(bufferSize);
    traceProperties->LoggerNameOffset = sizeof(EVENT_TRACE_PROPERTIES);
    std::memcpy(buffer.data() + traceProperties->LoggerNameOffset,
                loggerName.data(), ByteCount(loggerName));

    ULONG ec = ControlTraceW(0, loggerName.c_str(), traceProperties,
                             EVENT_TRACE_CONTROL_STOP);
    if (ec != ERROR_SUCCESS)
        fwprintf(stderr, L"Failed to close trace session (status=%X)\n", ec);
}

int wmain(int argc, wchar_t** argv)
{
    if (argc != 5) {
        fwprintf(stderr, L"Wrong number of arguments.\n");
        return -1;
    }

    try {
        DWORD processId = boost::lexical_cast<DWORD>(argv[1]);
        std::wstring loggerName(argv[2]);
        std::wstring readyEventName(argv[3]);
        std::wstring exitEventName(argv[4]);

        ProcessHandle process(OpenProcess(SYNCHRONIZE, FALSE, processId));
        if (!process) {
            HRESULT hr = GetLastErrorAsHResult();
            fwprintf(stderr, L"Failed to open process %lu to watch. (hr=0x%08X)\n", processId, hr);
            return -1;
        }

        WaitEvent readyEvent = WaitEvent::Open(readyEventName, SYNCHRONIZE | EVENT_MODIFY_STATE);
        WaitEvent exitEvent = WaitEvent::Open(exitEventName, SYNCHRONIZE);

        HRESULT hr = readyEvent.Set();
        if (FAILED(hr))
            fwprintf(stderr, L"Failed to set ready event. (hr=0x%08X)\n", hr);

        if (WaitForAny(process, exitEvent) == 0)
            CloseTraceSession(loggerName);
    } catch (std::exception const& ex) {
        fwprintf(stderr, L"Caught exception: %s\n", ex.what());
        return -1;
    } catch (...) {
        fwprintf(stderr, L"Caught unknown exception.\n");
        return -1;
    }

    return 0;
}
