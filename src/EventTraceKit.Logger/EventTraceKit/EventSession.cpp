#include "Descriptors.h"
#include "TraceLog.h"
#include "WatchDog.h"

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
    native.EventIdsFilterIn = provider->EventIdsFilterIn;
    native.StackWalkEventIds = marshal_as_vector(provider->StackWalkEventIds);
    native.StackWalkEventIdsFilterIn = provider->StackWalkEventIdsFilterIn;
    native.StackWalkMatchAnyKeyword = provider->StackWalkMatchAnyKeyword;
    native.StackWalkMatchAllKeyword = provider->StackWalkMatchAllKeyword;
    native.StackWalkLevel = provider->StackWalkLevel;
    native.StackWalkFilterIn = provider->StackWalkFilterIn;
    native.EnableStackWalkFilter = provider->EnableStackWalkFilter;

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
    EventSession(TraceProfileDescriptor^ profile, TraceLog^ traceLog);
    ~EventSession() { this->!EventSession(); }
    !EventSession();

    void Start();
    Task^ StartAsync();
    void Stop();
    void Flush();
    TraceStatistics^ Query();

    EventSessionInfo GetInfo() { return sessionInfo; }

private:
    ref struct StartAsyncHelper
    {
        StartAsyncHelper(EventSession^ parent)
            : parent(parent) {}

        void Run()
        {
            parent->Start();
        }

        EventSession^ parent;
    };

    TraceProfileDescriptor^ profile;
    TraceLog^ traceLog;
    std::wstring* loggerName = nullptr;
    std::vector<etk::TraceProviderDescriptor>* nativeProviders = nullptr;
    WatchDog^ watchDog;

    etk::ITraceSession* session = nullptr;
    etk::ITraceSession* kernelSession = nullptr;
    etk::ITraceProcessor* processor = nullptr;
    EventSessionInfo sessionInfo;
};

static std::wstring LoggerNameBase = L"EventTraceKit_54644792-9281-48E9-B69D-E82A86F98960";

static std::wstring CreateLoggerName()
{
    int pid = System::Diagnostics::Process::GetCurrentProcess()->Id;
    return LoggerNameBase + L"_" + std::to_wstring(pid);
}

static etk::TraceProperties CreateTraceProperties(CollectorDescriptor^ profile)
{
    etk::TraceProperties properties(marshal_as<GUID>(System::Guid::NewGuid()));
    if (profile->BufferSize.HasValue)
        properties.BufferSize = profile->BufferSize.Value;
    if (profile->MinimumBuffers.HasValue)
        properties.MinimumBuffers = profile->MinimumBuffers.Value;
    if (profile->MaximumBuffers.HasValue)
        properties.MaximumBuffers = profile->MaximumBuffers.Value;
    if (profile->LogFileName)
        properties.LogFileName = marshal_as<std::wstring>(profile->LogFileName);
    properties.CustomFlushTimer = std::chrono::duration<unsigned, std::milli>(profile->CustomFlushPeriod);
    return properties;
}

EventSession::EventSession(TraceProfileDescriptor^ profile, TraceLog^ traceLog)
    : profile(profile)
    , traceLog(traceLog)
    , loggerName(new std::wstring(CreateLoggerName()))
{
    if (profile->Collectors->Count == 0)
        throw gcnew System::ArgumentException(L"profile");
    if (!traceLog)
        throw gcnew System::ArgumentNullException(L"traceLog");

    watchDog = gcnew WatchDog(marshal_as<String^>(*loggerName));
    traceLog->UpdateTraceData(profile);

    for each (auto collector in profile->Collectors) {
        if (auto systemCollector = dynamic_cast<SystemCollectorDescriptor^>(collector)) {
            auto traceProperties = CreateTraceProperties(systemCollector);
            auto session = etk::CreateEtwTraceSession(L"NT Kernel Logger", traceProperties);
            session->SetKernelProviders(systemCollector->KernelFlags, true);
            this->kernelSession = session.release();
        } else {
            auto eventCollector = static_cast<EventCollectorDescriptor^>(collector);
            auto traceProperties = CreateTraceProperties(eventCollector);
            auto session = etk::CreateEtwTraceSession(*loggerName, traceProperties);

            nativeProviders = new std::vector<etk::TraceProviderDescriptor>();
            for each (auto provider in eventCollector->Providers)
                nativeProviders->push_back(marshal_as<etk::TraceProviderDescriptor>(provider));

            for (auto const& provider : *nativeProviders) {
                session->AddProvider(provider);
                session->EnableProvider(provider.Id);
            }

            this->session = session.release();
        }
    }
}

EventSession::!EventSession()
{
    Stop();
    delete session;
    delete kernelSession;
    delete watchDog;
    delete nativeProviders;
    delete loggerName;
}

void EventSession::Start()
{
    watchDog->Start();

    HRESULT hr;
    if (kernelSession) {
        hr = kernelSession->Start();
        if (FAILED(hr))
            throw gcnew Win32Exception(hr);
    }

    if (session) {
        hr = session->Start();
        if (FAILED(hr)) {
            if (kernelSession)
                (void)kernelSession->Stop();
            throw gcnew Win32Exception(hr);
        }
    }

    std::vector<std::wstring_view> loggerNames;
    if (session)
        loggerNames.push_back(*loggerName);
    if (kernelSession)
        loggerNames.push_back(L"NT Kernel Logger");

    auto processor = etk::CreateEtwTraceProcessor(loggerNames, *nativeProviders);
    processor->SetEventSink(traceLog->Native());

    this->processor = processor.release();
    this->processor->StartProcessing();

    auto logFileHeader = this->processor->GetLogFileHeader();
    sessionInfo.StartTime = logFileHeader->StartTime.QuadPart;
    sessionInfo.PerfFreq = logFileHeader->PerfFreq.QuadPart;
    sessionInfo.PointerSize = logFileHeader->PointerSize;
}

Task^ EventSession::StartAsync()
{
    return Task::Run(gcnew Action(this, &EventSession::Start));
}

void EventSession::Stop()
{
    if (!processor)
        return;

    processor->StopProcessing();
    if (session)
        session->Stop();
    if (kernelSession)
        kernelSession->Stop();
    watchDog->Stop();

    delete processor;
    processor = nullptr;
}

void EventSession::Flush()
{
    if (!processor)
        return;

    if (session)
        session->Flush();
    if (kernelSession)
        kernelSession->Flush();
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
