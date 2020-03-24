#include "TraceDataContext.h"

#include "BinaryFind.h"
#include "etk/Support/ErrorHandling.h"

#include <tdh.h>

namespace etk
{

static HRESULT LoadManifest(std::wstring const& manifest)
{
    return HResultFromWin32(TdhLoadManifest(const_cast<wchar_t*>(manifest.c_str())));
}

static HRESULT UnloadManifest(std::wstring const& manifest)
{
    return HResultFromWin32(TdhUnloadManifest(const_cast<wchar_t*>(manifest.c_str())));
}

std::mutex TraceDataContext::globalContextLock;
std::weak_ptr<TraceDataContext> TraceDataContext::globalContext;

TraceDataContext::~TraceDataContext() noexcept
{
    for (auto const& entry : loadedManifests)
        (void)TdhUnloadManifest(const_cast<wchar_t*>(entry.ManifestPath.c_str()));
}

HRESULT TraceDataContext::AddRefManifest(std::wstring const& manifestPath) noexcept
{
    ExclusiveLock lock(mutex);

    auto it = binary_find(loadedManifests.begin(), loadedManifests.end(), manifestPath);
    if (it != loadedManifests.end()) {
        ++it->RefCount;
        return S_OK;
    }

    HR(LoadManifest(manifestPath));

    loadedManifests.insert(it, {manifestPath, 1});
    return S_OK;
}

HRESULT TraceDataContext::ReleaseManifest(std::wstring const& manifestPath) noexcept
{
    ExclusiveLock lock(mutex);

    auto it = binary_find(loadedManifests.begin(), loadedManifests.end(), manifestPath);
    if (it == loadedManifests.end() || --it->RefCount > 0)
        return S_OK;

    loadedManifests.erase(it);

    HR(UnloadManifest(manifestPath));
    return S_OK;
}

} // namespace etk
