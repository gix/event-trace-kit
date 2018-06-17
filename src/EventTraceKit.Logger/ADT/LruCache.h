#pragma once
#include <list>
#include <unordered_map>

namespace etk
{

template<typename K, typename T, typename Hasher = std::hash<K>>
class LruCache
{
public:
    using key_type = K;
    using mapped_type = T;
    using size_type = size_t;

    LruCache(size_t capacity)
        : capacity(capacity)
    {
        valueMap.reserve(capacity);
    }

    size_t Capacity() const { return capacity; }
    size_t Size() const { return valueMap.size(); }

    template<typename ValueFactory>
    T& operator()(K const& key, ValueFactory factory)
    {
        return GetOrCreate(key, factory);
    }

    template<typename ValueFactory>
    T& GetOrCreate(K const& key, ValueFactory factory)
    {
        auto it = valueMap.find(key);
        if (it != valueMap.end()) {
            // Move accessed key to the end of the LRU list.
            lruKeys.splice(lruKeys.end(), lruKeys, it->second.second);
            return it->second.first;
        }

        return Add(key, factory(key));
    }

    void SetCapacity(size_t capacity)
    {
        for (size_t i = this->capacity; i > capacity; --i)
            Evict();

        this->capacity = capacity;
    }

    void Remove(K const& key)
    {
        auto it = valueMap.find(key);
        if (it != valueMap.end()) {
            lruKeys.erase(it->second.second);
            valueMap.erase(key);
        }
    }

private:
    using KeyAccessList = std::list<key_type>;
    using MapEntry = std::pair<mapped_type, typename KeyAccessList::iterator>;
    using ValueMap = std::unordered_map<key_type, MapEntry, Hasher>;

    mapped_type& Add(key_type const& key, mapped_type&& value)
    {
        if (lruKeys.size() == capacity)
            Evict();

        auto lruIt = lruKeys.insert(lruKeys.end(), key);
        auto result = valueMap.insert({key, MapEntry(std::move(value), lruIt)});
        return result.first->second.first;
    }

    void Evict()
    {
        valueMap.erase(lruKeys.front());
        lruKeys.pop_front();
    }

    KeyAccessList lruKeys;
    ValueMap valueMap;
    size_t capacity;
};

} // namespace etk
