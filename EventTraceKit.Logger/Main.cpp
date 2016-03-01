#include "EtwTraceProcessor.h"
#include "EtwTraceSession.h"

#include "ADT/Guid.h"
#include "ADT/Handle.h"
#include "ADT/StringView.h"
#include "ADT/VarStructPtr.h"
#include "ADT/WaitEvent.h"
#include "Support/ByteCount.h"
#include "Support/CountOf.h"
#include "Support/Debug.h"
//#include "Diagnostics/DebugUtils.h"
#include "Support/ErrorHandling.h"

#include <atomic>
#include <cstdarg>
#include <cstddef>
#include <cstdio>
#include <memory>
#include <system_error>
#include <unordered_map>
#include <vector>

#include <evntrace.h>
#include <evntcons.h>
#include <strsafe.h>
#include <Tdh.h>
#include <windows.h>
#include <winevt.h>
#include <wmistr.h>

//#define DBGHELP_TRANSLATE_TCHAR
#include <DbgHelp.h>

using namespace etk;

namespace
{

template<typename T>
inline void hash_combine(std::size_t& seed, T const& value)
{
    seed ^= std::hash<T>()(value) + 0x9E3779B9 + (seed << 6) + (seed >> 2);
}

} // namespace


namespace std
{

template<>
struct hash<GUID>
{
public:
    std::size_t operator ()(GUID const& guid) const
    {
        static_assert(sizeof(GUID) == 2 * sizeof(uint64_t), "Invariant violated");

        std::size_t value = 0;
        ::hash_combine(value, reinterpret_cast<uint64_t const*>(&guid)[0]);
        ::hash_combine(value, reinterpret_cast<uint64_t const*>(&guid)[1]);
        return value;
    }
};

} // namespace std

EVT_HANDLE SubscribeEvents(wchar_t const* path, wchar_t const* query = nullptr);

namespace etk
{

static ManualResetEvent g_userCancelledEvent;

void LogMessageArgs(_In_z_ _Printf_format_string_ wchar_t const* format,
                    va_list args)
{
    //wchar_t buffer[512] = {};
    //vswprintf(buffer, 512, format, args);
    //fwprintf(stdout, buffer);
    vfwprintf(stdout, format, args);
}

void LogMessage(_In_z_ _Printf_format_string_ wchar_t const* format, ...)
{
    va_list args;
    va_start(args, format);
    LogMessageArgs(format, args);
    va_end(args);
}

HANDLE exitEvent = nullptr;

class TdhManifestLoadCookie
{
public:
    TdhManifestLoadCookie()
        : path()
        , loaded(false) {}

    TdhManifestLoadCookie(std::wstring path)
        : path(std::move(path))
        , loaded(false) {}

    ~TdhManifestLoadCookie()
    {
        if (loaded)
            TdhUnloadManifest(const_cast<wchar_t*>(path.c_str()));
    }

    TdhManifestLoadCookie(TdhManifestLoadCookie&& source)
        : path(std::move(source.path))
        , loaded(std::exchange(source.loaded, false))
    {
    }

    TdhManifestLoadCookie& operator =(TdhManifestLoadCookie&& source)
    {
        using namespace std;
        path = std::move(source.path);
        loaded = std::exchange(source.loaded, false);
        return *this;
    }

