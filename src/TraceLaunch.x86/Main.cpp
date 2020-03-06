#include <cstdio>
#include <string>
#include <string_view>

#include <fcntl.h>
#include <io.h>
#include <objbase.h>
#include <psapi.h>
#include <windows.h>

using namespace std::literals;

namespace
{

bool Consume(wchar_t const*& p, wchar_t chr)
{
    if (*p != chr)
        return false;

    ++p;
    return true;
}

bool ConsumeAppPath(wchar_t const*& p, std::wstring_view* appPath)
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

bool ConsumeNonSpace(wchar_t const*& p, std::wstring_view* str)
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

bool SplitCommandLine(std::wstring& args, std::wstring& appPath, std::wstring& pipeName)
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
FILE* OpenLog()
{
    HANDLE hlogFile =
        CreateFileW(L"TraceLaunch.log", GENERIC_WRITE, FILE_SHARE_WRITE | FILE_SHARE_READ,
                    nullptr, CREATE_ALWAYS, 0, nullptr);
    return _fdopen(_open_osfhandle(reinterpret_cast<intptr_t>(hlogFile), 0), "wb");
}

void Log(char const* format, ...)
{
    static FILE* logFile = OpenLog();

    va_list args;
    va_start(args, format);
    vfprintf(logFile, format, args);
    va_end(args);
    fflush(logFile);
}
#else
void Log(char const* /*format*/, ...)
{
}
#endif

void LogCreateProcessFailed(DWORD ec)
{
    Log("[TraceLaunch] CreateProcessW failed: ec=%lu\n", ec);
}

void LogPipeFailure(DWORD ec)
{
    Log("[TraceLaunch] CallNamedPipeW failed: ec=%lu\n", ec);
}

bool StartsWith(std::wstring_view str, std::wstring_view head)
{
    if (head.length() > str.length())
        return false;

    return str.compare(0, head.length(), head) == 0;
}

bool EndsWith(std::wstring_view str, std::wstring_view tail)
{
    if (tail.length() > str.length())
        return false;

    return str.compare(str.length() - tail.length(), tail.length(), tail) == 0;
}

bool IsCmdWrapper(std::wstring_view appPath, std::wstring_view args)
{
    return EndsWith(appPath, L"\\cmd.exe"sv) && StartsWith(args, L"/c \"\""sv) &&
           EndsWith(args, L" & pause\""sv);
}

bool IsConHost(wchar_t const* imageName, wchar_t const* conhostPath)
{
    return _wcsicmp(imageName, conhostPath) == 0;
}

bool DispatchPid(std::wstring const& pipeName, DWORD childPid)
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

std::wstring GetConhostPath()
{
    constexpr auto const fileName = L"\\conhost.exe"sv;

    std::wstring path;
    path.resize(MAX_PATH + fileName.length());

    UINT actualLength = GetSystemDirectoryW(&path[0], static_cast<UINT>(path.length()));
    if (actualLength > path.length()) {
        path.resize(actualLength);
        actualLength = GetSystemDirectoryW(&path[0], static_cast<UINT>(path.length()));
    }

    if (actualLength == 0)
        return L"C:\\Windows\\System32\\conhost.exe"s;

    path.resize(actualLength);
    path += fileName;
    return path;
}

enum class ExitCode : int
{
    Success = 0,
    InvalidArgs = -1,
    ErrorCreateProcess = -2,
    ErrorDispatchPid = -3,
    ErrorChildExited = -4,
};

struct ProcessInformation : PROCESS_INFORMATION
{
    ~ProcessInformation()
    {
        CloseHandle(hThread);
        CloseHandle(hProcess);
    }
};

void DebugCmdLaunch(PROCESS_INFORMATION const& processInfo, std::wstring const& pipeName,
                    ExitCode& exitCode)
{
    DebugSetProcessKillOnExit(FALSE);

    // Resume right away so we can listen to debug events.
    ResumeThread(processInfo.hThread);

    auto const conhostPath = GetConhostPath();

    bool consumeEvents = true;
    DEBUG_EVENT debugEvent;
    wchar_t imageName[MAX_PATH] = {};

    while (consumeEvents && WaitForDebugEvent(&debugEvent, INFINITE)) {
        if (debugEvent.dwDebugEventCode == CREATE_PROCESS_DEBUG_EVENT) {
            DWORD const pid = GetProcessId(debugEvent.u.CreateProcessInfo.hProcess);

            if (!K32GetModuleFileNameExW(debugEvent.u.CreateProcessInfo.hProcess, nullptr,
                                         imageName, MAX_PATH))
                imageName[0] = 0;

            // Ignore the first pid (it will be the cmd.exe process). Also
            // ignore any conhost.exe process that might spawn before the
            // real child process.
            if (pid != processInfo.dwProcessId &&
                !IsConHost(imageName, conhostPath.c_str())) {
                if (!DispatchPid(pipeName, pid))
                    exitCode = ExitCode::ErrorDispatchPid;

                consumeEvents = false;
                if (!DebugActiveProcessStop(pid)) {
                    DWORD const ec = GetLastError();
                    Log("[TraceLaunch] Failed to detach from '%ls' (process %ld): ec=%lu\n",
                        imageName, pid, ec);
                }
            }
        } else if (debugEvent.dwDebugEventCode == EXIT_PROCESS_DEBUG_EVENT) {
            // cmd.exe failed to create a child process, abort.
            exitCode = ExitCode::ErrorChildExited;
            consumeEvents = false;
        }

        ContinueDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, DBG_CONTINUE);
    }

