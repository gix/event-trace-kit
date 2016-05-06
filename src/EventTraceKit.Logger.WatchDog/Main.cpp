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
    if (argc != 4) {
        fwprintf(stderr, L"Wrong number of arguments.");
        return -1;
    }

    try {
        DWORD processId = boost::lexical_cast<DWORD>(argv[1]);
        std::wstring loggerName(argv[2]);
        std::wstring exitEventName(argv[3]);

        ProcessHandle process(OpenProcess(SYNCHRONIZE, FALSE, processId));
        if (!process) {
            fwprintf(stderr, L"Failed to open process %lu to watch.", processId);
            return -1;
        }

        WaitEvent exitEventHandle = WaitEvent::Open(exitEventName, SYNCHRONIZE);

        if (WaitForAny(process, exitEventHandle) == 0)
            CloseTraceSession(loggerName);
    } catch (std::exception const& ex) {
        fwprintf(stderr, L"Caught exception: %s\n", ex.what());
        return -1;
    } catch (...) {
        fwprintf(stderr, L"Caught unknown exception.");
        return -1;
    }

    return 0;
}
