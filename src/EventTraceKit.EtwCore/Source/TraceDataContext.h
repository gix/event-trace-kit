#pragma once
#include "etk/ADT/Span.h"
#include "etk/Support/ErrorHandling.h"

#include <algorithm>
#include <iterator>
#include <mutex>
#include <string>
#include <unordered_set>
#include <vector>

#include <windows.h>

namespace etk
{

class TraceDataContext
{
public:
    ~TraceDataContext() noexcept;

    HRESULT AddRefManifest(std::wstring const& manifestPath) noexcept;
    HRESULT ReleaseManifest(std::wstring const& manifestPath) noexcept;

    static std::shared_ptr<TraceDataContext> GlobalContext()
    {
        std::unique_lock<std::mutex> lock(globalContextLock);
        auto context = globalContext.lock();
        if (context)
            return context;

        context = std::make_shared<TraceDataContext>();
        globalContext = context;
        return context;
    }

private:
    using ExclusiveLock = std::unique_lock<std::mutex>;
    std::mutex mutex;

    struct Entry
    {
        std::wstring ManifestPath;
        unsigned RefCount;

        friend bool operator<(Entry const& lhs, std::wstring const& rhs)
        {
            return lhs.ManifestPath < rhs;
        }

        friend bool operator<(std::wstring const& lhs, Entry const& rhs)
        {
            return lhs < rhs.ManifestPath;
        }
    };

    std::vector<Entry> loadedManifests;

    static std::mutex globalContextLock;
    static std::weak_ptr<TraceDataContext> globalContext;
};

class TraceDataToken
{
public:
    TraceDataToken() = default;

    explicit TraceDataToken(std::shared_ptr<TraceDataContext> context)
        : context(std::move(context))
    {}

    ~TraceDataToken() { Invalidate(); }

    TraceDataToken(TraceDataToken&&) = default;
    TraceDataToken& operator=(TraceDataToken&&) = default;

    static HRESULT Create(std::shared_ptr<TraceDataContext> context,
                          cspan<std::wstring> eventManifests, TraceDataToken& token)
    {
        HRESULT hr = S_OK;
        for (auto const& manifest : eventManifests) {
            hr = context->AddRefManifest(manifest);
            if (FAILED(hr))
                break;
        }

        if (FAILED(hr)) {
            for (auto const& manifest : eventManifests)
                (void)context->ReleaseManifest(manifest);
            return hr;
        }

        token = TraceDataToken(std::move(context), eventManifests);
        return S_OK;
    }

    void Invalidate()
    {
        if (!context)
            return;

        for (auto const& manifest : eventManifests)
            (void)context->ReleaseManifest(manifest);
        eventManifests.clear();
    }

    HRESULT Update(cspan<std::wstring> newEventManifests)
    {
        if (!context)
            return E_FAIL;

        TraceDataToken token;
        HR(Create(context, newEventManifests, token));

        std::copy(token.eventManifests.begin(), token.eventManifests.end(),
                  std::inserter(eventManifests, eventManifests.begin()));
        token.eventManifests.clear();

        return S_OK;
    }

private:
    TraceDataToken(std::shared_ptr<TraceDataContext> context,
                   cspan<std::wstring> eventManifests)
        : context(std::move(context))
        , eventManifests(eventManifests.begin(), eventManifests.end())
    {}

    std::shared_ptr<TraceDataContext> context;
    std::unordered_set<std::wstring> eventManifests;
};

} // namespace etk
