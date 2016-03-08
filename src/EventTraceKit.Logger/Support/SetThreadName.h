#pragma once
typedef unsigned long DWORD;

namespace etk
{

void SetThreadName(DWORD dwThreadID, char const* threadName);
void SetCurrentThreadName(char const* threadName);

} // namespace etk
