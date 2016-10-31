#include "Descriptors.h"
#include "TraceLog.h"

#include "ADT/Guid.h"
#include "InteropHelper.h"
#include "ITraceLog.h"
#include "ITraceProcessor.h"
#include "ITraceSession.h"
#include "WatchDog.h"

#include <memory>
#include <string>

using namespace System;
using namespace System::Linq;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
using namespace System::Threading::Tasks;
using msclr::interop::marshal_as;

namespace msclr
{
namespace interop
{

static bool IsProviderBinary(String^ filePath)
{
    return
        filePath->EndsWith(L".exe", StringComparison::OrdinalIgnoreCase) ||
        filePath->EndsWith(L".dll", StringComparison::OrdinalIgnoreCase);
}

template<>
inline etk::TraceProviderDescriptor marshal_as(EventTraceKit::TraceProviderDescriptor^ const& provider)
{
    etk::TraceProviderDescriptor native(
        marshal_as<GUID>(provider->Id), provider->Level, provider->MatchAnyKeyword,
        provider->MatchAllKeyword);
    native.IncludeSecurityId = provider->IncludeSecurityId;
    native.IncludeTerminalSessionId = provider->IncludeTerminalSessionId;
    native.IncludeStackTrace = provider->IncludeStackTrace;
    if (provider->Manifest) {
        auto manifest = marshal_as<std::wstring>(provider->Manifest);
        if (IsProviderBinary(provider->Manifest))
            native.SetProviderBinary(manifest);
        else
            native.SetManifest(manifest);
    }

    native.ProcessIds = marshal_as_vector(provider->ProcessIds);
    native.EventIds = marshal_as_vector(provider->EventIds);

    return native;
}

} // namespace interop
} // namespace msclr

namespace EventTraceKit
{

public ref struct TraceStatistics
{
    property unsigned NumberOfBuffers;
    property unsigned FreeBuffers;
    property unsigned EventsLost;
    property unsigned BuffersWritten;
    property unsigned LogBuffersLost;
    property unsigned RealTimeBuffersLost;
    property unsigned LoggerThreadId;
};

public ref class TraceSession : public System::IDisposable
{
public:
    TraceSession(TraceSessionDescriptor^ descriptor);
    ~TraceSession() { this->!TraceSession(); }
    !TraceSession();

    void Start(TraceLog^ traceLog);
    Task^ StartAsync(TraceLog^ traceLog);
    void Stop();
    void Flush();
    TraceStatistics^ Query();

    TraceSessionInfo GetInfo()
    {
        if (!this->processor)
            return TraceSessionInfo();

        auto logFileHeader = this->processor->GetLogFileHeader();
        TraceSessionInfo info = {};
        info.StartTime = logFileHeader->StartTime.QuadPart;
        info.PerfFreq = logFileHeader->PerfFreq.QuadPart;
        info.PointerSize = logFileHeader->PointerSize;
        return info;
    }

private:
    ref struct StartAsyncHelper
    {
        StartAsyncHelper(TraceSession^ parent, TraceLog^ traceLog)
            : parent(parent), traceLog(traceLog) {}

        void Run();

        TraceSession^ parent;
        TraceLog^ traceLog;
    };

    TraceSessionDescriptor^ descriptor;
    std::wstring* loggerName = nullptr;
    std::vector<etk::TraceProviderDescriptor>* nativeProviders = nullptr;
    WatchDog^ watchDog;

    etk::ITraceSession* session = nullptr;
    etk::ITraceProcessor* processor = nullptr;
};

static std::wstring LoggerNameBase = L"EventTraceKit_54644792-9281-48E9-B69D-E82A86F98960";

static std::wstring CreateLoggerName()
{
    int pid = System::Diagnostics::Process::GetCurrentProcess()->Id;
    return LoggerNameBase + L"_" + std::to_wstring(pid);
}

static etk::TraceProperties CreateTraceProperties(TraceSessionDescriptor^ descriptor)
{
    etk::TraceProperties properties(marshal_as<GUID>(System::Guid::NewGuid()));
    if (descriptor->BufferSize.HasValue)
        properties.BufferSize = descriptor->BufferSize.Value;
    if (descriptor->MinimumBuffers.HasValue)
        properties.MinimumBuffers = descriptor->MinimumBuffers.Value;
    if (descriptor->MaximumBuffers.HasValue)
        properties.MaximumBuffers = descriptor->MaximumBuffers.Value;
    if (descriptor->LogFileName)
        properties.LogFileName = marshal_as<std::wstring>(descriptor->LogFileName);
    return properties;
}

TraceSession::TraceSession(TraceSessionDescriptor^ descriptor)
    : descriptor(descriptor)
    , loggerName(new std::wstring(CreateLoggerName()))
{
    watchDog = gcnew WatchDog(marshal_as<String^>(*loggerName));

    etk::TraceProperties properties = CreateTraceProperties(descriptor);
    auto session = etk::CreateEtwTraceSession(*loggerName, properties);

    nativeProviders = new std::vector<etk::TraceProviderDescriptor>();
    for each (TraceProviderDescriptor^ provider in descriptor->Providers)
        nativeProviders->push_back(marshal_as<etk::TraceProviderDescriptor>(provider));

    for (auto const& provider : *nativeProviders) {
        session->AddProvider(provider);
        session->EnableProvider(provider.Id);
    }

    this->session = session.release();
}

TraceSession::!TraceSession()
{
    Stop();
    delete session;
    delete watchDog;
    delete nativeProviders;
    delete loggerName;
}

void TraceSession::Start(TraceLog^ traceLog)
{
    if (!traceLog)
        throw gcnew ArgumentNullException("traceLog");

    watchDog->Start();
    session->Start();

    auto processor = etk::CreateEtwTraceProcessor(*loggerName, *nativeProviders);
    processor->SetEventSink(traceLog->Native());

    this->processor = processor.release();
    this->processor->StartProcessing();
}

Task^ TraceSession::StartAsync(TraceLog^ traceLog)
{
    if (!traceLog)
        throw gcnew ArgumentNullException("traceLog");

    auto helper = gcnew StartAsyncHelper(this, traceLog);
    return Task::Run(gcnew Action(helper, &StartAsyncHelper::Run));
}

void TraceSession::StartAsyncHelper::Run()
{
    parent->watchDog->Start();
    parent->session->Start();

    auto processor = etk::CreateEtwTraceProcessor(*parent->loggerName, *parent->nativeProviders);
    processor->SetEventSink(traceLog->Native());

    parent->processor = processor.release();
    parent->processor->StartProcessing();
}

void TraceSession::Stop()
{
    if (!processor)
        return;

    processor->StopProcessing();
    session->Stop();
    watchDog->Stop();

    delete processor;
    processor = nullptr;
}

void TraceSession::Flush()
{
    if (!processor)
        return;

    session->Flush();
}

TraceStatistics^ TraceSession::Query()
{
    if (!session)
        return gcnew TraceStatistics();

    etk::TraceStatistics nativeStats;
    session->Query(nativeStats);

    auto stats = gcnew TraceStatistics();
    stats->NumberOfBuffers = nativeStats.NumberOfBuffers;
    stats->FreeBuffers = nativeStats.FreeBuffers;
    stats->EventsLost = nativeStats.EventsLost;
    stats->BuffersWritten = nativeStats.BuffersWritten;
    stats->LogBuffersLost = nativeStats.LogBuffersLost;
    stats->RealTimeBuffersLost = nativeStats.RealTimeBuffersLost;
    stats->LoggerThreadId = nativeStats.LoggerThreadId;

    return stats;
}

} // namespace EventTraceKit