    TdhManifestLoadCookie(TdhManifestLoadCookie const&) = delete;
    TdhManifestLoadCookie& operator =(TdhManifestLoadCookie const&) = delete;

private:
    std::wstring path;
    bool loaded;
};

TdhManifestLoadCookie LoadManifest(wchar_t const* path)
{
    TdhManifestLoadCookie cookie(path);

    TDHSTATUS ec = TdhLoadManifest(const_cast<wchar_t*>(path));
    if (ec != ERROR_SUCCESS) {
        switch (ec) {
        case ERROR_FILE_NOT_FOUND:
            LogMessage(L"Failed to load manifest \"%ls\": ec=0x%lX (ERROR_FILE_NOT_FOUND)\n", path, ec);
            break;
        case ERROR_INVALID_PARAMETER:
            LogMessage(L"Failed to load manifest \"%ls\": ec=0x%lX (ERROR_INVALID_PARAMETER)\n", path, ec);
            break;
        case ERROR_XML_PARSE_ERROR:
            LogMessage(L"Failed to load manifest \"%ls\": ec=0x%lX (ERROR_XML_PARSE_ERROR)\n", path, ec);
            break;
        default:
            LogMessage(L"Failed to load manifest \"%ls\": ec=0x%lX\n", path, ec);
            break;
        }

        return {};
    }

    return cookie;
}

TdhManifestLoadCookie AddSculptorProvider(EtwTraceSession& session)
{
    static GUID const ProviderId = { 0x716EFEF7, 0x5AC2, 0x4EE0,{ 0x82, 0x77, 0xD9, 0x22, 0x64, 0x11, 0xA1, 0x55 } };
    //static GUID const ProviderId = { 0x00000000, 0x0000, 0x0000, { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 } };
    //ProviderState provider(ProviderId, 0xFF, 0x7FFFFFFF, 0x7FFFFFFF);
    ProviderState provider(ProviderId, 0xFF, 0xFFFFFFFFFFFFFFFFULL, 0);

    wchar_t path[] = { L"C:\\Users\\nrieck\\dev\\ffmf\\src\\Sculptor\\Sculptor.man" };
    //ULONG ec = TdhLoadManifest(L"C:\\Users\\nrieck\\dev\\InstrManifestCompiler\\src\\InstrManifestCompiler.Tests\\Input\\Test2.man");
    session.AddProvider(provider);
    session.EnableProvider(provider.Id);
    return LoadManifest(path);
}

static GUID const Microsoft_Windows_MediaFoundation_ProviderId                  = { 0xA7364E1A, 0x894F, 0x4B3D, { 0xA9, 0x30, 0x2E, 0xD9, 0xC8, 0xC4, 0xC8, 0x11 } };
static GUID const Microsoft_Windows_MediaFoundation_Platform_ProviderId         = { 0xBC97B970, 0xD001, 0x482F, { 0x87, 0x45, 0xB8, 0xD7, 0xD5, 0x75, 0x9F, 0x99 } };
static GUID const Microsoft_Windows_MediaFoundation_Performance_Core_ProviderId = { 0xB20E65AC, 0xC905, 0x4014, { 0x8F, 0x78, 0x1B, 0x6A, 0x50, 0x81, 0x42, 0xEB } };
static GUID const Microsoft_Windows_MediaFoundation_Performance_ProviderId      = { 0xF404B94E, 0x27E0, 0x4384, { 0xBF, 0xE8, 0x1D, 0x8D, 0x39, 0x0B, 0x0A, 0xA3 } };

TdhManifestLoadCookie AddMFPerformanceProvider(EtwTraceSession& session)
{
    session.AddProvider({
        Microsoft_Windows_MediaFoundation_Performance_ProviderId, 0xFF, 0xFFFFFFFFFFFFFFFFULL, 0 });

    session.EnableProvider(Microsoft_Windows_MediaFoundation_Performance_ProviderId);
    //return LoadManifest(L"D:\\mfplat.man");
    wchar_t path[] = { L"C:\\Windows\\system32\\mfplat.dll" };
    TDHSTATUS st = TdhLoadManifestFromBinary(path);
    return{};
}

void RunTrace()
{
    exitEvent = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    if (!exitEvent) {
        LogMessage(L"Failed to create exit event: ec=0x%lX\n", GetLastError());
        return;
    }

    EVT_HANDLE hSub = 0; //SubscribeEvents(L"TestProvider-TestProduct-TestComponent/Operational");
    if (!hSub) {
        //LogMessage(L"Failed to subscribe to channel.\n");
        //return;
    }

    TraceProperties properties(Guid::ToGUID(Guid::Create()));
    std::wstring loggerName = L"RTLogSession0";

    auto session = std::make_unique<EtwTraceSession>(loggerName, properties);
    //auto sculptorCookie = AddSculptorProvider(*session);
    auto mfPerfCookie = AddMFPerformanceProvider(*session);
    session->Start();

    auto processor = std::make_unique<EtwTraceProcessor>(loggerName);
    processor->StartProcessing();

    for (;;) {
        if (g_userCancelledEvent.Wait(std::chrono::milliseconds(500))) {
            LogMessage(L"Exiting: user-cancellation request\n");
            break;
        }

        session->Flush();

        if (processor->IsEndOfTracing()) {
            LogMessage(L"Exiting: end-of-trace reached\n");
            break;
        }
    }

    if (!processor->IsEndOfTracing()) {
        session->Stop();
        LogMessage(L"Waiting for processor to shut down...");
        while (!processor->IsEndOfTracing()) {
            Sleep(500);
            LogMessage(L".");
        }
        LogMessage(L" done\n");
    }

    if (hSub)
        EvtClose(hSub);
    SetEvent(exitEvent);
}

} // namespace tracemon

