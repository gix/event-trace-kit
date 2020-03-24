#pragma once

namespace etk
{

void SetThreadDescription(void* thread, wchar_t const* threadName);
void SetCurrentThreadDescription(wchar_t const* threadName);

} // namespace etk
