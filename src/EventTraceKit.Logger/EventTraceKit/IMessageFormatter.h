#pragma once
#if __cplusplus_cli
#include "Descriptors.h"
#include "ParseTdhContext.h"

namespace EventTraceKit
{

public interface class IMessageFormatter
{
public:
    System::String^ GetMessageForEvent(
        EventInfo eventInfo,
        ParseTdhContext^ parseTdhContext,
        System::IFormatProvider^ formatProvider);
};

} // namespace EventTraceKit

#endif // __cplusplus_cli