void NotifyError(HWND hwnd, wchar_t const* errorMessage, HRESULT hrErr)
{
    size_t const kMessageLen = 512;
    wchar_t message[kMessageLen];

    HRESULT hr = StringCchPrintfW(message, kMessageLen, L"%ls (HRESULT = 0x%X)", errorMessage, hrErr);
    if (SUCCEEDED(hr)) {
        int ret = MessageBoxW(hwnd, message, nullptr, MB_OK | MB_ICONERROR);
        if (ret == 0)
            MessageBoxW(NULL, message, nullptr, MB_OK | MB_ICONERROR);
    }
}

struct EvtHandleTraits
{
    typedef EVT_HANDLE HandleType;
    static HandleType InvalidHandle() noexcept { return NULL; }
    static bool IsValid(HandleType h) noexcept { return h != InvalidHandle(); }
    static void Close(HandleType h) noexcept   { ::EvtClose(h); }
};

using EvtHandle = Handle<EvtHandleTraits>;

std::wstring GetProperty(EvtHandle const& provider,
                         EVT_PUBLISHER_METADATA_PROPERTY_ID propertyId)
{
    EVT_VARIANT variant = {};
    DWORD bufferUsed = 0;
    if (!EvtGetPublisherMetadataProperty(
        provider, propertyId, 0, sizeof(variant), &variant, &bufferUsed)) {
        return std::wstring();
    }

    if (variant.Type != EvtVarTypeString || variant.StringVal == nullptr)
        return std::wstring();

    return std::wstring(variant.StringVal);
}

DWORD WINAPI SubscriptionCallback(EVT_SUBSCRIBE_NOTIFY_ACTION action, PVOID pContext, EVT_HANDLE hEvent);
DWORD PrintEvent(EVT_HANDLE hEvent);

EVT_HANDLE SubscribeEvents(wchar_t const* path, wchar_t const* query)
{
    // Subscribe to events beginning with the oldest event in the channel. The subscription
    // will return all current events in the channel and any future events that are raised
    // while the application is active.
    EVT_HANDLE hSub = EvtSubscribe(
        nullptr, nullptr, path, query, nullptr, nullptr,
        SubscriptionCallback, EvtSubscribeStartAtOldestRecord);
    if (hSub == NULL) {
        DWORD ec = GetLastError();
        if (ec == ERROR_EVT_CHANNEL_NOT_FOUND)
            wprintf(L"Channel %ls was not found.\n", path);
        else if (ec == ERROR_EVT_INVALID_QUERY)
            // You can call EvtGetExtendedStatus to get information as to why the query is not valid.
            wprintf(L"The query \"%ls\" is not valid.\n", query);
        else
            wprintf(L"EvtSubscribe failed with %lu.\n", ec);

        return NULL;
    }

    return hSub;
}

