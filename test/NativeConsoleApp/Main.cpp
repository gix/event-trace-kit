#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <cstdio>

int main(int argc, char** argv)
{
    for (int i = 0; i < 5; ++i) {
        printf("NativeConsoleApp %d\n", i);
        Sleep(1000);
    }
    return 0;
}
