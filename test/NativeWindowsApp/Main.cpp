#define WIN32_LEAN_AND_MEAN
#include <Windows.h>

int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, PWSTR pCmdLine,
                    int nCmdShow)
{
    MessageBoxW(nullptr, L"", L"NativeWindowsApp", MB_OK);
    return 0;
}
