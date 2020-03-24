#include "etk/Support/StringConversions.h"

#include <Windows.h>
#include <type_traits>

namespace etk
{

namespace
{

struct U8To16Conversion
{
    using InputChar = char;
    using OutputChar =
        std::conditional_t<sizeof(wchar_t) == sizeof(char16_t), wchar_t, char16_t>;
    static bool EstimateLength(InputChar const* source, size_t sourceLength,
                               size_t& outLength) noexcept;
    static size_t Convert(OutputChar* out, size_t outLength, InputChar const* source,
                          size_t sourceLength) noexcept;
};

bool U8To16Conversion::EstimateLength(char const* source, size_t sourceLength,
                                      size_t& outLength) noexcept
{
    int const length = MultiByteToWideChar(CP_UTF8, 0, source,
                                           static_cast<int>(sourceLength), nullptr, 0);
    if (length <= 0)
        return false;

    outLength = static_cast<size_t>(length);
    return true;
}

size_t U8To16Conversion::Convert(wchar_t* out, size_t outSize, char const* source,
                                 size_t sourceLength) noexcept
{
    if (outSize == 0)
        return 0;

    int const length =
        MultiByteToWideChar(CP_UTF8, 0, source, static_cast<int>(sourceLength), out,
                            static_cast<int>(outSize));
    if (length <= 0)
        return 0;

    return static_cast<size_t>(length);
}

struct U16To8Conversion
{
    using InputChar =
        std::conditional_t<sizeof(wchar_t) == sizeof(char16_t), wchar_t, char16_t>;
    using OutputChar = char;
    static bool EstimateLength(InputChar const* source, size_t sourceLength,
                               size_t& outLength) noexcept;
    static size_t Convert(OutputChar* out, size_t outLength, InputChar const* source,
                          size_t sourceLength) noexcept;
};

bool U16To8Conversion::EstimateLength(wchar_t const* source, size_t sourceLength,
                                      size_t& outLength) noexcept
{
    int const length = WideCharToMultiByte(
        CP_UTF8, 0, source, static_cast<int>(sourceLength), nullptr, 0, nullptr, nullptr);
    if (length <= 0)
        return false;

    outLength = static_cast<size_t>(length);
    return true;
}

size_t U16To8Conversion::Convert(char* out, size_t outSize, wchar_t const* source,
                                 size_t sourceLength) noexcept
{
    if (outSize == 0)
        return 0;

    int const length =
        WideCharToMultiByte(CP_UTF8, 0, source, static_cast<int>(sourceLength), out,
                            static_cast<int>(outSize), nullptr, nullptr);
    if (length <= 0)
        return 0;

    return static_cast<size_t>(length);
}

template<typename Converter,
         typename OutputCharTraits = std::char_traits<typename Converter::OutputChar>,
         typename OutputStringAllocator = std::allocator<typename Converter::OutputChar>>
bool Convert(typename Converter::InputChar const* source, size_t sourceLength,
             std::basic_string<typename Converter::OutputChar, OutputCharTraits,
                               OutputStringAllocator>& buffer)
{
    if (sourceLength == 0)
        return true;

    size_t outLength;
    if (!Converter::EstimateLength(source, sourceLength, outLength))
        return false;

    buffer.resize(outLength);

    size_t const convertedLength =
        Converter::Convert(&buffer[0], buffer.length() + 1, source, sourceLength);
    if (convertedLength == 0) {
        buffer.clear();
        return false;
    }

    buffer.resize(convertedLength);
    return true;
}

} // namespace

bool U8To16(std::string_view source, std::wstring& output)
{
    return Convert<U8To16Conversion>(source.data(), source.length(), output);
}

std::wstring U8To16(std::string_view source)
{
    std::wstring buffer;
    (void)Convert<U8To16Conversion>(source.data(), source.length(), buffer);
    return buffer;
}

bool U16To8(std::wstring_view source, std::string& output)
{
    return Convert<U16To8Conversion>(source.data(), source.length(), output);
}

std::string U16To8(std::wstring_view source)
{
    std::string buffer;
    (void)Convert<U16To8Conversion>(source.data(), source.length(), buffer);
    return buffer;
}

} // namespace etk
