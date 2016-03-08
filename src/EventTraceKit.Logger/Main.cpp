#include "EtwTraceSession.h"

#include "ADT/Guid.h"
#include <memory>

#include <Tdh.h>
#include <windows.h>
#include <winevt.h>

using namespace etk;

EVT_HANDLE SubscribeEvents(wchar_t const* path, wchar_t const* query = nullptr);

namespace etk
{

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
    TraceProviderSpec provider(ProviderId, 0xFF, 0xFFFFFFFFFFFFFFFFULL, 0);

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
    TraceProperties properties(Guid::ToGUID(Guid::Create()));
    std::wstring loggerName = L"RTLogSession0";

    auto session = std::make_unique<EtwTraceSession>(loggerName, properties);
    //auto sculptorCookie = AddSculptorProvider(*session);
    auto mfPerfCookie = AddMFPerformanceProvider(*session);
    session->Start();
}

} // namespace etk
