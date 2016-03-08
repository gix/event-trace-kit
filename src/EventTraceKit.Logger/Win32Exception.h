#pragma once
#include "Support/CompilerSupport.h"
#include <exception>
#include <string>

namespace etk
{

class Win32Exception
    : public virtual std::exception
{
public:
    Win32Exception();
    Win32Exception(long error);
    Win32Exception(unsigned long error);
    Win32Exception(std::wstring const& message);
    Win32Exception(long error, std::wstring const& message);
    Win32Exception(unsigned long error, std::wstring const& message);

    virtual char const* what() const noexcept override;

private:
    void Format();

    unsigned long error;
    std::wstring message;
    std::string ansiMessage;
};

} // namespace etk
