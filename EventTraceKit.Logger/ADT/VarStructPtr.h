#pragma once
#include <algorithm>
#include <memory>

namespace etk
{

template<typename T>
struct VarStructDeleter
{
    void operator ()(T* p)
    {
        p->~T();
        operator delete(reinterpret_cast<void*>(p));
    }
};

template<typename T>
using vstruct_ptr = std::unique_ptr<T, VarStructDeleter<T>>;

template<typename T>
inline vstruct_ptr<T> make_vstruct(size_t count)
{
    void* buffer = operator new(std::max(sizeof(T), count));
    return vstruct_ptr<T>(new(buffer) T());
}

template<typename T>
inline vstruct_ptr<T> make_vstruct_nothrow(size_t count)
{
    void* buffer = operator new(std::max(sizeof(T), count), std::nothrow);
    return vstruct_ptr<T>(new(buffer) T());
}

} // namespace etk