    if (!DebugActiveProcessStop(processInfo.dwProcessId)) {
        DWORD const ec = GetLastError();
        Log("[TraceLaunch] Failed to detach from '%ls' (process %ld): ec=%lu\n",
            imageName, processInfo.dwProcessId, ec);
    }
}

ExitCode CommonMain(bool const waitForProcess)
{
    std::wstring args(GetCommandLineW());
    std::wstring appPath;
    std::wstring pipeName;
    if (!SplitCommandLine(args, appPath, pipeName))
        return ExitCode::InvalidArgs;

    // Overwrite the console title. When Visual Studio uses VsDebugConsole.exe
    // to launch a console application, the console title shows our TraceLaunch
    // instead of the actual application.
    SetConsoleTitleW(appPath.c_str());

    // Create a suspended process because we want to report the process id and
    // start tracing before the process starts running.
    DWORD creationFlags = CREATE_SUSPENDED;

    // If a cmd.exe wrapper is used we need the id of the spawned child process.
    // We attach ourselves as a debugger and listen to process creation events.
    bool const usesCmdWrapper = IsCmdWrapper(appPath, args);
    if (usesCmdWrapper)
        creationFlags |= DEBUG_PROCESS;

    STARTUPINFOW startupInfo = {};
    startupInfo.cb = sizeof(startupInfo);

    ProcessInformation processInfo = {};
    if (!CreateProcessW(appPath.data(), args.data(), nullptr, nullptr, FALSE,
                        creationFlags, nullptr, nullptr, &startupInfo, &processInfo)) {
        LogCreateProcessFailed(GetLastError());
        return ExitCode::ErrorCreateProcess;
    }

    ExitCode exitCode = ExitCode::Success;
    if (!usesCmdWrapper) {
        // Without a wrapper we already have the real process id.
        if (!DispatchPid(pipeName, processInfo.dwProcessId))
            exitCode = ExitCode::ErrorDispatchPid;

        ResumeThread(processInfo.hThread);
    } else {
        // Debug the cmd.exe wrapper until it spawns the real child process.
        DebugCmdLaunch(processInfo, pipeName, exitCode);
    }

    if (!waitForProcess)
        return exitCode;

    WaitForSingleObject(processInfo.hProcess, INFINITE);

    DWORD processExitCode;
    if (!GetExitCodeProcess(processInfo.hProcess, &processExitCode))
        processExitCode = 0;

    // VsDebugConsole.exe prints a message when the launched console
    // application exits. Since this will refer to TraceLaunch add an
    // additional message before that with info about the real process.
    _setmode(_fileno(stdout), _O_U16TEXT);
    putwchar('\n');
    wprintf(L"%ls (process %ld) exited with code %lu.", appPath.data(),
            processInfo.dwProcessId, processExitCode);
    FlushConsoleInputBuffer(GetStdHandle(STD_INPUT_HANDLE));

    return exitCode;
}

} // namespace

int main(int /*argc*/, char** /*argv*/)
{
    return static_cast<int>(CommonMain(true));
}

int CALLBACK wWinMain(_In_ HINSTANCE /*hInstance*/, _In_ HINSTANCE /*hPrevInstance*/,
                      _In_ PWSTR /*lpCmdLine*/, _In_ int /*nCmdShow*/)
{
    return static_cast<int>(CommonMain(false));
}
