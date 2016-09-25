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

public ref class TraceProviderDescriptor
{
public:
    TraceProviderDescriptor(Guid id)
    {
        Id = id;
        Level = 0xFF;
        MatchAnyKeyword = 0xFFFFFFFFFFFFFFFFULL;
        MatchAllKeyword = 0;

        ProcessIds = gcnew List<unsigned>();
        EventIds = gcnew List<uint16_t>();
    }

    property Guid Id;
    property uint8_t Level;
    property uint64_t MatchAnyKeyword;
    property uint64_t MatchAllKeyword;

    property bool IncludeSecurityId;
    property bool IncludeTerminalSessionId;
    property bool IncludeStackTrace;

    property String^ Manifest;
    property List<unsigned>^ ProcessIds;
    property List<uint16_t>^ EventIds;
};

public ref class TraceSessionDescriptor
{
public:
    TraceSessionDescriptor()
    {
        Providers = gcnew List<TraceProviderDescriptor^>();
    }

    property Nullable<unsigned> BufferSize;
    property Nullable<unsigned> MinimumBuffers;
    property Nullable<unsigned> MaximumBuffers;
    property String^ LogFileName;
    property IList<TraceProviderDescriptor^>^ Providers;
};

} // namespace EventTraceKit

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

public value struct TraceSessionInfo
{
    property long long StartTime;
    property long long PerfFreq;
    property unsigned PointerSize;
};

public value struct EventInfo
{
    property IntPtr EventRecord;
    property IntPtr TraceEventInfo;
    property IntPtr TraceEventInfoSize;
};

public ref class TraceLog : public System::IDisposable
{
public:
    [UnmanagedFunctionPointer(CallingConvention::Cdecl)]
    delegate void EventsChangedDelegate(UIntPtr);

    TraceLog()
    {
        onEventsChangedCallback = gcnew EventsChangedDelegate(this, &TraceLog::OnEventsChanged);

        auto nativeCallback = static_cast<etk::TraceLogEventsChangedCallback*>(
            Marshal::GetFunctionPointerForDelegate(onEventsChangedCallback).ToPointer());
        auto nativeLog = etk::CreateEtwTraceLog(nativeCallback);
        if (!nativeLog)
            throw gcnew Exception("Failed to create native trave log.");

        this->nativeLog = nativeLog.release();
    }

    ~TraceLog() { this->!TraceLog(); }
    !TraceLog() { delete nativeLog; }

    event Action<UIntPtr>^ EventsChanged;

    property unsigned EventCount
    {
        unsigned get() { return nativeLog->GetEventCount(); }
    }

    void Clear() { nativeLog->Clear(); }

    EventInfo GetEvent(int index)
    {
        auto eventInfo = nativeLog->GetEvent(index);
        EventInfo info;
        info.EventRecord = IntPtr(eventInfo.Record());
        info.TraceEventInfo = IntPtr(eventInfo.Info());
        info.TraceEventInfoSize = IntPtr((void*)eventInfo.InfoSize());
        return info;
    }

internal:
    etk::ITraceLog* Native() { return nativeLog;  }

private:
    void OnEventsChanged(UIntPtr newCount)
    {
        EventsChanged(newCount);
    }

    EventsChangedDelegate^ onEventsChangedCallback;
    etk::ITraceLog* nativeLog;
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
