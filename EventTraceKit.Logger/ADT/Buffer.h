#pragma once
#include "ffmf/Common/Debug.h"
#include "ffmf/Common/Windows.h"
#include "ffmf/Common/Support/MakeUniqueNothrow.h"
#include "ffmf/Common/Support/Types.h"

#include <memory>
#include <new>

namespace ffmf
{

template<typename T>
class GrowableArray
{
public:
    GrowableArray()
        : count(0)
        , allocated(0)
    {
    }

    GrowableArray& operator=(GrowableArray const& source) = delete;
    GrowableArray(GrowableArray const& source) = delete;

    // Allocate: Reserves memory for the array, but does not increase the count.
    HRESULT Allocate(size_t alloc)
    {
        HRESULT hr = S_OK;
        if (alloc > allocated) {
            auto tmp = make_unique_nothrow<T[]>(alloc);
            if (tmp) {
                FFMF_ASSERT(count <= allocated);

                // Copy the elements to the re-allocated array.
                for (size_t i = 0; i < count; ++i)
                    tmp[i] = buffer[i];

                buffer = std::move(tmp);
                allocated = alloc;
            } else {
                hr = E_OUTOFMEMORY;
            }
        }
        return hr;
    }

    //! Changes the count, and grows the array if needed.
    HRESULT resize(size_t count)
    {
        FFMF_ASSERT(count <= allocated);

        HRESULT hr = S_OK;
        if (count > allocated)
            hr = Allocate(count);
        if (SUCCEEDED(hr))
            this->count = count;

        return hr;
    }

    size_t GetCount() const { return count; }

    T& operator [](size_t index)
    {
        FFMF_ASSERT(index < count);
        return buffer[index];
    }

    T const& operator [](size_t index) const
    {
        FFMF_ASSERT(index < count);
        return buffer[index];
    }

    //! Return the underlying array.
    T* ptr() { return buffer.get(); }
    T const* ptr() const { return buffer.get(); }

protected:
    std::unique_ptr<T[]> buffer;
    size_t count;        // Nominal count.
    size_t allocated;    // Actual allocation size.
};

class Buffer : GrowableArray<uint8_t>
{
public:
    Buffer();
    HRESULT Initalize(size_t cbSize);

    //! Returns a pointer to the start of the buffer.
    uint8_t* data() { return ptr() + m_begin; }
    uint8_t const* data() const { return ptr() + m_begin; }

    size_t size() const
    {
        FFMF_ASSERT(m_end >= m_begin);
        return m_end - m_begin;
    }

    //! Reserves additional bytes of memory for the buffer.
    /// The reserved bytes start at data() + size().
    HRESULT reserve(size_t n);

    //! Moves the front of the buffer.
    /// Call this method after consuming data from the buffer.
    HRESULT consume(size_t bytes);

    //! Moves the end of the buffer.
    /// Call this method after reading data into the buffer.
    HRESULT produce(size_t bytes);

    void reset()
    {
        m_begin = 0;
        m_end = 0;
    }

private:
    //! Returns the size of the array minus the size of the data.
    size_t CurrentFreeSize() const;

private:
    DWORD m_begin;
    DWORD m_end; // 1 past the last element
};

} // namespace ffmf
