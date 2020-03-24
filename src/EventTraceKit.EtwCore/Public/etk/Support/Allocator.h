#pragma once
#include <cassert>
#include <cstddef>
#include <cstdint>
#include <malloc.h>
#include <tuple>
#include <vector>

namespace etk
{

class Alignment
{
public:
    explicit constexpr Alignment(size_t alignment)
        : alignment((assert(alignment > 0), alignment))
    {}

    template<typename T>
    constexpr T Align(T value) const
    {
        return ((value + static_cast<T>(alignment - 1)) & ~static_cast<T>(alignment - 1));
    }

    template<typename T>
    constexpr T* Align(T* ptr) const
    {
        return reinterpret_cast<T*>(Align(reinterpret_cast<uintptr_t>(ptr)));
    }

    size_t PtrAdjustment(void const* ptr) const
    {
        auto addr = reinterpret_cast<uintptr_t>(ptr);
        return Align(addr) - addr;
    }

    size_t bytes() const { return alignment; }

private:
    size_t alignment;
};

template<typename T>
constexpr Alignment AlignmentOf()
{
    return Alignment(alignof(T));
}

template<typename Derived>
class AllocatorBase
{
public:
    template<typename T>
    T* Allocate(size_t count = 1)
    {
        return static_cast<T*>(Allocate(count * sizeof(T), AlignmentOf<T>()));
    }

    void* Allocate(size_t size, Alignment alignment)
    {
        return static_cast<Derived*>(this)->Allocate(size, alignment);
    }

    void Deallocate(size_t size, Alignment alignment)
    {
        return static_cast<Derived*>(this)->Deallocate(size, alignment);
    }
};

class MallocAllocator : public AllocatorBase<MallocAllocator>
{
public:
    using AllocatorBase<MallocAllocator>::Allocate;
    using AllocatorBase<MallocAllocator>::Deallocate;

    void* Allocate(size_t size, Alignment alignment)
    {
        return _aligned_malloc(size, alignment.bytes());
    }

    void Deallocate(void const* ptr, size_t /*size*/)
    {
        _aligned_free(const_cast<void*>(ptr));
    }
};

template<typename Allocator, size_t SlabSize = 4096>
class BumpPtrAllocator : public AllocatorBase<BumpPtrAllocator<Allocator, SlabSize>>
{
public:
    BumpPtrAllocator() = default;

    BumpPtrAllocator(Allocator&& allocator)
        : allocator(std::move(allocator))
    {}

    ~BumpPtrAllocator()
    {
        DeallocateSlabs();
        DeallocateCustomSizedSlabs();
    }

    using AllocatorBase<BumpPtrAllocator>::Allocate;
    using AllocatorBase<BumpPtrAllocator>::Deallocate;

    void* Allocate(size_t size, Alignment alignment)
    {
        size_t const remaining = static_cast<size_t>(slabEnd - slabCurr);

        size_t const adjustment = alignment.PtrAdjustment(slabCurr);
        if (adjustment + size < remaining) {
            std::byte* alignedPtr = slabCurr + adjustment;
            slabCurr = alignedPtr + size;
            return alignedPtr;
        }

        if (adjustment + size > SlabSize) {
            void* newSlab = AllocateCustomSizedSlab(adjustment + size);
            return alignment.Align(static_cast<std::byte*>(newSlab));
        }

        AllocateSlab();
        std::byte* alignedPtr = alignment.Align(static_cast<std::byte*>(slabCurr));
        slabCurr = alignedPtr + size;
        return alignedPtr;
    }

    void Deallocate(void const* ptr, size_t size)
    {
        if (!ptr)
            return;

        if (slabCurr == ptr && slabCurr - size >= slabEnd - SlabSize)
            slabCurr -= size;
    }

    void Reset()
    {
        DeallocateCustomSizedSlabs();
        customSizedSlabs.clear();
        customSizedSlabs.shrink_to_fit();

        if (slabs.empty())
            return;

        slabCurr = static_cast<std::byte*>(slabs.front());
        slabEnd = slabCurr + SlabSize;

        DeallocateSlabs(std::next(slabs.begin()), slabs.end());
        slabs.erase(std::next(slabs.begin()), slabs.end());
    }

private:
    void AllocateSlab()
    {
        size_t slabSize = SlabSize;
        void* newSlab = allocator.Allocate(slabSize, Alignment(1));
        slabs.push_back(newSlab);
        slabCurr = static_cast<std::byte*>(newSlab);
        slabEnd = slabCurr + slabSize;
    }

    void DeallocateSlabs()
    {
        for (void* slab : slabs)
            allocator.Deallocate(slab, SlabSize);
    }

    void DeallocateSlabs(std::vector<void*>::iterator it,
                         std::vector<void*>::iterator end)
    {
        for (; it != end; ++it)
            allocator.Deallocate(*it, SlabSize);
    }

    void* AllocateCustomSizedSlab(size_t size)
    {
        void* newSlab = allocator.Allocate(size, Alignment(1));
        customSizedSlabs.emplace_back(newSlab, size);
        return newSlab;
    }

    void DeallocateCustomSizedSlabs()
    {
        for (auto&& slab : customSizedSlabs) {
            void* ptr;
            size_t size;
            std::tie(ptr, size) = slab;
            allocator.Deallocate(ptr, size);
        }
    }

    std::vector<void*> slabs;
    std::vector<std::tuple<void*, size_t>> customSizedSlabs;
    std::byte* slabCurr = nullptr;
    std::byte* slabEnd = nullptr;
    Allocator allocator;
};

} // namespace etk
