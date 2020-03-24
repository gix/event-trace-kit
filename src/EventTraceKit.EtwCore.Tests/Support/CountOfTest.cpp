#include "Support/CountOf.h"
#include <gtest/gtest.h>

namespace etk::tests
{

TEST(CountOfTest, CountBytes)
{
    uint8_t const input[128] = {};
    EXPECT_EQ(128u, countof(input));
}

TEST(CountOfTest, CountInts)
{
    uint32_t const input[23] = {};
    EXPECT_EQ(23u, countof(input));
}

TEST(CountOfTest, CountString)
{
    char const input[] = "1234567";
    EXPECT_EQ(8u, countof(input));
}

TEST(CountOfTest, CountWideString)
{
    wchar_t const input[] = L"1234567";
    EXPECT_EQ(8u, countof(input));
}

TEST(CountOfTest, CountUtf8String)
{
    char8_t const input[] = u8"1234567";
    EXPECT_EQ(8u, countof(input));
}

TEST(CountOfTest, CountUtf16String)
{
    char16_t const input[] = u"1234567";
    EXPECT_EQ(8u, countof(input));
}

TEST(CountOfTest, CountUtf32String)
{
    char32_t const input[] = U"1234567";
    EXPECT_EQ(8u, countof(input));
}

} // namespace etk::tests
