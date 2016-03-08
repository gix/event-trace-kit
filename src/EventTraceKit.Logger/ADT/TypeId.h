#pragma once
#include "Guid.h"

namespace ffmf
{

typedef TaggedGuid<struct TypeIdTag> TypeId;

namespace Details
{
    template<typename T>
    struct typeidofImpl
    {
        inline static TypeId const& GetTypeId()
        {
            return T::TId();
        }
    };
}

template<typename T>
TypeId const& typeidof()
{
    return Details::typeidofImpl<T>::GetTypeId();
}

#define FFMF_BEFRIEND_TYPEIDOF() \
template<typename T> \
friend struct ::ffmf::Details::typeidofImpl;

#define FFMF_DECLARE_TYPE_ID(TypeIdHi, TypeIdLo)                                      \
private:                                                                              \
    static TypeId const& TId()                                                        \
    {                                                                                 \
        static TypeId const tid = { (uint64_t)TypeIdHi##LL, (uint64_t)TypeIdLo##LL }; \
        return tid;                                                                   \
    }                                                                                 \
    FFMF_BEFRIEND_TYPEIDOF()

#define FFMF_DECLARE_TYPE_ID_EX(Type, TypeIdHi, TypeIdLo)                             \
template<>                                                                            \
struct ::ffmf::Details::typeidofImpl<Type>                                            \
{                                                                                     \
    inline static TypeId const& GetTypeId()                                           \
    {                                                                                 \
        static TypeId const tid = { (uint64_t)TypeIdHi##LL, (uint64_t)TypeIdLo##LL }; \
        return tid;                                                                   \
    }                                                                                 \
};

} // namespace ffmf
