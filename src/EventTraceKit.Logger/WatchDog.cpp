#include "WatchDog.h"

using namespace System;
using namespace System::Diagnostics;
using namespace System::IO;
using namespace System::Reflection;
using namespace System::Text;
using namespace System::Threading;
using namespace Microsoft::Win32::SafeHandles;

namespace EventTraceKit
{

static String^ GetAssemblyFilePath(Assembly^ assembly)
{
    String^ path = (gcnew Uri(assembly->CodeBase))->AbsolutePath;
    path = Uri::UnescapeDataString(path);
    return Path::GetFullPath(path);
}

static String^ GetCurrentAssemblyDir()
{
    String^ filePath = GetAssemblyFilePath(Assembly::GetExecutingAssembly());
    return Path::GetDirectoryName(filePath);
}

WatchDog::WatchDog(String^ loggerName)
    : loggerName(loggerName)
    , readyEventName(Guid::NewGuid().ToString())
    , exitEventName(Guid::NewGuid().ToString())
{
    if (String::IsNullOrWhiteSpace(loggerName))
        throw gcnew ArgumentException("Empty or missing logger name.");

    readyEvent = gcnew System::Threading::EventWaitHandle(
        false, System::Threading::EventResetMode::ManualReset,
        readyEventName);

    exitEvent = gcnew System::Threading::EventWaitHandle(
        false, System::Threading::EventResetMode::ManualReset,
        exitEventName);

    watchDogExe = Path::Combine(
        GetCurrentAssemblyDir(), "EventTraceKit.Logger.WatchDog.exe");

    if (!File::Exists(watchDogExe))
        throw gcnew InvalidOperationException("WatchDog executable not found.");
}

WatchDog::~WatchDog()
{
    Stop();
}

void WatchDog::Start()
{
    Stop();

    readyEvent->Reset();
    exitEvent->Reset();
    watchDogProcess = gcnew Process();
    watchDogProcess->StartInfo = CreateStartInfo();

    String^ output = nullptr;
    if (!StartAndWaitUntilReady(output)) {
        Stop();
        throw gcnew Exception("WatchDog failed to start.\r\n\r\n" + output);
    }
}

static WaitHandle^ WaitHandleForProcess(Process^ process)
{
    auto resetEvent = gcnew ManualResetEvent(true);
    resetEvent->SafeWaitHandle = gcnew SafeWaitHandle(process->Handle, false);
    return resetEvent;
}

static void WaitForOrForceExit(Process^ process, int timeout)
{
    if (!process->WaitForExit(timeout))
        process->Kill();
    process->WaitForExit();
}

bool WatchDog::StartAndWaitUntilReady(String^% output)
{
    auto handler = gcnew DataReceivedEventHandler(
        this, &EventTraceKit::WatchDog::OnStdErrorReceived);

    capturedStdError = gcnew StringBuilder();
    stdErrorFinishedEvent = gcnew ManualResetEventSlim(false);
    watchDogProcess->ErrorDataReceived += handler;
    watchDogProcess->Start();
    watchDogProcess->BeginErrorReadLine();

    auto pwh = WaitHandleForProcess(watchDogProcess);

    int index = WaitHandle::WaitAny(
        gcnew array<WaitHandle^> { pwh, readyEvent }, 1500);
    bool success = index == 1;

    if (!success) {
        exitEvent->Set();
        WaitForOrForceExit(watchDogProcess, 500);
        stdErrorFinishedEvent->Wait(500);
    }

    watchDogProcess->ErrorDataReceived -= handler;
    output = capturedStdError->ToString();
    capturedStdError = nullptr;

    return success;
}

void WatchDog::Stop()
{
    if (!watchDogProcess)
        return;

    exitEvent->Set();
    WaitForOrForceExit(watchDogProcess, 500);

    delete watchDogProcess;
    watchDogProcess = nullptr;
}

ProcessStartInfo^ WatchDog::CreateStartInfo()
{
    auto startInfo = gcnew ProcessStartInfo();
    startInfo->CreateNoWindow = true;
    startInfo->UseShellExecute = false;
    startInfo->ErrorDialog = false;
    startInfo->FileName = watchDogExe;
    startInfo->RedirectStandardError = true;
    startInfo->Arguments = String::Concat(
        Process::GetCurrentProcess()->Id.ToString() + " " +
        loggerName + " " +
        readyEventName + " " +
        exitEventName);
    return startInfo;
}

void WatchDog::OnStdErrorReceived(Object^ /*sender*/, DataReceivedEventArgs^ args)
{
    if (!capturedStdError)
        return;

    if (!args->Data) {
        stdErrorFinishedEvent->Set();
        return;
    }

    capturedStdError->Append(args->Data);
}

} // namespace EventTraceKit
