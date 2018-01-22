#include <cstdio>
#include <string>
#include <string_view>

#include <io.h>
#include <objbase.h>
#include <psapi.h>
#include <windows.h>

using namespace std::literals;

static bool Consume(wchar_t const*& p, wchar_t chr)
{
    if (*p != chr)
        return false;

    ++p;
    return true;
}

static bool ConsumeAppPath(wchar_t const*& p, std::wstring_view* appPath)
{
    auto const first = p;

    // Opening "
    if (!Consume(p, L'"'))
        return false;

    while (*p && *p != L'"')
        ++p;

    // Closing "
    if (!Consume(p, L'"'))
        return false;

    if (appPath)
        *appPath = {first + 1, static_cast<size_t>(p - first - 2)};

    return true;
}

static bool ConsumeNonSpace(wchar_t const*& p, std::wstring_view* str)
{
    auto const first = p;

    while (*p && *p != L' ')
        ++p;

    if (!*p || p == first)
        return false;

    if (str)
        *str = {first, static_cast<size_t>(p - first)};

    return true;
}

static bool SplitCommandLine(std::wstring& args, std::wstring& appPath,
                             std::wstring& pipeName)
{
    wchar_t const* p = args.data();

    // Skip program path
    if (!ConsumeAppPath(p, nullptr))
        return false;
    if (!Consume(p, L' '))
        return false;

    std::wstring_view pipe;
    if (!ConsumeNonSpace(p, &pipe))
        return false;

    if (!Consume(p, L' '))
        return false;

    std::wstring_view path;
    if (!ConsumeAppPath(p, &path))
        return false;

    if (!Consume(p, L' '))
        return false;

    appPath = static_cast<std::wstring>(path);
    pipeName = L"\\\\.\\pipe\\"s;
    pipeName += pipe;

    args.erase(0, p - args.data());
    return true;
}

#ifdef _DEBUG
static FILE* OpenLog()
{
    HANDLE hlogFile = CreateFileW(L"d:\\TraceLaunch.log", GENERIC_WRITE,
                                  FILE_SHARE_WRITE | FILE_SHARE_READ, nullptr,
                                  CREATE_ALWAYS, 0, nullptr);
    return _fdopen(_open_osfhandle(reinterpret_cast<intptr_t>(hlogFile), 0), "wb");
}

static void Log(char const* format, ...)
{
    static FILE* logFile = OpenLog();

    va_list args;
    va_start(args, format);
    vfprintf(logFile, format, args);
    va_end(args);
    fflush(logFile);
}
#else
static void Log(char const* format, ...)
{
}
#endif

static void LogCreateProcessFailed(DWORD ec)
{
#ifdef _DEBUG
    Log("[TraceLaunch] CreateProcessW failed: ec=%lu\n", ec);
#endif
}

static void LogPipeFailure(DWORD ec)
{
#ifdef _DEBUG
    Log("[TraceLaunch] CallNamedPipeW failed: ec=%lu\n", ec);
#endif
}

static bool StartsWith(std::wstring_view str, std::wstring_view head)
{
    if (head.length() > str.length())
        return false;

    return str.compare(0, head.length(), head) == 0;
}

static bool EndsWith(std::wstring_view str, std::wstring_view tail)
{
    if (tail.length() > str.length())
        return false;

    return str.compare(str.length() - tail.length(), tail.length(), tail) == 0;
}

static bool IsCmdWrapper(std::wstring_view appPath, std::wstring_view args)
{
    return EndsWith(appPath, L"\\cmd.exe"sv) && StartsWith(args, L"/c \"\""sv) &&
           EndsWith(args, L" & pause\""sv);
}

static bool IsConHost(wchar_t const* imageName)
{
    return _wcsicmp(imageName, L"C:\\Windows\\System32\\conhost.exe") == 0;
}

static bool DispatchPid(std::wstring const& pipeName, DWORD childPid)
{
    BYTE response = 0;
    DWORD bytesRead = 0;
    if (!CallNamedPipeW(pipeName.c_str(), &childPid, sizeof(childPid), &response,
                        sizeof(response), &bytesRead, NMPWAIT_WAIT_FOREVER)) {
        LogPipeFailure(GetLastError());
        return false;
    }

    return true;
}

int CALLBACK wWinMain(_In_ HINSTANCE /*hInstance*/, _In_ HINSTANCE /*hPrevInstance*/,
                      _In_ PWSTR /*lpCmdLine*/, _In_ int /*nCmdShow*/)
{
    bool const waitForProcess = false;

    std::wstring args(GetCommandLineW());
    std::wstring appPath;
    std::wstring pipeName;
    if (!SplitCommandLine(args, appPath, pipeName))
        return -1;

    bool const usesCmdWrapper = IsCmdWrapper(appPath, args);

    STARTUPINFOW startupInfo = {};
    startupInfo.cb = sizeof(startupInfo);

    PROCESS_INFORMATION processInfo = {};

    // Create a suspended process because we want to report the process id and
    // start tracing before the process starts running.
    DWORD creationFlags = CREATE_SUSPENDED;

    // If a cmd.exe wrapper is used we need the id of the spawned child process.
    // We attach ourselves as a debugger and listen to process creation events.
    if (usesCmdWrapper)
        creationFlags |= DEBUG_PROCESS;

    if (!CreateProcessW(appPath.data(), args.data(), nullptr, nullptr, FALSE,
                        creationFlags, nullptr, nullptr, &startupInfo, &processInfo)) {
        LogCreateProcessFailed(GetLastError());
        return -2;
    }

    DebugSetProcessKillOnExit(FALSE);

    int exitCode = 0;
    if (usesCmdWrapper) {
        // Resume right away so we can listen to debug events.
        ResumeThread(processInfo.hThread);

        bool consumeEvents = true;
        wchar_t imageName[MAX_PATH] = {};
        DEBUG_EVENT debugEvent;

        while (consumeEvents && WaitForDebugEvent(&debugEvent, INFINITE)) {
            if (debugEvent.dwDebugEventCode == CREATE_PROCESS_DEBUG_EVENT) {
                DWORD const pid = GetProcessId(debugEvent.u.CreateProcessInfo.hProcess);

                if (!K32GetModuleFileNameExW(debugEvent.u.CreateProcessInfo.hProcess,
                                             nullptr, imageName, MAX_PATH))
                    imageName[0] = 0;

                // Ignore the first pid (it will be the cmd.exe process). Also
                // ignore any conhost.exe process that might spawn before the
                // real child process.
                if (pid != processInfo.dwProcessId && !IsConHost(imageName)) {
                    if (!DispatchPid(pipeName, pid))
                        exitCode = -3;
                    consumeEvents = false;
                }
            } else if (debugEvent.dwDebugEventCode == EXIT_PROCESS_DEBUG_EVENT) {
                // cmd.exe failed to create a child process, abort.
                exitCode = -4;
                consumeEvents = false;
            }

            ContinueDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId,
                               DBG_CONTINUE);
        }
    } else {
        // Without a wrapper we already have the actual process id.
        if (!DispatchPid(pipeName, processInfo.dwProcessId))
            exitCode = -3;

        ResumeThread(processInfo.hThread);
    }

    DebugActiveProcessStop(processInfo.dwProcessId);

    if (waitForProcess)
        WaitForSingleObject(processInfo.hProcess, INFINITE);

    CloseHandle(processInfo.hThread);
    CloseHandle(processInfo.hProcess);

    return exitCode;
}
