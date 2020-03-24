#include "ADT/Span.h"

#include <type_traits>
#include <vector>

#include <gtest/gtest.h>

namespace etk::tests
{

namespace
{

struct X
{
    X(int value)
        : value(value)
    {
    }

    bool operator==(X const& other) const { return value == other.value; }
    bool operator!=(X const& other) const { return value != other.value; }

    bool operator<(X const& other) const { return value < other.value; }
    bool operator>(X const& other) const { return value > other.value; }
    bool operator<=(X const& other) const { return value <= other.value; }
    bool operator>=(X const& other) const { return value >= other.value; }

    int value;
};

} // namespace

TEST(SpanTest, DefaultConstructor)
{
    span<int> const s1;

    EXPECT_EQ(nullptr, s1.data());
    EXPECT_EQ(0, s1.size());
}

TEST(SpanTest, Constructor_FromItem)
{
    int value = 1;
    span<int> const s(value);

    EXPECT_EQ(&value, s.data());
    EXPECT_EQ(1, s.size());
}

TEST(SpanTest, Constructor_FromArray)
{
    int values[] = {1, 2, 3, 4};
    span<int> const s(values, 3);

    EXPECT_EQ(values, s.data());
    EXPECT_EQ(3, s.size());
}

TEST(SpanTest, Constructor_FromStdArray)
{
    std::array<int, 4> values = {{1, 2, 3, 4}};
    span<int> const s(values);

    EXPECT_EQ(values.data(), s.data());
    EXPECT_EQ(values.size(), s.size());
}

TEST(SpanTest, Constructor_FromStdVector)
{
    std::vector<int> values = {1, 2, 3, 4};
    span<int> const s(values);

    EXPECT_EQ(values.data(), s.data());
    EXPECT_EQ(values.size(), s.size());
}

TEST(SpanTest, CopyConstructor)
{
    int values[] = {1, 2};
    span<int> const s(values, 2);

    span<int> const copy(s);

    EXPECT_EQ(values, s.data());
    EXPECT_EQ(2, s.size());
    EXPECT_EQ(s.data(), copy.data());
    EXPECT_EQ(s.size(), copy.size());
}

TEST(SpanTest, CopyAssignment)
{
    int values[] = {1, 2};
    span<int> const src(values, 2);

    span<int> copy;
    copy = src;

    EXPECT_EQ(values, src.data());
    EXPECT_EQ(2, src.size());
    EXPECT_EQ(src.data(), copy.data());
    EXPECT_EQ(src.size(), copy.size());
}

TEST(SpanTest, MoveConstructor)
{
    int values[] = {1, 2};
    span<int> src(values, 2);

    span<int> const moved(std::move(src));

    EXPECT_EQ(values, src.data());
    EXPECT_EQ(2, src.size());
    EXPECT_EQ(src.data(), moved.data());
    EXPECT_EQ(src.size(), moved.size());
}

TEST(SpanTest, MoveAssignment)
{
    int values[] = {1, 2};
    span<int> src(values, 2);

    span<int> moved;
    moved = std::move(src);

    EXPECT_EQ(values, src.data());
    EXPECT_EQ(2, src.size());
    EXPECT_EQ(src.data(), moved.data());
    EXPECT_EQ(src.size(), moved.size());
}

TEST(SpanTest, size_bytes)
{
    int values[] = {1, 2};
    span<int> s(values, 2);

    EXPECT_EQ(8, s.size_bytes());
}

TEST(SpanTest, empty)
{
    int values[] = {1, 2};
    span<int> const s1(values, 2);
    span<int> const s2;

    EXPECT_FALSE(s1.empty());
    EXPECT_TRUE(s2.empty());
}

TEST(SpanTest, SubscriptOperator)
{
    int values[] = {1, 2};
    span<int> const src(values, 2);

    src[0] = 42;

    EXPECT_EQ(values, src.data());
    EXPECT_EQ(2, src.size());
    EXPECT_EQ(42, values[0]);
    EXPECT_EQ(2, values[1]);

    static_assert(std::is_same_v<int&, decltype(std::declval<span<int>>()[0])>);
    static_assert(std::is_same_v<int const&, decltype(std::declval<span<int const>>()[0])>);
}

TEST(SpanTest, data)
{
    int values[] = {1, 2};
    span<int> const s1(values, 2);
    span<int const> const s2(values, 2);

    s1.data()[0] = 42;

    EXPECT_EQ(values, s1.data());
    EXPECT_EQ(values, s2.data());
    EXPECT_EQ(42, values[0]);
    EXPECT_EQ(2, values[1]);

    static_assert(std::is_same_v<int*, decltype(std::declval<span<int>>().data())>);
    static_assert(std::is_same_v<int const*, decltype(std::declval<span<int const>>().data())>);
}

TEST(SpanTest, EqualityOperator)
{
    int values1[] = {1, 2};
    int values2[] = {1, 2};
    span<int> const s1(values1, 2);
    span<int> const s2(values1, 2);
    span<int> const s3(values2, 2);

    EXPECT_TRUE(s1 == s1);
    EXPECT_TRUE(s1 == s2);
    EXPECT_TRUE(s2 == s1);
    EXPECT_FALSE(s1 == s3);
    EXPECT_FALSE(s3 == s1);
}

TEST(SpanTest, EqualityOperator_UDT)
{
    X values1[] = {X(1), X(2)};
    X values2[] = {X(1), X(2)};
    span<X> const s1(values1, 2);
    span<X> const s2(values1, 2);
    span<X> const s3(values2, 2);

    EXPECT_TRUE(s1 == s1);
    EXPECT_TRUE(s1 == s2);
    EXPECT_TRUE(s2 == s1);
    EXPECT_FALSE(s1 == s3);
    EXPECT_FALSE(s3 == s1);
}

TEST(SpanTest, EqualityOperator_DifferentValues)
{
    int values1[] = {1, 2};
    int values2[] = {2, 2};
    span<int> const s1(values1, 2);
    span<int> const s2(values2, 2);

    EXPECT_FALSE(s1 == s2);
    EXPECT_FALSE(s2 == s1);
}

TEST(SpanTest, EqualityOperator_DifferentLengths)
{
    int values[] = {1, 2};
    span<int> const s1(values, 2);
    span<int> const s2(values, 1);

    EXPECT_FALSE(s1 == s2);
    EXPECT_FALSE(s2 == s1);
}

TEST(SpanTest, InequalityOperator)
{
    int values1[] = {1, 2};
    int values2[] = {1, 2};
    span<int> const s1(values1, 2);
    span<int> const s2(values1, 2);
    span<int> const s3(values2, 2);

    EXPECT_FALSE(s1 != s2);
    EXPECT_FALSE(s2 != s1);
    EXPECT_TRUE(s1 != s3);
    EXPECT_TRUE(s3 != s1);
}

TEST(SpanTest, InequalityOperator_UDT)
{
    X values1[] = {X(1), X(2)};
    X values2[] = {X(1), X(2)};
    span<X> const s1(values1, 2);
    span<X> const s2(values1, 2);
    span<X> const s3(values2, 2);

    EXPECT_FALSE(s1 != s2);
    EXPECT_FALSE(s2 != s1);
    EXPECT_TRUE(s1 != s3);
    EXPECT_TRUE(s3 != s1);
}

TEST(SpanTest, InequalityOperator_DifferentValues)
{
    int values1[] = {1, 2};
    int values2[] = {2, 2};
    span<int> const s1(values1, 2);
    span<int> const s2(values2, 2);

    EXPECT_TRUE(s1 != s2);
    EXPECT_TRUE(s2 != s1);
}

TEST(SpanTest, InequalityOperator_DifferentLengths)
{
    X values[] = {1, 2};
    span<X> const s1(values, 2);
    span<X> const s2(values, 1);

    EXPECT_TRUE(s1 != s2);
    EXPECT_TRUE(s2 != s1);
}

TEST(SpanTest, LessThanOperator)
{
    X values[] = {1, 2, 3, 4, 5};
    span<X> const s1(values, 2);
    span<X> const s2(&values[1], 3);
    span<X> const s3(&values[1], 2);
    span<X> const s4(&values[1], 1);
    span<X> const s5(values, 3);
    span<X> const s6(values, 2);
    span<X> const s7(values, 1);

    // Same variable
    EXPECT_FALSE(s1 < s1);

    // Smaller pointer, smaller size
    EXPECT_TRUE(s1 < s2);
    EXPECT_FALSE(s2 < s1);

    // Smaller pointer, equal size
    EXPECT_TRUE(s1 < s3);
    EXPECT_FALSE(s3 < s1);

    // Smaller pointer, larger size
    EXPECT_TRUE(s1 < s4);
    EXPECT_FALSE(s4 < s1);

    // Equal pointer, smaller size
    EXPECT_TRUE(s1 < s5);
    EXPECT_FALSE(s5 < s1);

    // Equal pointer and size
    EXPECT_FALSE(s1 < s6);
    EXPECT_FALSE(s6 < s1);

    // Equal pointer, larger size
    EXPECT_FALSE(s1 < s7);
    EXPECT_TRUE(s7 < s1);
}

TEST(SpanTest, GreaterThanOperator)
{
    X values[] = {1, 2, 3, 4, 5};
    span<X> const s1(values, 2);
    span<X> const s2(&values[1], 3);
    span<X> const s3(&values[1], 2);
    span<X> const s4(&values[1], 1);
    span<X> const s5(values, 3);
    span<X> const s6(values, 2);
    span<X> const s7(values, 1);

    // Same variable
    EXPECT_FALSE(s1 > s1);

    // Smaller pointer, smaller size
    EXPECT_TRUE(s2 > s1);
    EXPECT_FALSE(s1 > s2);

    // Smaller pointer, equal size
    EXPECT_TRUE(s3 > s1);
    EXPECT_FALSE(s1 > s3);

    // Smaller pointer, larger size
    EXPECT_TRUE(s4 > s1);
    EXPECT_FALSE(s1 > s4);

    // Equal pointer, smaller size
    EXPECT_TRUE(s5 > s1);
    EXPECT_FALSE(s1 > s5);

    // Equal pointer and size
    EXPECT_FALSE(s6 > s1);
    EXPECT_FALSE(s1 > s6);

    // Equal pointer, larger size
    EXPECT_FALSE(s7 > s1);
    EXPECT_TRUE(s1 > s7);
}

TEST(SpanTest, LessThanOrEqualOperator)
{
    X values[] = {1, 2, 3, 4, 5};
    span<X> const s1(values, 2);
    span<X> const s2(&values[1], 3);
    span<X> const s3(&values[1], 2);
    span<X> const s4(&values[1], 1);
    span<X> const s5(values, 3);
    span<X> const s6(values, 2);
    span<X> const s7(values, 1);

    // Same variable
    EXPECT_TRUE(s1 <= s1);

    // Smaller pointer, smaller size
    EXPECT_TRUE(s1 <= s2);
    EXPECT_FALSE(s2 <= s1);

    // Smaller pointer, equal size
    EXPECT_TRUE(s1 <= s3);
    EXPECT_FALSE(s3 <= s1);

    // Smaller pointer, larger size
    EXPECT_TRUE(s1 <= s4);
    EXPECT_FALSE(s4 <= s1);

    // Equal pointer, smaller size
    EXPECT_TRUE(s1 <= s5);
    EXPECT_FALSE(s5 <= s1);

    // Equal pointer and size
    EXPECT_TRUE(s1 <= s6);
    EXPECT_TRUE(s6 <= s1);

    // Equal pointer, larger size
    EXPECT_FALSE(s1 <= s7);
    EXPECT_TRUE(s7 <= s1);
}

TEST(SpanTest, GreaterThanOrEqualOperator)
{
    X values[] = {1, 2, 3, 4, 5};
    span<X> const s1(values, 2);
    span<X> const s2(&values[1], 3);
    span<X> const s3(&values[1], 2);
    span<X> const s4(&values[1], 1);
    span<X> const s5(values, 3);
    span<X> const s6(values, 2);
    span<X> const s7(values, 1);

    // Same variable
    EXPECT_TRUE(s1 >= s1);

    // Smaller pointer, smaller size
    EXPECT_TRUE(s2 >= s1);
    EXPECT_FALSE(s1 >= s2);

    // Smaller pointer, equal size
    EXPECT_TRUE(s3 >= s1);
    EXPECT_FALSE(s1 >= s3);

    // Smaller pointer, larger size
    EXPECT_TRUE(s4 >= s1);
    EXPECT_FALSE(s1 >= s4);

    // Equal pointer, smaller size
    EXPECT_TRUE(s5 >= s1);
    EXPECT_FALSE(s1 >= s5);

    // Equal pointer and size
    EXPECT_TRUE(s6 >= s1);
    EXPECT_TRUE(s1 >= s6);

    // Equal pointer, larger size
    EXPECT_FALSE(s7 >= s1);
    EXPECT_TRUE(s1 >= s7);
}

TEST(SpanTest, as_bytes)
{
    X values[] = {0x11223344, 0x55667788};
    span<X> const s(values, 2);

    span<std::byte const> b = as_bytes(s);

    EXPECT_EQ((uintptr_t)values, (uintptr_t)b.data());
    EXPECT_EQ(8, b.size());
    EXPECT_EQ(0x44, (uint8_t)b[0]);
    EXPECT_EQ(0x55, (uint8_t)b[7]);
}


TEST(SpanTest_StaticExtent, DefaultConstructor)
{
    span<int, 0> const s1;
    EXPECT_EQ(nullptr, s1.data());
    EXPECT_EQ(0, s1.size());

    static_assert(!std::is_default_constructible_v<span<int, 1>>);
}

TEST(SpanTest_StaticExtent, Constructor_FromItem)
{
    int value = 1;
    span<int, 1> const s(value);

    EXPECT_EQ(&value, s.data());
    EXPECT_EQ(1, s.size());
}

TEST(SpanTest_StaticExtent, Constructor_FromArray)
{
    int values[] = {1, 2, 3, 4};
    span<int, 3> const s(values, 3);

    EXPECT_EQ(values, s.data());
    EXPECT_EQ(3, s.size());
}

TEST(SpanTest_StaticExtent, Constructor_FromStdArray)
{
    std::array<int, 4> values = {{1, 2, 3, 4}};
    span<int, 4> const s(values);

    EXPECT_EQ(values.data(), s.data());
    EXPECT_EQ(values.size(), s.size());

    static_assert(!std::is_constructible_v<span<int, 3>, std::array<int, 4>>);
}

TEST(SpanTest_StaticExtent, CopyConstructor)
{
    int values[] = {1, 2};
    span<int, 2> const s(values, 2);

    span<int, 2> const copy(s);

    EXPECT_EQ(values, s.data());
    EXPECT_EQ(2, s.size());
    EXPECT_EQ(s.data(), copy.data());
    EXPECT_EQ(s.size(), copy.size());
}

TEST(SpanTest_StaticExtent, CopyAssignment)
{
    int values[] = {1, 2};
    int dummy[2] = {};
    span<int, 2> const src(values, 2);

    span<int, 2> copy(dummy, 2);
    copy = src;

    EXPECT_EQ(values, src.data());
    EXPECT_EQ(2, src.size());
    EXPECT_EQ(src.data(), copy.data());
    EXPECT_EQ(src.size(), copy.size());
}

TEST(SpanTest_StaticExtent, MoveConstructor)
{
    int values[] = {1, 2};
    span<int, 2> src(values, 2);

    span<int, 2> const moved(std::move(src));

    EXPECT_EQ(values, src.data());
    EXPECT_EQ(2, src.size());
    EXPECT_EQ(src.data(), moved.data());
    EXPECT_EQ(src.size(), moved.size());
}

TEST(SpanTest_StaticExtent, MoveAssignment)
{
    int values[] = {1, 2};
    int dummy[2] = {};
    span<int, 2> src(values, 2);

    span<int, 2> moved(dummy, 2);
    moved = std::move(src);

    EXPECT_EQ(values, src.data());
    EXPECT_EQ(2, src.size());
    EXPECT_EQ(src.data(), moved.data());
    EXPECT_EQ(src.size(), moved.size());
}

TEST(SpanTest_StaticExtent, size_bytes)
{
    int values[] = {1, 2};
    span<int, 2> s(values, 2);

    EXPECT_EQ(8, s.size_bytes());
}

TEST(SpanTest_StaticExtent, empty)
{
    int values[] = {1, 2};
    span<int, 2> const s1(values, 2);
    span<int, 0> const s2;

    EXPECT_FALSE(s1.empty());
    EXPECT_TRUE(s2.empty());
}

TEST(SpanTest_StaticExtent, SubscriptOperator)
{
    int values[] = {1, 2};
    span<int, 2> const src(values, 2);

    src[0] = 42;

    EXPECT_EQ(values, src.data());
    EXPECT_EQ(2, src.size());
    EXPECT_EQ(42, values[0]);
    EXPECT_EQ(2, values[1]);

    static_assert(std::is_same_v<int&, decltype(span<int>()[0])>);
    static_assert(std::is_same_v<int const&, decltype(span<int const>()[0])>);
}

TEST(SpanTest_StaticExtent, data)
{
    int values[] = {1, 2};
    span<int, 2> const s1(values, 2);
    span<int const, 2> const s2(values, 2);

    s1.data()[0] = 42;

    EXPECT_EQ(values, s1.data());
    EXPECT_EQ(values, s2.data());
    EXPECT_EQ(42, values[0]);
    EXPECT_EQ(2, values[1]);

    static_assert(std::is_same_v<int*, decltype(std::declval<span<int, 2>>().data())>);
    static_assert(std::is_same_v<int const*, decltype(std::declval<span<int const, 2>>().data())>);
}

TEST(SpanTest_StaticExtent, EqualityOperator)
{
    int values1[] = {1, 2};
    int values2[] = {1, 2};
    span<int, 2> const s1(values1, 2);
    span<int, 2> const s2(values1, 2);
    span<int, 2> const s3(values2, 2);

    EXPECT_TRUE(s1 == s1);
    EXPECT_TRUE(s1 == s2);
    EXPECT_TRUE(s2 == s1);
    EXPECT_FALSE(s1 == s3);
    EXPECT_FALSE(s3 == s1);
}

TEST(SpanTest_StaticExtent, EqualityOperator_UDT)
{
    X values1[] = {X(1), X(2)};
    X values2[] = {X(1), X(2)};
    span<X, 2> const s1(values1, 2);
    span<X, 2> const s2(values1, 2);
    span<X, 2> const s3(values2, 2);

    EXPECT_TRUE(s1 == s1);
    EXPECT_TRUE(s1 == s2);
    EXPECT_TRUE(s2 == s1);
    EXPECT_FALSE(s1 == s3);
    EXPECT_FALSE(s3 == s1);
}

TEST(SpanTest_StaticExtent, EqualityOperator_DifferentValues)
{
    int values1[] = {1, 2};
    int values2[] = {2, 2};
    span<int, 2> const s1(values1, 2);
    span<int, 2> const s2(values2, 2);

    EXPECT_FALSE(s1 == s2);
    EXPECT_FALSE(s2 == s1);
}

TEST(SpanTest_StaticExtent, EqualityOperator_DifferentLengths)
{
    int values[] = {1, 2};
    span<int, 2> const s1(values, 2);
    span<int, 1> const s2(values, 1);

    EXPECT_FALSE(s1 == s2);
    EXPECT_FALSE(s2 == s1);
}

TEST(SpanTest_StaticExtent, InequalityOperator)
{
    int values1[] = {1, 2};
    int values2[] = {1, 2};
    span<int, 2> const s1(values1, 2);
    span<int, 2> const s2(values1, 2);
    span<int, 2> const s3(values2, 2);

    EXPECT_FALSE(s1 != s2);
    EXPECT_FALSE(s2 != s1);
    EXPECT_TRUE(s1 != s3);
    EXPECT_TRUE(s3 != s1);
}

TEST(SpanTest_StaticExtent, InequalityOperator_UDT)
{
    X values1[] = {X(1), X(2)};
    X values2[] = {X(1), X(2)};
    span<X, 2> const s1(values1, 2);
    span<X, 2> const s2(values1, 2);
    span<X, 2> const s3(values2, 2);

    EXPECT_FALSE(s1 != s2);
    EXPECT_FALSE(s2 != s1);
    EXPECT_TRUE(s1 != s3);
    EXPECT_TRUE(s3 != s1);
}

TEST(SpanTest_StaticExtent, InequalityOperator_DifferentValues)
{
    int values1[] = {1, 2};
    int values2[] = {2, 2};
    span<int, 2> const s1(values1, 2);
    span<int, 2> const s2(values2, 2);

    EXPECT_TRUE(s1 != s2);
    EXPECT_TRUE(s2 != s1);
}

TEST(SpanTest_StaticExtent, InequalityOperator_DifferentLengths)
{
    X values[] = {1, 2};
    span<X, 2> const s1(values, 2);
    span<X, 1> const s2(values, 1);

    EXPECT_TRUE(s1 != s2);
    EXPECT_TRUE(s2 != s1);
}

TEST(SpanTest_StaticExtent, LessThanOperator)
{
    X values[] = {1, 2, 3, 4, 5};
    span<X, 2> const s1(values, 2);
    span<X, 3> const s2(&values[1], 3);
    span<X, 2> const s3(&values[1], 2);
    span<X, 1> const s4(&values[1], 1);
    span<X, 3> const s5(values, 3);
    span<X, 2> const s6(values, 2);
    span<X, 1> const s7(values, 1);

    // Same variable
    EXPECT_FALSE(s1 < s1);

    // Smaller pointer, smaller size
    EXPECT_TRUE(s1 < s2);
    EXPECT_FALSE(s2 < s1);

    // Smaller pointer, equal size
    EXPECT_TRUE(s1 < s3);
    EXPECT_FALSE(s3 < s1);

    // Smaller pointer, larger size
    EXPECT_TRUE(s1 < s4);
    EXPECT_FALSE(s4 < s1);

    // Equal pointer, smaller size
    EXPECT_TRUE(s1 < s5);
    EXPECT_FALSE(s5 < s1);

    // Equal pointer and size
    EXPECT_FALSE(s1 < s6);
    EXPECT_FALSE(s6 < s1);

    // Equal pointer, larger size
    EXPECT_FALSE(s1 < s7);
    EXPECT_TRUE(s7 < s1);
}

TEST(SpanTest_StaticExtent, GreaterThanOperator)
{
    X values[] = {1, 2, 3, 4, 5};
    span<X, 2> const s1(values, 2);
    span<X, 3> const s2(&values[1], 3);
    span<X, 2> const s3(&values[1], 2);
    span<X, 1> const s4(&values[1], 1);
    span<X, 3> const s5(values, 3);
    span<X, 2> const s6(values, 2);
    span<X, 1> const s7(values, 1);

    // Same variable
    EXPECT_FALSE(s1 > s1);

    // Smaller pointer, smaller size
    EXPECT_TRUE(s2 > s1);
    EXPECT_FALSE(s1 > s2);

    // Smaller pointer, equal size
    EXPECT_TRUE(s3 > s1);
    EXPECT_FALSE(s1 > s3);

    // Smaller pointer, larger size
    EXPECT_TRUE(s4 > s1);
    EXPECT_FALSE(s1 > s4);

    // Equal pointer, smaller size
    EXPECT_TRUE(s5 > s1);
    EXPECT_FALSE(s1 > s5);

    // Equal pointer and size
    EXPECT_FALSE(s6 > s1);
    EXPECT_FALSE(s1 > s6);

    // Equal pointer, larger size
    EXPECT_FALSE(s7 > s1);
    EXPECT_TRUE(s1 > s7);
}

TEST(SpanTest_StaticExtent, LessThanOrEqualOperator)
{
    X values[] = {1, 2, 3, 4, 5};
    span<X, 2> const s1(values, 2);
    span<X, 3> const s2(&values[1], 3);
    span<X, 2> const s3(&values[1], 2);
    span<X, 1> const s4(&values[1], 1);
    span<X, 3> const s5(values, 3);
    span<X, 2> const s6(values, 2);
    span<X, 1> const s7(values, 1);

    // Same variable
    EXPECT_TRUE(s1 <= s1);

    // Smaller pointer, smaller size
    EXPECT_TRUE(s1 <= s2);
    EXPECT_FALSE(s2 <= s1);

    // Smaller pointer, equal size
    EXPECT_TRUE(s1 <= s3);
    EXPECT_FALSE(s3 <= s1);

    // Smaller pointer, larger size
    EXPECT_TRUE(s1 <= s4);
    EXPECT_FALSE(s4 <= s1);

    // Equal pointer, smaller size
    EXPECT_TRUE(s1 <= s5);
    EXPECT_FALSE(s5 <= s1);

    // Equal pointer and size
    EXPECT_TRUE(s1 <= s6);
    EXPECT_TRUE(s6 <= s1);

    // Equal pointer, larger size
    EXPECT_FALSE(s1 <= s7);
    EXPECT_TRUE(s7 <= s1);
}

TEST(SpanTest_StaticExtent, GreaterThanOrEqualOperator)
{
    X values[] = {1, 2, 3, 4, 5};
    span<X, 2> const s1(values, 2);
    span<X, 3> const s2(&values[1], 3);
    span<X, 2> const s3(&values[1], 2);
    span<X, 1> const s4(&values[1], 1);
    span<X, 3> const s5(values, 3);
    span<X, 2> const s6(values, 2);
    span<X, 1> const s7(values, 1);

    // Same variable
    EXPECT_TRUE(s1 >= s1);

    // Smaller pointer, smaller size
    EXPECT_TRUE(s2 >= s1);
    EXPECT_FALSE(s1 >= s2);

    // Smaller pointer, equal size
    EXPECT_TRUE(s3 >= s1);
    EXPECT_FALSE(s1 >= s3);

    // Smaller pointer, larger size
    EXPECT_TRUE(s4 >= s1);
    EXPECT_FALSE(s1 >= s4);

    // Equal pointer, smaller size
    EXPECT_TRUE(s5 >= s1);
    EXPECT_FALSE(s1 >= s5);

    // Equal pointer and size
    EXPECT_TRUE(s6 >= s1);
    EXPECT_TRUE(s1 >= s6);

    // Equal pointer, larger size
    EXPECT_FALSE(s7 >= s1);
    EXPECT_TRUE(s1 >= s7);
}

TEST(SpanTest_StaticExtent, as_bytes)
{
    X values[] = {0x11223344, 0x55667788};
    span<X, 2> const s(values, 2);

    span<std::byte const, 8> b = as_bytes(s);

    EXPECT_EQ((uintptr_t)values, (uintptr_t)b.data());
    EXPECT_EQ(8, b.size());
    EXPECT_EQ(0x44, (uint8_t)b[0]);
    EXPECT_EQ(0x55, (uint8_t)b[7]);
}

} // namespace etk::tests
