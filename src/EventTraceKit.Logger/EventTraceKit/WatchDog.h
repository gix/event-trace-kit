#pragma once
#if __cplusplus_cli

namespace EventTraceKit
{

public ref class WatchDog : public System::IDisposable
{
public:
    WatchDog(System::String^ loggerName);
    ~WatchDog();
    void Start();
    void Stop();

private:
    System::Diagnostics::ProcessStartInfo^ CreateStartInfo();
    void OnStdErrorReceived(System::Object^ sender, System::Diagnostics::DataReceivedEventArgs^ args);
    bool StartAndWaitUntilReady(System::String^% output);

    initonly System::String^ loggerName;
    initonly System::String^ watchDogExe;
    initonly System::String^ readyEventName;
    initonly System::Threading::EventWaitHandle^ readyEvent;
    initonly System::String^ exitEventName;
    initonly System::Threading::EventWaitHandle^ exitEvent;

    System::Diagnostics::Process^ watchDogProcess;
    System::Threading::ManualResetEventSlim^ stdErrorFinishedEvent;
    System::Text::StringBuilder^ capturedStdError;
};

} // namespace EventTraceKit

#endif // __cplusplus_cli
