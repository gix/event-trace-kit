#include "Variant.h"

#include "ffmf/Common/Diagnostics/Contracts.h"
#include "ffmf/Common/Debug.h"
#include "ffmf/Common/Support/CompilerSupport.h"
#include "ffmf/Common/Support/Unused.h"

#include <memory>
#include <oleauto.h>


namespace ffmf
{

Variant::Variant() noexcept
    : tagVARIANT()
{
    VariantInit(this);
}

Variant::~Variant() noexcept
{
    HRESULT hr = Clear();
    FFMF_ASSERT(SUCCEEDED(hr));
    unused(hr);
}

Variant::Variant(_In_ Variant const& source) noexcept
{
    vt = VT_EMPTY;
    InternalCopy(&source);
}

Variant::Variant(_In_ VARIANT const& source) noexcept
{
    vt = VT_EMPTY;
    InternalCopy(&source);
}

Variant::Variant(_In_ Variant&& source) noexcept
{
    (void)Attach(&source);
}

Variant::Variant(_In_ char source) noexcept
{
    vt = VT_I1;
    cVal = source;
}

Variant::Variant(_In_ short source) noexcept
{
    vt = VT_I2;
    iVal = source;
}

Variant::Variant(_In_ int source, _In_ VARTYPE type /*= VT_I4*/) noexcept
{
    FFMF_ASSERT(type == VT_I4 || type == VT_INT);
    if (type == VT_I4 || type == VT_INT) {
        vt = type;
        intVal = source;
    } else {
        vt = VT_ERROR;
        scode = E_INVALIDARG;
    }
}

Variant::Variant(_In_ long value, _In_ VARTYPE type /*= VT_I4*/) noexcept
{
    FFMF_ASSERT(type == VT_I4 || type == VT_ERROR);
    if (type == VT_I4 || type == VT_ERROR) {
        vt = type;
        lVal = value;
    } else {
        vt = VT_ERROR;
        scode = E_INVALIDARG;
    }
}

Variant::Variant(_In_ long long value) noexcept
{
    vt = VT_I8;
    llVal = value;
}

Variant::Variant(_In_ unsigned char value) noexcept
{
    vt = VT_UI1;
    bVal = value;
}

Variant::Variant(_In_ unsigned short value) noexcept
{
    vt = VT_UI2;
    uiVal = value;
}

Variant::Variant(_In_ unsigned int value, _In_ VARTYPE type /*= VT_UI4*/) noexcept
{
    FFMF_ASSERT(type == VT_UI4 || type == VT_UINT);
    if (type == VT_UI4 || type == VT_UINT) {
        vt = type;
        uintVal= value;
    } else {
        vt = VT_ERROR;
        scode = E_INVALIDARG;
    }
}

Variant::Variant(_In_ unsigned long value) noexcept
{
    vt = VT_UI4;
    ulVal = value;
}

Variant::Variant(_In_ unsigned long long value) noexcept
{
    vt = VT_UI8;
    ullVal = value;
}

Variant::Variant(_In_ bool value) noexcept
{
    vt = VT_BOOL;
    boolVal = value ? VARIANT_TRUE : VARIANT_FALSE;
}

Variant::Variant(_In_ float value) noexcept
{
    vt = VT_R4;
    fltVal = value;
}

Variant::Variant(_In_ double value, _In_ VARTYPE type /*= VT_R8*/) noexcept
{
    FFMF_ASSERT(type == VT_R8 || type == VT_DATE);
    if (type == VT_R8 || type == VT_DATE) {
        vt = type;
        dblVal = value;
    } else {
        vt = VT_ERROR;
        scode = E_INVALIDARG;
    }
}

//Variant::Variant(_In_z_ char const* value) noexcept
//{
//    *this = value;
//}

Variant::Variant(_In_z_ wchar_t const* value) noexcept
{
    *this = value;
}

Variant::Variant(_In_ CY value) noexcept
{
    vt = VT_CY;
    cyVal.Hi = value.Hi;
    cyVal.Lo = value.Lo;
}

Variant::Variant(_In_opt_ IDispatch* value) noexcept
{
    vt = VT_DISPATCH;
    pdispVal = value;
    if (pdispVal != nullptr)
        pdispVal->AddRef();
}

Variant::Variant(_In_opt_ IUnknown* value) noexcept
{
    vt = VT_UNKNOWN;
    punkVal = value;
    if (punkVal != nullptr)
        punkVal->AddRef();
}

Variant::Variant(_In_ SAFEARRAY const* source) noexcept
{
    FFMF_ASSERT(source != nullptr);
    if (source == nullptr) {
        vt = VT_ERROR;
        scode = E_INVALIDARG;
        return;
    }

    SAFEARRAY* copy = nullptr;
    HRESULT hr = ::SafeArrayCopy(const_cast<SAFEARRAY*>(source), &copy);
    if (FAILED(hr)) {
        vt = VT_ERROR;
        scode = hr;
        return;
    }

    SafeArrayGetVartype(const_cast<SAFEARRAY*>(source), &vt);
    vt |= VT_ARRAY;
    parray = copy;
}

Variant& Variant::operator =(_In_ Variant source) noexcept
{
   source.swap(*this);
   return *this;
}

Variant& Variant::operator =(_In_ VARIANT const& source) noexcept
{
    if (static_cast<VARIANT*>(this) != &source)
        InternalCopy(&source);
    return *this;
}

Variant& Variant::operator =(_In_ char value) noexcept
{
    ClearIfNot(VT_I1);
    cVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ short value) noexcept
{
    ClearIfNot(VT_I2);
    iVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ int value) noexcept
{
    ClearIfNot(VT_I4);
    intVal = value;

    return *this;
}

Variant& Variant::operator =(_In_ long value) noexcept
{
    ClearIfNot(VT_I4);
    lVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ long long value) noexcept
{
    ClearIfNot(VT_I8);
    llVal = value;

    return *this;
}

Variant& Variant::operator =(_In_ unsigned char value) noexcept
{
    ClearIfNot(VT_UI1);
    bVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ unsigned short value) noexcept
{
    ClearIfNot(VT_UI2);
    uiVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ unsigned int value) noexcept
{
    ClearIfNot(VT_UI4);
    uintVal= value;
    return *this;
}

Variant& Variant::operator =(_In_ unsigned long value) noexcept
{
    ClearIfNot(VT_UI4);
    ulVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ unsigned long long value) noexcept
{
    ClearIfNot(VT_UI8);
    ullVal = value;

    return *this;
}

Variant& Variant::operator =(_In_ bool value) noexcept
{
    ClearIfNot(VT_BOOL);
    boolVal = value ? VARIANT_TRUE : VARIANT_FALSE;
    return *this;
}

Variant& Variant::operator =(_In_ float value) noexcept
{
    ClearIfNot(VT_R4);
    fltVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ double value) noexcept
{
    ClearIfNot(VT_R8);
    dblVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ short* value) noexcept
{
    ClearIfNot(VT_I2 | VT_BYREF);
    piVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ int* value) noexcept
{
    ClearIfNot(VT_I4 | VT_BYREF);
    pintVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ long* value) noexcept
{
    ClearIfNot(VT_I4 | VT_BYREF);
    plVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ long long* value) noexcept
{
    ClearIfNot(VT_I8 | VT_BYREF);
    pllVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ unsigned char* value) noexcept
{
    ClearIfNot(VT_UI1 | VT_BYREF);
    pbVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ unsigned short* value) noexcept
{
    ClearIfNot(VT_UI2 | VT_BYREF);
    puiVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ unsigned int* value) noexcept
{
    ClearIfNot(VT_UI4 | VT_BYREF);
    puintVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ unsigned long* value) noexcept
{
    ClearIfNot(VT_UI4 | VT_BYREF);
    pulVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ unsigned long long* value) noexcept
{
    ClearIfNot(VT_UI8 | VT_BYREF);
    pullVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ float* value) noexcept
{
    ClearIfNot(VT_R4 | VT_BYREF);
    pfltVal = value;
    return *this;
}

Variant& Variant::operator =(_In_ double* value) noexcept
{
    ClearIfNot(VT_R8 | VT_BYREF);
    pdblVal = value;
    return *this;
}

//Variant& Variant::operator =(_In_z_ char const* str) noexcept
//{
//    ClearThrow();

//    vt = VT_BSTR;
//    bstrVal = ::SysAllocString(...);

//    if (bstrVal == NULL && str != nullptr) {
//        vt = VT_ERROR;
//        scode = E_OUTOFMEMORY;
//    }
//    return *this;
//}

Variant& Variant::operator =(_In_z_ wchar_t const* str) noexcept
{
    if (vt != VT_BSTR || bstrVal != str) {
        ClearThrow();

        vt = VT_BSTR;
        bstrVal = ::SysAllocString(str);

        if (bstrVal == NULL && str != nullptr) {
            vt = VT_ERROR;
            scode = E_OUTOFMEMORY;
        }
    }
    return *this;
}

Variant& Variant::operator =(_In_ CY value) noexcept
{
    ClearIfNot(VT_CY);
    cyVal.Hi = value.Hi;
    cyVal.Lo = value.Lo;
    return *this;
}

Variant& Variant::operator =(_Inout_opt_ IUnknown* value) noexcept
{
    if (vt != VT_UNKNOWN || value != punkVal) {
        ClearThrow();
        vt = VT_UNKNOWN;
        punkVal = value;
        if (punkVal != nullptr)
            punkVal->AddRef();
    }

    return *this;
}

Variant& Variant::operator =(_Inout_opt_ IDispatch* value) noexcept
{
    if (vt != VT_DISPATCH || value != pdispVal) {
        ClearThrow();
        vt = VT_DISPATCH;
        pdispVal = value;
        if (pdispVal != nullptr)
            pdispVal->AddRef();
    }

    return *this;
}

Variant& Variant::operator =(_In_ SAFEARRAY const* value) noexcept
{
    FFMF_ASSERT(value != nullptr);

    if (value == nullptr) {
        ClearThrow();
        vt = VT_ERROR;
        scode = E_INVALIDARG;
        return *this;
    }
    if ((vt & VT_ARRAY) != 0 && value == parray)
        return *this;

    ClearThrow();
    SAFEARRAY* copy = nullptr;
    HRESULT hr = SafeArrayCopy(const_cast<SAFEARRAY*>(value), &copy);
    if (FAILED(hr)) {
        vt = VT_ERROR;
        scode = hr;
        return *this;
    }

    SafeArrayGetVartype(const_cast<SAFEARRAY*>(value), &vt);
    vt |= VT_ARRAY;
    parray = copy;
    return *this;
}

bool Variant::operator ==(_In_ VARIANT const& other) const noexcept
{
    // For backwards compatibility
    if (vt == VT_NULL && other.vt == VT_NULL)
        return true;

    if (vt != other.vt)
        return false;

    return VarCmp(
        const_cast<Variant*>(this),
        const_cast<VARIANT*>(&other),
        LOCALE_USER_DEFAULT,
        0) == static_cast<HRESULT>(VARCMP_EQ);
}

bool Variant::operator <(_In_ VARIANT const& other) const noexcept
{
    if (vt == VT_NULL && other.vt == VT_NULL)
        return false;
    return VarCmp(
        const_cast<Variant*>(this),
        const_cast<VARIANT*>(&other),
        LOCALE_USER_DEFAULT,
        0) == static_cast<HRESULT>(VARCMP_LT);
}

bool Variant::operator >(_In_ VARIANT const& other) const noexcept
{
    if (vt == VT_NULL && other.vt == VT_NULL)
        return false;
    return VarCmp(
        const_cast<Variant*>(this),
        const_cast<VARIANT*>(&other),
        LOCALE_USER_DEFAULT,
        0) == static_cast<HRESULT>(VARCMP_GT);
}

VARTYPE Variant::Type() const noexcept
{
    return vt;
}

bool Variant::Is(VARTYPE type) const noexcept
{
    return vt == type;
}

void Variant::swap(Variant& other) noexcept
{
    std::swap<VARIANT>(*this, other);
}

_Check_return_ HRESULT Variant::Clear()
{
    return ::VariantClear(this);
}

void Variant::ClearIfNot(VARTYPE type)
{
    if (vt != type) {
        ClearThrow();
        vt = type;
    }
}

_Check_return_ HRESULT Variant::CopyFrom(_In_ VARIANT const* source)
{
    return ::VariantCopy(this, const_cast<VARIANT*>(source));
}

_Check_return_ HRESULT Variant::Attach(_In_ VARIANT* source)
{
    FFMF_CheckPointer(source, E_INVALIDARG);

    HRESULT hr = S_OK;
    if (this != source) {
        // Clear out the variant
        ENSURE_HR(hr = Clear());

        // Copy the contents and give control to Variant
        FFMF_Assume(sizeof(Variant) >= sizeof(VARIANT));
        memcpy_s(this, sizeof(Variant), source, sizeof(VARIANT));
        source->vt = VT_EMPTY;
        hr = S_OK;
    }

    return hr;
}

_Check_return_ HRESULT Variant::Detach(_Inout_ VARIANT* dest)
{
    FFMF_ASSERT(dest != nullptr);
    FFMF_CheckPointer(dest, E_POINTER);

    HRESULT hr = ::VariantClear(dest);
    if (SUCCEEDED(hr)) {
        // Copy the contents and remove control from Variant
        memcpy_s(dest, sizeof(VARIANT), this, sizeof(VARIANT));
        vt = VT_EMPTY;
        hr = S_OK;
    }

    return hr;
}

void Variant::ClearThrow() noexcept
{
    HRESULT hr = Clear();
    FFMF_ASSERT(SUCCEEDED(hr));
}

_Check_return_ HRESULT Variant::InternalClear() noexcept
{
    HRESULT hr = Clear();
    if (FAILED(hr)) {
        vt = VT_ERROR;
        scode = hr;
    }

    return hr;
}

void Variant::InternalCopy(_In_ VARIANT const* source) noexcept
{
    HRESULT hr = CopyFrom(source);
    if (FAILED(hr)) {
        vt = VT_ERROR;
        scode = hr;
    }
}

HRESULT Variant::VarCmpEx(
    _In_ LPVARIANT pvarLeft,
    _In_ LPVARIANT pvarRight,
    _In_ LCID lcid,
    _In_ ULONG dwFlags) const noexcept
{
    switch (vt) {
    case VT_I1:
        if (pvarLeft->cVal == pvarRight->cVal)
            return VARCMP_EQ;
        return (pvarLeft->cVal > pvarRight->cVal) ? VARCMP_GT : VARCMP_LT;

    case VT_UI2:
        if (pvarLeft->uiVal == pvarRight->uiVal)
            return VARCMP_EQ;
        return (pvarLeft->uiVal > pvarRight->uiVal) ? VARCMP_GT : VARCMP_LT;

    case VT_UI4:
        if (pvarLeft->uintVal == pvarRight->uintVal)
            return VARCMP_EQ;
        return (pvarLeft->uintVal > pvarRight->uintVal) ? VARCMP_GT : VARCMP_LT;

    case VT_UI8:
        if (pvarLeft->ullVal == pvarRight->ullVal)
            return VARCMP_EQ;
        return (pvarLeft->ullVal > pvarRight->ullVal) ? VARCMP_GT : VARCMP_LT;

    default:
        return ::VarCmp(pvarLeft, pvarRight, lcid, dwFlags);
    }
}

} // namespace ffmf
