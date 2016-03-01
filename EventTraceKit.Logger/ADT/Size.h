#pragma once
#include "ffmf/Common/ADT/Length.h"
#include "ffmf/Common/Support/Types.h"
#include "ffmf/Common/Windows.h"

namespace ffmf
{

template<typename T>
class Size
{
public:
    Size() noexcept
        : width(T(0))
        , height(T(0))
    {
    }

    Size(T width, T height) noexcept
        : width(width)
        , height(height)
    {
    }

    Size(Size<T> const& size) noexcept
        : width(size.Width())
        , height(size.Height())
    {
    }

    T Width() const noexcept { return width; }
    void SetWidth(T value) noexcept { width = value; }

    T Height() const noexcept { return height; }
    void SetHeight(T value) noexcept { height = value; }

private:
    T width;
    T height;
};


template<>
class Size<Pixels>
{
public:
    Size() noexcept
        : width()
        , height()
    {
    }

    Size(Pixels width, Pixels height) noexcept
        : width(width)
        , height(height)
    {
    }

    Size(Size<Pixels> const& size) noexcept
        : width(size.Width())
        , height(size.Height())
    {
    }

    Pixels Width() const noexcept { return width; }
    void SetWidth(Pixels value) noexcept { width = value; }

    Pixels Height() const noexcept { return height; }
    void SetHeight(Pixels value) noexcept { height = value; }

    operator SIZE()
    {
        return { static_cast<long>(width.amount()),
                 static_cast<long>(height.amount()) };
    }

private:
    Pixels width;
    Pixels height;
};


typedef Size<int> SizeI;
typedef Size<unsigned> SizeU;
typedef Size<float> SizeF;
typedef Size<double> SizeD;

} // namespace ffmf
