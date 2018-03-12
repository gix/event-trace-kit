#pragma once
#include "EventInfoCache.h"
#include "ADT/SmallVector.h"
#include <vector>

namespace etk
{

class TdhMessageFormatter
{
public:
    bool FormatEventMessage(
        EventInfo info, size_t pointerSize, wchar_t* buffer, size_t bufferSize);

private:
    bool FormatMofEvent(
        EventInfo const& info, size_t pointerSize, wchar_t* buffer, size_t bufferSize);

    std::vector<wchar_t> propertyBuffer;
    std::vector<BYTE> mapBuffer;
    std::wstring formattedProperties;
    SmallVector<size_t, 16> formattedPropertiesOffsets;
    SmallVector<DWORD_PTR, 16> formattedPropertiesPointers;
};

} // namespace etk
