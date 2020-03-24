#include <sdkddkver.h>
#undef _WIN32_WINNT
#define _WIN32_WINNT _WIN32_WINNT_WINBLUE

#include "ADT/VarStructPtr.h"
#include "Support/CompilerSupport.h"
#include "InteropHelper.h"

#include <memory>
#include <windows.h>
#include <tdh.h>

using namespace System;
using namespace System::Collections::Generic;
using namespace System::ComponentModel;
using namespace System::Runtime::InteropServices;
using namespace System::Threading;
using msclr::interop::marshal_as;

namespace
{
using namespace etk;

template<typename T, typename U>
ETK_ALWAYS_INLINE
T GetAt(U* ptr, size_t offset)
{
    return reinterpret_cast<T>(reinterpret_cast<uint8_t*>(ptr) + offset);
}

ULONG EnumerateProviders(vstruct_ptr<PROVIDER_ENUMERATION_INFO>& buffer)
{
    ULONG bufferSize = 0;
    ULONG ec = TdhEnumerateProviders(nullptr, &bufferSize);
    if (ec != ERROR_INSUFFICIENT_BUFFER)
        return ec;

    auto local = etk::make_vstruct<PROVIDER_ENUMERATION_INFO>(bufferSize);
    ec = TdhEnumerateProviders(local.get(), &bufferSize);
    if (ec != ERROR_SUCCESS)
        return ec;

    buffer = std::move(local);
    return ERROR_SUCCESS;
}

ULONG EnumerateManifestProviderEvents(
    GUID const& providerId, vstruct_ptr<PROVIDER_EVENT_INFO>& buffer)
{
    ULONG bufferSize = 0;
    ULONG ec = TdhEnumerateManifestProviderEvents(
        const_cast<GUID*>(&providerId), nullptr, &bufferSize);
    if (ec != ERROR_INSUFFICIENT_BUFFER)
        return ec;

    auto local = etk::make_vstruct<PROVIDER_EVENT_INFO>(bufferSize);
    ec = TdhEnumerateManifestProviderEvents(
        const_cast<GUID*>(&providerId), local.get(), &bufferSize);
    if (ec != ERROR_SUCCESS)
        return ec;

    buffer = std::move(local);
    return ERROR_SUCCESS;
}

ULONG GetManifestEventInformation(
    GUID const& providerId, EVENT_DESCRIPTOR const& eventDescriptor,
    vstruct_ptr<TRACE_EVENT_INFO>& buffer)
{
    ULONG bufferSize = 0;
    ULONG ec = TdhGetManifestEventInformation(
        const_cast<GUID*>(&providerId),
        const_cast<EVENT_DESCRIPTOR*>(&eventDescriptor), nullptr, &bufferSize);
    if (ec != ERROR_INSUFFICIENT_BUFFER)
        return ec;

    auto local = etk::make_vstruct<TRACE_EVENT_INFO>(bufferSize);
    ec = TdhGetManifestEventInformation(
        const_cast<GUID*>(&providerId),
        const_cast<EVENT_DESCRIPTOR*>(&eventDescriptor), local.get(),
        &bufferSize);
    if (ec != ERROR_SUCCESS)
        return ec;

    buffer = std::move(local);
    return ERROR_SUCCESS;
}

} // namespace

namespace EventTraceKit
{

[StructLayout(LayoutKind::Sequential)]
public value struct EventDescriptor
{
    property unsigned short Id;
    property unsigned char Version;
    property unsigned char Channel;
    property unsigned char Level;
    property unsigned char Opcode;
    property unsigned short Task;
    property unsigned long long Keyword;
};

public ref class ProviderEventInfo
{
public:
    property EventDescriptor Descriptor;
    property String^ Message;
};

public ref class ProviderInfo
{
public:
    property Guid Id;
    property String^ Name;
    property bool IsMOF;
    property Exception^ Exception;
    property List<ProviderEventInfo^>^ Events;
};

public value struct ManifestInfoProcess
{
    ManifestInfoProcess(int processed, int totalProviders)
    {
        Processed = processed;
        TotalProviders = totalProviders;
    }

    property int Processed;
    property int TotalProviders;
};

public ref class ManifestInfo
{
public:
    static ManifestInfo^ Enumerate(
        CancellationToken cancel, IProgress<ManifestInfoProcess>^ progress);

    property List<ProviderInfo^>^ Providers;

private:
    ManifestInfo()
    {
    }
};

ManifestInfo^ ManifestInfo::Enumerate(
    CancellationToken cancel, IProgress<ManifestInfoProcess>^ progress)
{
    auto info = gcnew ManifestInfo();
    ULONG ec;

    etk::vstruct_ptr<PROVIDER_ENUMERATION_INFO> pei;
    ec = EnumerateProviders(pei);
    if (ec != ERROR_SUCCESS)
        throw gcnew Exception(L"Failed to enumerate providers.",
                              gcnew Win32Exception((int)ec));

    info->Providers = gcnew List<ProviderInfo^>();
    for (ULONG i = 0; i < pei->NumberOfProviders; ++i) {
        if (cancel.IsCancellationRequested)
            break;

        progress->Report(ManifestInfoProcess((int)i, (int)pei->NumberOfProviders));

        auto& tpi = pei->TraceProviderInfoArray[i];
        auto providerInfo = gcnew ProviderInfo();
        providerInfo->Id = marshal_as<Guid>(tpi.ProviderGuid);
        if (tpi.ProviderNameOffset != 0)
            providerInfo->Name = gcnew String(
                GetAt<wchar_t const*>(pei.get(), tpi.ProviderNameOffset));
        providerInfo->IsMOF = tpi.SchemaSource != 0;
        info->Providers->Add(providerInfo);

        etk::vstruct_ptr<PROVIDER_EVENT_INFO> eventInfos;
        ec = EnumerateManifestProviderEvents(tpi.ProviderGuid, eventInfos);
        if (ec != ERROR_SUCCESS) {
            providerInfo->Exception = gcnew Exception(
                L"Failed to enumerate provider events.",
                gcnew Win32Exception((int)ec));
            continue;
        }

        providerInfo->Events = gcnew List<ProviderEventInfo^>();
        for (ULONG j = 0; j < eventInfos->NumberOfEvents; ++j) {
            auto& ed = eventInfos->EventDescriptorsArray[j];
            auto eventInfo = gcnew ProviderEventInfo();
            providerInfo->Events->Add(eventInfo);

            EventDescriptor descriptor;
            std::memcpy(&descriptor, &ed, sizeof(EVENT_DESCRIPTOR));
            eventInfo->Descriptor = descriptor;

            etk::vstruct_ptr<TRACE_EVENT_INFO> tei;
            ec = GetManifestEventInformation(tpi.ProviderGuid, ed, tei);
            if (ec != ERROR_SUCCESS)
                continue;

            if (tei->EventMessageOffset != 0)
                eventInfo->Message = gcnew String(
                    GetAt<wchar_t const*>(tei.get(), tei->EventMessageOffset));
        }
    }

    if (cancel.IsCancellationRequested)
        return nullptr;

    progress->Report(ManifestInfoProcess(
        (int)pei->NumberOfProviders, (int)pei->NumberOfProviders));

    return info;
}

} // namespace EventTraceKit