DWORD WINAPI SubscriptionCallback(
    EVT_SUBSCRIBE_NOTIFY_ACTION action, PVOID pContext, EVT_HANDLE hEvent)
{
    UNREFERENCED_PARAMETER(pContext);
    
    DWORD st = ERROR_SUCCESS;
    switch (action) {
        // You should only get the EvtSubscribeActionError action if your subscription flags 
        // includes EvtSubscribeStrict and the channel contains missing event records.
        case EvtSubscribeActionError:
            if (ERROR_EVT_QUERY_RESULT_STALE == (DWORD)hEvent) {
                wprintf(L"The subscription callback was notified that event records are missing.\n");
                // Handle if this is an issue for your application.
            } else {
                wprintf(L"The subscription callback received the following Win32 error: %lu\n", (DWORD)hEvent);
            }
            break;

        case EvtSubscribeActionDeliver:
            if (ERROR_SUCCESS != (st = PrintEvent(hEvent))) {
                goto cleanup;
            }
            break;

        default:
            wprintf(L"SubscriptionCallback: Unknown action.\n");
    }

cleanup:
    if (st != ERROR_SUCCESS) {
        // End subscription - Use some kind of IPC mechanism to signal
        // your application to close the subscription handle.
    }

    return st; // The service ignores the returned status.
}

// Render the event as an XML string and print it.
DWORD PrintEvent(EVT_HANDLE hEvent)
{
    DWORD status = ERROR_SUCCESS;
    DWORD dwBufferSize = 0;
    DWORD dwBufferUsed = 0;
    DWORD dwPropertyCount = 0;
    LPWSTR pRenderedContent = NULL;

    if (!EvtRender(NULL, hEvent, EvtRenderEventXml, dwBufferSize, pRenderedContent, &dwBufferUsed, &dwPropertyCount))
    {
        if (ERROR_INSUFFICIENT_BUFFER == (status = GetLastError()))
        {
            dwBufferSize = dwBufferUsed;
            pRenderedContent = (LPWSTR)malloc(dwBufferSize);
            if (pRenderedContent)
            {
                EvtRender(NULL, hEvent, EvtRenderEventXml, dwBufferSize, pRenderedContent, &dwBufferUsed, &dwPropertyCount);
            }
            else
            {
                wprintf(L"malloc failed\n");
                status = ERROR_OUTOFMEMORY;
                goto cleanup;
            }
        }

        if (ERROR_SUCCESS != (status = GetLastError()))
        {
            wprintf(L"EvtRender failed with %d\n", status);
            goto cleanup;
        }
    }

    wprintf(L"%ls\n\n", pRenderedContent);

cleanup:

    if (pRenderedContent)
        free(pRenderedContent);

    return status;
}


