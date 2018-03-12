#pragma once
#include <windows.h>

// RtlGetVersion does not require a manifest to detect Windows 10+.
typedef _Return_type_success_(return >= 0) LONG NTSTATUS;
extern "C" NTSTATUS WINAPI RtlGetVersion(_Inout_ LPOSVERSIONINFOEXW lpVersionInformation);
