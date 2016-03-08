#include "PropVariant.h"

#include "ffmf/Common/Diagnostics/Contracts.h"
#include "ffmf/Common/Debug.h"
#include "ffmf/Common/Support/CompilerConfig.h"

#include <shlwapi.h>
#include <propvarutil.h>

#if defined(FFMF_CLANG) && defined(FFMF_X64)
#undef STDAPICALLTYPE
#define STDAPICALLTYPE
#endif

#define PSSTDAPI        EXTERN_C DECLSPEC_IMPORT HRESULT STDAPICALLTYPE
#define PSSTDAPI_(type) EXTERN_C DECLSPEC_IMPORT type STDAPICALLTYPE

PSSTDAPI InitPropVariantFromResource(_In_ HINSTANCE hinst, _In_ UINT id, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromBuffer(_In_reads_bytes_(cb) void const* pv, _In_ UINT cb, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromCLSID(_In_ REFCLSID clsid, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromGUIDAsString(_In_ REFGUID guid, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromFileTime(_In_ FILETIME const* pftIn, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromPropVariantVectorElem(_In_ REFPROPVARIANT propvarIn, _In_ ULONG iElem, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantVectorFromPropVariant(_In_ REFPROPVARIANT propvarSingle, _Out_ PROPVARIANT* ppropvarVector);
PSSTDAPI InitPropVariantFromBooleanVector(_In_reads_opt_(cElems) BOOL const* prgf, _In_ ULONG cElems, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromInt16Vector(_In_reads_opt_(cElems) SHORT const* prgn, _In_ ULONG cElems, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromUInt16Vector(_In_reads_opt_(cElems) USHORT const* prgn, _In_ ULONG cElems, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromInt32Vector(_In_reads_opt_(cElems) LONG const* prgn, _In_ ULONG cElems, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromUInt32Vector(_In_reads_opt_(cElems) ULONG const* prgn, _In_ ULONG cElems, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromInt64Vector(_In_reads_opt_(cElems) LONGLONG const* prgn, _In_ ULONG cElems, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromUInt64Vector(_In_reads_opt_(cElems) ULONGLONG const* prgn, _In_ ULONG cElems, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromDoubleVector(_In_reads_opt_(cElems) DOUBLE const* prgn, _In_ ULONG cElems, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromFileTimeVector(_In_reads_opt_(cElems) FILETIME const* prgft, _In_ ULONG cElems, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromStringVector(_In_reads_opt_(cElems) PCWSTR* prgsz, _In_ ULONG cElems, _Out_ PROPVARIANT* ppropvar);
PSSTDAPI InitPropVariantFromStringAsVector(_In_opt_ PCWSTR psz, _Out_ PROPVARIANT* ppropvar);


namespace ffmf
{

PropVariant::PropVariant()
{
    PropVariantInit(&propVar);
}

PropVariant::~PropVariant()
{
    Clear();
}

PropVariant::PropVariant(int8_t value)
{
    PropVariantInit(&propVar);
    propVar.vt = VT_I1;
    propVar.cVal = value;
}

PropVariant::PropVariant(int16_t value)
{
    PropVariantInit(&propVar);
    propVar.vt = VT_I2;
    propVar.iVal = value;
}

PropVariant::PropVariant(int32_t value)
{
    PropVariantInit(&propVar);
    propVar.vt = VT_INT;
    propVar.intVal = value;
}

PropVariant::PropVariant(int64_t value)
{
    PropVariantInit(&propVar);
    propVar.vt = VT_I8;
    propVar.hVal.QuadPart = value;
}

PropVariant::PropVariant(uint8_t value)
{
    PropVariantInit(&propVar);
    propVar.vt = VT_UI1;
    propVar.bVal = value;
}

PropVariant::PropVariant(uint16_t value)
{
    PropVariantInit(&propVar);
    propVar.vt = VT_UI2;
    propVar.uiVal = value;
}

PropVariant::PropVariant(uint32_t value)
{
    PropVariantInit(&propVar);
    propVar.vt = VT_UINT;
    propVar.uintVal = value;
}

PropVariant::PropVariant(uint64_t value)
{
    PropVariantInit(&propVar);
    propVar.vt = VT_UI8;
    propVar.uhVal.QuadPart = value;
}

PropVariant::PropVariant(long value)
{
    PropVariantInit(&propVar);
    propVar.vt = VT_I4;
    propVar.lVal = value;
}

PropVariant::PropVariant(ulong value)
{
    PropVariantInit(&propVar);
    propVar.vt = VT_UI4;
    propVar.ulVal = value;
}

PropVariant::PropVariant(float value)
{
    PropVariantInit(&propVar);
    propVar.vt = VT_R4;
    propVar.fltVal = value;
}

PropVariant::PropVariant(double value)
{
    PropVariantInit(&propVar);
    propVar.vt = VT_R8;
    propVar.dblVal = value;
}

HRESULT PropVariant::Clear()
{
    return PropVariantClear(&propVar);
}

PropVariant::operator PROPVARIANT const*()
{
    return &propVar;
}

PropVariant::operator PROPVARIANT const&() const
{
    return propVar;
}

PROPVARIANT const* PropVariant::Get() const
{
    return &propVar;
}

PROPVARIANT* PropVariant::Receive()
{
    Clear();
    return &propVar;
}

PROPVARIANT& PropVariant::operator *()
{
    return propVar;
}

PROPVARIANT* PropVariant::operator ->()
{
    return &propVar;
}

PROPVARIANT const* PropVariant::operator ->() const
{
    return &propVar;
}

VARTYPE PropVariant::Type() const
{
    return propVar.vt;
}

bool PropVariant::Is(VARTYPE type) const
{
    return propVar.vt == type;
}

HRESULT PropVariant::Set(PROPVARIANT const& propVar)
{
    Clear();
    return PropVariantCopy(&this->propVar, &propVar);
}

//HRESULT PropVariant::SetBlob(uint8_t* buffer, size_t bufferSize)
//{
//    Clear();
//    uint8_t* pb = static_cast<uint8_t*>(CoTaskMemAlloc(bufferSize));
//    if (pb == nullptr)
//        return FFMF_TRACE_HR(E_OUTOFMEMORY);
//
//    propVar.blob.pBlobData = pb;
//    propVar.blob.cbSize = bufferSize;
//    memcpy(propVar.blob.pBlobData, buffer, bufferSize);
//    return S_OK;
//}

HRESULT PropVariant::GetInt8(_Out_ int8_t* value) const noexcept
{
    FFMF_Requires(Type() == VT_I1, E_FAIL);
    *value = propVar.cVal;
    return S_OK;
}

HRESULT PropVariant::GetInt16(_Out_ int16_t* value) const noexcept
{
    FFMF_Requires(Type() == VT_I2, E_FAIL);
    *value = propVar.iVal;
    return S_OK;
}

HRESULT PropVariant::GetInt32(_Out_ int32_t* value) const noexcept
{
    FFMF_Requires(Type() == VT_INT, E_FAIL);
    *value = propVar.intVal;
    return S_OK;
}

HRESULT PropVariant::GetInt64(_Out_ int64_t* value) const noexcept
{
    FFMF_Requires(Type() == VT_I8, E_FAIL);
    *value = propVar.hVal.QuadPart;
    return S_OK;
}

HRESULT PropVariant::GetUInt8(_Out_ uint8_t* value) const noexcept
{
    FFMF_Requires(Type() == VT_UI1, E_FAIL);
    *value = propVar.bVal;
    return S_OK;
}

HRESULT PropVariant::GetUInt16(_Out_ uint16_t* value) const noexcept
{
    FFMF_Requires(Type() == VT_UI2, E_FAIL);
    *value = propVar.uiVal;
    return S_OK;
}

HRESULT PropVariant::GetUInt32(_Out_ uint32_t* value) const noexcept
{
    FFMF_Requires(Type() == VT_UINT, E_FAIL);
    *value = propVar.uintVal;
    return S_OK;
}

HRESULT PropVariant::GetUInt64(_Out_ uint64_t* value) const noexcept
{
    FFMF_Requires(Type() == VT_UI8, E_FAIL);
    *value = propVar.uhVal.QuadPart;
    return S_OK;
}

HRESULT PropVariant::GetLong(_Out_ long* value) const noexcept
{
    FFMF_Requires(Type() == VT_I4, E_FAIL);
    *value = propVar.lVal;
    return S_OK;
}

HRESULT PropVariant::GetULong(_Out_ ulong* value) const noexcept
{
    FFMF_Requires(Type() == VT_UI4, E_FAIL);
    *value = propVar.ulVal;
    return S_OK;
}

HRESULT PropVariant::GetSingle(_Out_ float* value) const noexcept
{
    FFMF_Requires(Type() == VT_R4, E_FAIL);
    *value = propVar.fltVal;
    return S_OK;
}

HRESULT PropVariant::GetDouble(_Out_ double* value) const noexcept
{
    FFMF_Requires(Type() == VT_R8, E_FAIL);
    *value = propVar.dblVal;
    return S_OK;
}

HRESULT PropVariant::SetInt8(int8_t value) noexcept
{
    Clear();
    propVar.vt = VT_I1;
    propVar.cVal = value;
    return S_OK;
}

HRESULT PropVariant::SetInt16(int16_t value) noexcept
{
    Clear();
    propVar.vt = VT_I2;
    propVar.iVal = value;
    return S_OK;
}

HRESULT PropVariant::SetInt32(int32_t value) noexcept
{
    Clear();
    propVar.vt = VT_INT;
    propVar.intVal = value;
    return S_OK;
}

HRESULT PropVariant::SetInt64(int64_t value) noexcept
{
    Clear();
    propVar.vt = VT_I8;
    propVar.hVal.QuadPart = value;
    return S_OK;
}

HRESULT PropVariant::SetUInt8(uint8_t value) noexcept
{
    Clear();
    propVar.vt = VT_UI1;
    propVar.bVal = value;
    return S_OK;
}

HRESULT PropVariant::SetUInt16(uint16_t value) noexcept
{
    Clear();
    propVar.vt = VT_UI2;
    propVar.uiVal = value;
    return S_OK;
}

HRESULT PropVariant::SetUInt32(uint32_t value) noexcept
{
    Clear();
    propVar.vt = VT_UINT;
    propVar.uintVal = value;
    return S_OK;
}

HRESULT PropVariant::SetUInt64(uint64_t value) noexcept
{
    Clear();
    propVar.vt = VT_UI8;
    propVar.uhVal.QuadPart = value;
    return S_OK;
}

HRESULT PropVariant::SetLong(long value) noexcept
{
    Clear();
    propVar.vt = VT_I4;
    propVar.lVal = value;
    return S_OK;
}

HRESULT PropVariant::SetULong(ulong value) noexcept
{
    Clear();
    propVar.vt = VT_UI4;
    propVar.ulVal = value;
    return S_OK;
}

HRESULT PropVariant::SetSingle(float value) noexcept
{
    Clear();
    propVar.vt = VT_R4;
    propVar.fltVal = value;
    return S_OK;
}

HRESULT PropVariant::SetDouble(double value) noexcept
{
    Clear();
    propVar.vt = VT_R8;
    propVar.dblVal = value;
    return S_OK;
}

HRESULT PropVariant::SetBOOL(BOOL value)
{
    Clear();
    propVar.vt = VT_BOOL;
    propVar.boolVal = (value != 0) ? VARIANT_TRUE : VARIANT_FALSE;
    return S_OK;
}

HRESULT PropVariant::SetGUID(GUID const& guid)
{
    Clear();
    return InitPropVariantFromCLSID(guid, &propVar);
}

HRESULT PropVariant::SetString(wchar_t const* string)
{
    Clear();
    return InitPropVariantFromString(string, &propVar);
}

HRESULT PropVariant::SetStringVector(wchar_t const** strings, ulong count)
{
    Clear();
    return InitPropVariantFromStringVector(strings, count, &propVar);
}

HRESULT PropVariant::SetUInt32Vector(ulong const* values, ulong count)
{
    Clear();
    return InitPropVariantFromUInt32Vector(values, count, &propVar);
}

HRESULT PropVariant::GetUnknown(
    REFIID iid, _Outptr_result_maybenull_ void** ppv) const noexcept
{
    FFMF_ASSERT(Type() == VT_UNKNOWN);
    return propVar.punkVal->QueryInterface(iid, ppv);
}

HRESULT PropVariant::SetUnknown(_In_ IUnknown* ptr) noexcept
{
    FFMF_CheckPointer(ptr, E_INVALIDARG);

    Clear();
    propVar.vt = VT_UNKNOWN;
    propVar.punkVal = ptr;
    propVar.punkVal->AddRef();
    return S_OK;
}

HRESULT PropVariant::CopyTo(PROPVARIANT& propVar) const noexcept
{
    PropVariantClear(&propVar);
    return PropVariantCopy(&propVar, &this->propVar);
}

HRESULT PropVariant::CopyTo(PropVariant& propVar) const noexcept
{
    PropVariantClear(&propVar.propVar);
    return PropVariantCopy(&propVar.propVar, &this->propVar);
}

} // namespace ffmf
