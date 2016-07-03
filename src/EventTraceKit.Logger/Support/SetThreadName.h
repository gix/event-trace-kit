#pragma once
using DWORD = unsigned long;

namespace etk
{

void SetThreadName(DWORD dwThreadID, char const* threadName);
void SetCurrentThreadName(char const* threadName);

} // namespace etk
