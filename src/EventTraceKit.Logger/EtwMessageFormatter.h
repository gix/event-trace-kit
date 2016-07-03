#pragma once
#include "EventInfoCache.h"
#include <vector>

namespace etk
{

class EtwMessageFormatter
{
public:
    bool FormatEventMessage(
        EventInfo info, size_t pointerSize, wchar_t* buffer, size_t bufferSize);

private:
    std::vector<wchar_t> propertyBuffer;
    std::wstring formattedProperties;
    std::vector<size_t> formattedPropertiesOffsets;
    std::vector<DWORD_PTR> formattedPropertiesPointers;
};

} // namespace etk
