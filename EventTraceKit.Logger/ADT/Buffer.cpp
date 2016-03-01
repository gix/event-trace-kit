#include "Buffer.h"

#include "ffmf/Common/Diagnostics/Logging.h"
#include "ffmf/Common/fill_zero.h"

namespace ffmf
{

Buffer::Buffer()
    : m_begin(0)
    , m_end(0)
{
}

HRESULT Buffer::Initalize(size_t cbSize)
{
    HRESULT hr = resize(cbSize);
    if (SUCCEEDED(hr))
        fill_zero(ptr(), cbSize);
    return hr;
}

HRESULT Buffer::reserve(size_t n)
{
    if (n > MAXDWORD - size())
        return FFMF_TRACE_HR(E_INVALIDARG); // Overflow

    HRESULT hr = S_OK;

    // If this would push the end position past the end of the array,
    // then we need to copy up the data to start of the array. We might
    // also need to realloc the array.
    if (n > GetCount() - m_end) {
        // New end position would be past the end of the array.
        // Check if we need to grow the array.

        if (n > CurrentFreeSize()) {
            // Array needs to grow
            CHECK_HR(hr = resize(size() + n));
        }

        memmove(ptr(), data(), size());

        // Reset begin and end.
        m_end = size(); // Update m_end first before resetting m_begin!
        m_begin = 0;
    }

    FFMF_ASSERT(CurrentFreeSize() >= n);

done:
    return hr;
}

HRESULT Buffer::consume(size_t bytes)
{
    // Cannot advance pass the end of the buffer.
    if (bytes > size())
        return FFMF_TRACE_HR(E_INVALIDARG);

    m_begin += bytes;
    return S_OK;
}

HRESULT Buffer::produce(size_t bytes)
{
    HRESULT hr = reserve(bytes);
    if (SUCCEEDED(hr))
        m_end += bytes;
    return hr;
}

size_t Buffer::CurrentFreeSize() const
{
    FFMF_ASSERT(GetCount() >= size());
    return GetCount() - size();
}

} // namespace ffmf
