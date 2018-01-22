#pragma once
#include "EventInfoCache.h"
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
    std::wstring formattedProperties;
    std::vector<size_t> formattedPropertiesOffsets;
    std::vector<DWORD_PTR> formattedPropertiesPointers;
};

} // namespace etk
