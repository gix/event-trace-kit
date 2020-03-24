#pragma once
#include "etk/ADT/Handle.h"

#include <chrono>
#include <threadpoolapiset.h>

namespace etk
{

struct ThreadpoolTimerTraits : NullIsInvalidHandleTraits<PTP_TIMER>
{
    static void Close(HandleType h) noexcept { CloseThreadpoolTimer(h); }
};

class ThreadpoolTimer : public Handle<ThreadpoolTimerTraits>
{
public:
    using Handle<ThreadpoolTimerTraits>::Handle;
    ThreadpoolTimer() = default;

    ThreadpoolTimer(PTP_TIMER_CALLBACK callback, void* context,
                    PTP_CALLBACK_ENVIRON cbe = nullptr)
        : ThreadpoolTimer(CreateThreadpoolTimer(callback, context, cbe))
    {}

    void Start(std::chrono::duration<unsigned, std::milli> period)
    {
        if (!IsValid())
            return;

        FILETIME dueTime = {};
        SetThreadpoolTimer(Get(), &dueTime, period.count(), 0);
    }

    void Stop()
    {
        if (!IsValid())
            return;

        SetThreadpoolTimer(Get(), nullptr, 0, 0);
    }
};

} // namespace etk
