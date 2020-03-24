#pragma once
#include <condition_variable>

namespace etk
{

class ManualResetEventSlim
{
public:
    explicit ManualResetEventSlim(bool signaled = false)
        : signaled(signaled)
    {
    }

    void Set()
    {
        {
            ExclusiveLock lock(mutex);
            signaled = true;
        }

        cv.notify_all();
    }

    void Reset()
    {
        ExclusiveLock lock(mutex);
        signaled = false;
    }

    void Wait()
    {
        ExclusiveLock lock(mutex);
        while (!signaled)
            cv.wait(lock);
    }

private:
    using ExclusiveLock = std::unique_lock<std::mutex>;
    std::mutex mutex;
    std::condition_variable cv;
    bool signaled;
};

} // namespace etk
