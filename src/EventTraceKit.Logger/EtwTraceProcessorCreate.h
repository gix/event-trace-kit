#pragma once
#include <memory>
#include <string>
#include "ADT/ArrayRef.h"
#include "ITraceProcessor.h"
#include "ITraceSession.h"
#include "Support/DllExport.h"

namespace etk
{

ETK_EXPORT std::unique_ptr<ITraceProcessor> CreateEtwTraceProcessor(
    std::wstring loggerName, ArrayRef<TraceProviderSpec> providers);

} // namespace etk
