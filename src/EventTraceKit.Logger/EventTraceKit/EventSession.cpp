#include "Descriptors.h"
#include "TraceLog.h"
#include "WatchDog.h"

#include "ADT/Guid.h"
#include "InteropHelper.h"
#include "ITraceLog.h"
#include "ITraceProcessor.h"
#include "ITraceSession.h"

#include <memory>
#include <string>

using namespace System;
using namespace System::Collections::Generic;
using namespace System::ComponentModel;
using namespace System::Linq;
using namespace System::Runtime::InteropServices;
using namespace System::Threading::Tasks;
using namespace EventTraceKit::Tracing;
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
inline etk::TraceProviderDescriptor marshal_as(EventProviderDescriptor^ const& provider)
{
    etk::TraceProviderDescriptor native(
        marshal_as<GUID>(provider->Id), provider->Level, provider->MatchAnyKeyword,
        provider->MatchAllKeyword);
    native.IncludeSecurityId = provider->IncludeSecurityId;
    native.IncludeTerminalSessionId = provider->IncludeTerminalSessionId;
    native.IncludeStackTrace = provider->IncludeStackTrace;

    if (provider->ExecutableName)
        native.ExecutableName = marshal_as<std::wstring>(provider->ExecutableName);
    native.ProcessIds = marshal_as_vector(provider->ProcessIds);
    native.EventIds = marshal_as_vector(provider->EventIds);
    native.EnableEventIds = provider->EnableEventIds;
    native.StackWalkEventIds = marshal_as_vector(provider->StackWalkEventIds);
    native.EnableStackWalkEventIds = provider->EnableStackWalkEventIds;

    if (provider->Manifest) {
        auto manifest = marshal_as<std::wstring>(provider->Manifest);
        if (IsProviderBinary(provider->Manifest))
            native.SetProviderBinary(manifest);
        else
            native.SetManifest(manifest);
    }

    return native;
}

} // namespace interop
} // namespace msclr

namespace EventTraceKit::Tracing
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

public ref class EventSession : public System::IDisposable
{
public:
    EventSession(TraceProfileDescriptor^ descriptor);
    ~EventSession() { this->!EventSession(); }
    !EventSession();

    void Start(TraceLog^ traceLog);
    Task^ StartAsync(TraceLog^ traceLog);
    void Stop();
    void Flush();
    TraceStatistics^ Query();

    EventSessionInfo GetInfo() { return sessionInfo; }

private:
    ref struct StartAsyncHelper
    {
        StartAsyncHelper(EventSession^ parent, TraceLog^ traceLog)
            : parent(parent), traceLog(traceLog) {}

        void Run();

        EventSession^ parent;
        TraceLog^ traceLog;
    };

    TraceProfileDescriptor^ descriptor;
    std::wstring* loggerName = nullptr;
    std::vector<etk::TraceProviderDescriptor>* nativeProviders = nullptr;
    WatchDog^ watchDog;

    etk::ITraceSession* session = nullptr;
    etk::ITraceProcessor* processor = nullptr;
    EventSessionInfo sessionInfo;
};

static std::wstring LoggerNameBase = L"EventTraceKit_54644792-9281-48E9-B69D-E82A86F98960";

static std::wstring CreateLoggerName()
{
    int pid = System::Diagnostics::Process::GetCurrentProcess()->Id;
    return LoggerNameBase + L"_" + std::to_wstring(pid);
}

static etk::TraceProperties CreateTraceProperties(EventCollectorDescriptor^ descriptor)
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

EventSession::EventSession(TraceProfileDescriptor^ descriptor)
    : descriptor(descriptor)
    , loggerName(new std::wstring(CreateLoggerName()))
{
    watchDog = gcnew WatchDog(marshal_as<String^>(*loggerName));

    auto collector = static_cast<EventCollectorDescriptor^>(descriptor->Collectors[0]);
    auto traceProperties = CreateTraceProperties(collector);
    auto session = etk::CreateEtwTraceSession(*loggerName, traceProperties);

    nativeProviders = new std::vector<etk::TraceProviderDescriptor>();
    for each (auto provider in collector->Providers)
        nativeProviders->push_back(marshal_as<etk::TraceProviderDescriptor>(provider));

    for (auto const& provider : *nativeProviders) {
        session->AddProvider(provider);
        session->EnableProvider(provider.Id);
    }

    this->session = session.release();
}

EventSession::!EventSession()
{
    Stop();
    delete session;
    delete watchDog;
    delete nativeProviders;
    delete loggerName;
}

void EventSession::Start(TraceLog^ traceLog)
{
    if (!traceLog)
        throw gcnew ArgumentNullException("traceLog");

    watchDog->Start();
    HRESULT hr = session->Start();
    if (FAILED(hr))
        throw gcnew Win32Exception(hr);

    auto processor = etk::CreateEtwTraceProcessor(*loggerName, *nativeProviders);
    processor->SetEventSink(traceLog->Native());

    this->processor = processor.release();
    this->processor->StartProcessing();

    auto logFileHeader = this->processor->GetLogFileHeader();
    sessionInfo.StartTime = logFileHeader->StartTime.QuadPart;
    sessionInfo.PerfFreq = logFileHeader->PerfFreq.QuadPart;
    sessionInfo.PointerSize = logFileHeader->PointerSize;
}

Task^ EventSession::StartAsync(TraceLog^ traceLog)
{
    if (!traceLog)
        throw gcnew ArgumentNullException("traceLog");

    auto helper = gcnew StartAsyncHelper(this, traceLog);
    return Task::Run(gcnew Action(helper, &StartAsyncHelper::Run));
}

void EventSession::StartAsyncHelper::Run()
{
    parent->Start(traceLog);
}

void EventSession::Stop()
{
    if (!processor)
        return;

    processor->StopProcessing();
    session->Stop();
    watchDog->Stop();

    delete processor;
    processor = nullptr;
}

void EventSession::Flush()
{
    if (!processor)
        return;

    session->Flush();
}

TraceStatistics^ EventSession::Query()
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

} // namespace EventTraceKit::Tracing
