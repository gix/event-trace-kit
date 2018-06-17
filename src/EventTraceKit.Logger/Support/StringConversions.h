#pragma once
#include <string>
#include <string_view>

namespace etk
{

bool U8To16(std::string_view source, std::wstring& output);
std::wstring U8To16(std::string_view source);

bool U16To8(std::wstring_view source, std::string& output);
std::string U16To8(std::wstring_view source);

} // namespace etk
