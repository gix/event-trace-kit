#pragma once
#if __cplusplus_cli

namespace EventTraceKit
{

public ref class ParseTdhContext
{
public:
    ParseTdhContext()
    {
        NativePointerSize = 8;
    }

    property int NativePointerSize;
};

} // namespace EventTraceKit

#endif // __cplusplus_cli