void DumpProviders()
{
    std::vector<wchar_t> buffer;
    std::vector<std::wstring> providers;

    EvtHandle peh(EvtOpenPublisherEnum(nullptr, 0));
    if (!peh) {
        wprintf(L"EvtOpenPublisherEnum failed with %d\n", GetLastError());
        return;
    }

    for (;;) {
        DWORD bufferUsed = 0;
        if (!EvtNextPublisherId(peh, static_cast<DWORD>(buffer.size()), buffer.data(), &bufferUsed)) {
            DWORD ec = GetLastError();
            if (ec == ERROR_NO_MORE_ITEMS)
                break;
            if (ec == ERROR_INSUFFICIENT_BUFFER) {
                buffer.resize(bufferUsed);
                continue;
            }
            if (ec != ERROR_SUCCESS) {
                wprintf(L"EvtNextPublisherId failed with %d\n", ec);
                break;
            }
        }
        providers.push_back(buffer.data());
    }
    peh.Close();

    std::vector<uint8_t> metadataBuffer;
    for (auto const& provider : providers) {
        wprintf(L"%ls\n", provider.c_str());
        EvtHandle pubMetaHandle(EvtOpenPublisherMetadata(nullptr, provider.c_str(), nullptr, 0, 0));
        if (!pubMetaHandle) {
            wprintf(L"  Retrieving publisher metadata failed for '%ls' with 0x%08lX.\n",
                    provider.c_str(), GetLastError());
            continue;
        }

        std::wstring resFilePath = GetProperty(pubMetaHandle, EvtPublisherMetadataResourceFilePath);
        std::wstring paramFilePath = GetProperty(pubMetaHandle, EvtPublisherMetadataParameterFilePath);
        std::wstring msgFilePath = GetProperty(pubMetaHandle, EvtPublisherMetadataMessageFilePath);

        wprintf(L"  res:   %ls\n", resFilePath.c_str());
        wprintf(L"  param: %ls\n", paramFilePath.c_str());
        wprintf(L"  msg:   %ls\n", msgFilePath.c_str());

        EvtHandle evtEnumHandle(EvtOpenEventMetadataEnum(pubMetaHandle, 0));
        if (!evtEnumHandle) {
            wprintf(L"  Opening event metadata enum failed for '%ls' with 0x%08lX.\n",
                    provider.c_str(), GetLastError());
            continue;
        }

        for (unsigned id = 0; ; ++id) {
            EvtHandle evtMetaHandle(EvtNextEventMetadata(evtEnumHandle, 0));
            if (!evtMetaHandle) {
                DWORD ec = GetLastError();
                if (ec == ERROR_NO_MORE_ITEMS)
                    break;
                if (ec != ERROR_SUCCESS) {
                    wprintf(L"  Retrieving next event metadata failed with 0x%08lX.\n",
                            GetLastError());
                    break;
                }
            }

            DWORD bufferUsed = 0;

            EVT_VARIANT variant = {};
            if (!EvtGetEventMetadataProperty(
                evtMetaHandle, EventMetadataEventMessageID, 0, sizeof(variant), &variant, &bufferUsed)) {
                wprintf(L"  Retrieving metadata property failed with 0x%08lX.\n",
                        GetLastError());
                continue;
            }

            if (variant.Type != EvtVarTypeUInt32) {
                wprintf(L"  Unknown variant type '%lu'.\n", variant.Type);
                continue;
            }

            if (variant.UInt32Val == -1)
                continue;

            unsigned messageId = variant.UInt32Val;

            if (!EvtGetEventMetadataProperty(
                evtMetaHandle, EventMetadataEventID, 0, sizeof(variant), &variant, &bufferUsed)) {
                wprintf(L"  Retrieving metadata property failed with 0x%08lX.\n",
                        GetLastError());
                continue;
            }

            unsigned eventId = variant.UInt32Val;

            if (!EvtGetEventMetadataProperty(
                evtMetaHandle, EventMetadataEventTemplate, 0, 0, nullptr, &bufferUsed)) {
                DWORD ec = GetLastError();
                if (ec != ERROR_INSUFFICIENT_BUFFER) {
                    wprintf(L"  Retrieving metadata property failed with 0x%08lX.\n", ec);
                    continue;
                }
            }

            auto stringVariant = make_vstruct<EVT_VARIANT>(bufferUsed);
            if (!EvtGetEventMetadataProperty(
                evtMetaHandle, EventMetadataEventTemplate, 0, bufferUsed, stringVariant.get(), &bufferUsed)) {
                DWORD ec = GetLastError();
                wprintf(L"  Retrieving metadata property failed with 0x%08lX.\n", ec);
                continue;
            }

            if (!EvtFormatMessage(pubMetaHandle, nullptr, messageId, 0,
                nullptr, EvtFormatMessageId, 0, nullptr, &bufferUsed)) {
                DWORD ec = GetLastError();
                if (ec != ERROR_INSUFFICIENT_BUFFER) {
                    wprintf(L"  EvtFormatMessage failed with with 0x%08lX.\n", ec);
                    continue;
                }
            }

            EVT_VARIANT insertions[1] = {};
            for (unsigned i = 0; i < countof(insertions); ++i)
                insertions[i].Type = EvtVarTypeString;
            insertions[0].StringVal = L"$1";

            buffer.resize(bufferUsed);
            if (!EvtFormatMessage(pubMetaHandle, nullptr, messageId,
                countof(insertions), insertions, EvtFormatMessageId,
                static_cast<DWORD>(buffer.size()), buffer.data(), &bufferUsed)) {
                DWORD ec = GetLastError();
                wprintf(L"    EvtFormatMessage failed with with 0x%08lX.\n", ec);
                continue;
            }

            wprintf(L"  [%5u] %ls\n", eventId, buffer.data());
            wprintf(L"    --->\n%ls    <---\n", stringVariant->StringVal);
            continue;
        }
    }
}
