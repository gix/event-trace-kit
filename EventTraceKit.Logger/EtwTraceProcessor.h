#pragma once
#include "Support/CompilerSupport.h"

#include <windows.h>
#include <thread>
#include <evntcons.h>

namespace etk
{

class EtwTraceProcessor
{
public:
    EtwTraceProcessor(std::wstring loggerName);
    ~EtwTraceProcessor();

    void StartProcessing();
    bool IsEndOfTracing();

private:
    static DWORD WINAPI ProcessTraceProc(_In_ LPVOID lpParameter);
    static VOID WINAPI EventRecordCallback(_In_ PEVENT_RECORD EventRecord);

    void OnProcessTrace();
    void OnEvent(EVENT_RECORD* eventRecord);
    void InitSym(DWORD processId);
    void CloseSym();

    std::wstring loggerName;
    std::thread procThread;
    TRACEHANDLE traceHandle;
    EVENT_TRACE_LOGFILEW logFile;
    HANDLE symProcess;
};

} // namespace etk
