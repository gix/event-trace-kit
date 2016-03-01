#pragma once
#include "CompilerSupport.h"

#include <type_traits>
#include <Unknwn.h>

namespace ffmf
{

/// Releases a COM pointer and sets the pointer to 0.
template<typename T, typename = std::enable_if_t<std::is_base_of<IUnknown, T>::value>>
FFMF_ALWAYS_INLINE
void SafeRelease(T*& ptr)
{
    if (ptr) {
        ptr->Release();
        ptr = nullptr;
    }
}

} // namespace ffmf
