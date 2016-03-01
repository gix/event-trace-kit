#pragma once
#ifdef __clang__
#define _FORCENAMELESSUNION
#endif
#include "ffmf/Common/Support/CompilerSupport.h"
#include "ffmf/Common/Support/Types.h"
#include <Unknwn.h>
#include <oaidl.h>

namespace ffmf
{

/// <summary>
///   Provides a safe wrapper for the <see cref="VARIANT"/> type.
/// </summary>
class Variant
    : public tagVARIANT
{
public:
    Variant() noexcept;
    ~Variant() noexcept;

    Variant(_In_ Variant const& source) noexcept;
    Variant(_In_ Variant&& source) noexcept;
    Variant(_In_ VARIANT const& source) noexcept;

    Variant(_In_ char value) noexcept;
    Variant(_In_ short value) noexcept;
    Variant(_In_ int value, _In_ VARTYPE type = VT_I4) noexcept;
    Variant(_In_ long value, _In_ VARTYPE type = VT_I4) noexcept;
    Variant(_In_ long long value) noexcept;
    Variant(_In_ unsigned char value) noexcept;
    Variant(_In_ unsigned short value) noexcept;
    Variant(_In_ unsigned int value, _In_ VARTYPE type = VT_UI4) noexcept;
    Variant(_In_ unsigned long value) noexcept;
    Variant(_In_ unsigned long long value) noexcept;
    Variant(_In_ bool value) noexcept;
    Variant(_In_ float value) noexcept;
    Variant(_In_ double value, _In_ VARTYPE type = VT_R8) noexcept;

    //Variant(_In_z_ char const* str);
    Variant(_In_z_ wchar_t const* str) noexcept;
    Variant(_In_opt_ IUnknown* value) noexcept;
    Variant(_In_opt_ IDispatch* value) noexcept;
    Variant(_In_ CY value) noexcept;
    Variant(_In_ SAFEARRAY const* value) noexcept;

    Variant& operator =(_In_ Variant source) noexcept;
    Variant& operator =(_In_ VARIANT const& source) noexcept;

    Variant& operator =(_In_ char value) noexcept;
    Variant& operator =(_In_ short value) noexcept;
    Variant& operator =(_In_ int value) noexcept;
    Variant& operator =(_In_ long value) noexcept;
    Variant& operator =(_In_ long long value) noexcept;
    Variant& operator =(_In_ unsigned char value) noexcept;
    Variant& operator =(_In_ unsigned short value) noexcept;
    Variant& operator =(_In_ unsigned int value) noexcept;
    Variant& operator =(_In_ unsigned long value) noexcept;
    Variant& operator =(_In_ unsigned long long value) noexcept;
    Variant& operator =(_In_ bool value) noexcept;
    Variant& operator =(_In_ float value) noexcept;
    Variant& operator =(_In_ double value) noexcept;

    Variant& operator =(_In_ short* value) noexcept;
    Variant& operator =(_In_ int* value) noexcept;
    Variant& operator =(_In_ long* value) noexcept;
    Variant& operator =(_In_ long long* value) noexcept;
    Variant& operator =(_In_ unsigned char* value) noexcept;
    Variant& operator =(_In_ unsigned short* value) noexcept;
    Variant& operator =(_In_ unsigned int* value) noexcept;
    Variant& operator =(_In_ unsigned long* value) noexcept;
    Variant& operator =(_In_ unsigned long long* value) noexcept;
    Variant& operator =(_In_ float* value) noexcept;
    Variant& operator =(_In_ double* value) noexcept;

    //Variant& operator =(_In_z_ char const* str) noexcept;
    Variant& operator =(_In_z_ wchar_t const* str) noexcept;
    Variant& operator =(_Inout_opt_ IUnknown* value) noexcept;
    Variant& operator =(_Inout_opt_ IDispatch* value) noexcept;
    Variant& operator =(_In_ CY value) noexcept;
    Variant& operator =(_In_ SAFEARRAY const* value) noexcept;

    bool operator ==(_In_ VARIANT const& other) const noexcept;
    bool operator !=(_In_ VARIANT const& other) const noexcept;

    bool operator <(_In_ VARIANT const& other) const noexcept;
    bool operator >(_In_ VARIANT const& other) const noexcept;

    VARTYPE Type() const noexcept;
    bool Is(VARTYPE type) const noexcept;
    void swap(Variant& other) noexcept;

    _Check_return_ HRESULT Clear();
    _Check_return_ HRESULT CopyFrom(_In_ VARIANT const* pSrc);

    _Check_return_ HRESULT Attach(_In_ VARIANT* source);
    _Check_return_ HRESULT Detach(_Inout_ VARIANT* dest);

private:
    void ClearIfNot(VARTYPE type);
    void ClearThrow() noexcept;
    _Check_return_ HRESULT InternalClear() noexcept;
    void InternalCopy(_In_ VARIANT const* pSrc) noexcept;

    inline HRESULT VarCmpEx(
        _In_ LPVARIANT pvarLeft,
        _In_ LPVARIANT pvarRight,
        _In_ LCID lcid,
        _In_ ULONG dwFlags) const noexcept;
};

inline bool Variant::operator !=(_In_ VARIANT const& other) const noexcept
{
    return !operator==(other);
}

} // namespace ffmf
