#pragma once
#include <cstdint>
#include <guiddef.h>
#include <utility>

namespace etk
{

template<typename T>
void hash_combine(std::size_t& seed, T const& value)
{
    seed ^= std::hash<T>()(value) + 0x9E3779B9 + (seed << 6) + (seed >> 2);
}

} // namespace etk

namespace std
{

template<>
struct hash<GUID>
{
    size_t operator()(GUID const& guid) const
    {
        static_assert(sizeof(GUID) == 2 * sizeof(uint64_t), "Invariant violated");

        size_t value = 0;
        ::etk::hash_combine(value, reinterpret_cast<uint64_t const*>(&guid)[0]);
        ::etk::hash_combine(value, reinterpret_cast<uint64_t const*>(&guid)[1]);
        return value;
    }
};

} // namespace std
