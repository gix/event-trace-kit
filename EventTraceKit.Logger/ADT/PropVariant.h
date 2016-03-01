#pragma once
#include "ffmf/Common/ADT/ArrayRef.h"
#include "ffmf/Common/ADT/ComPtr.h"
#include "ffmf/Common/Support/Types.h"

typedef struct IUnknown IUnknown;
#include <PropIdl.h>

namespace ffmf
{

/// <summary>
///   Encapsulates <see cref="PROPVARIANT"/>.
/// </summary>
class PropVariant
{
public:
    PropVariant();
    ~PropVariant();

    PropVariant(int8_t value);
    PropVariant(int16_t value);
    PropVariant(int32_t value);
    PropVariant(int64_t value);
    PropVariant(uint8_t value);
    PropVariant(uint16_t value);
    PropVariant(uint32_t value);
    PropVariant(uint64_t value);
    PropVariant(long value);
    PropVariant(ulong value);
    PropVariant(float value);
    PropVariant(double value);

    HRESULT Clear();

    operator PROPVARIANT const*();
    operator PROPVARIANT const&() const;
    PROPVARIANT const* Get() const;
    PROPVARIANT* Receive();
    PROPVARIANT& operator *();
    PROPVARIANT* operator ->();
    PROPVARIANT const* operator ->() const;

    VARTYPE Type() const;
    bool Is(VARTYPE type) const;

    HRESULT Set(PROPVARIANT const& propVar);
    //HRESULT SetBlob(uint8_t* buffer, size_t bufferSize);

    int8_t   GetInt8OrDefault(int8_t defaultValue = 0) const noexcept;
    int16_t  GetInt16OrDefault(int16_t defaultValue = 0) const noexcept;
    int32_t  GetInt32OrDefault(int32_t defaultValue = 0) const noexcept;
    int64_t  GetInt64OrDefault(int64_t defaultValue = 0) const noexcept;
    uint8_t  GetUInt8OrDefault(uint8_t defaultValue = 0) const noexcept;
    uint16_t GetUInt16OrDefault(uint16_t defaultValue = 0) const noexcept;
    uint32_t GetUInt32OrDefault(uint32_t defaultValue = 0) const noexcept;
    uint64_t GetUInt64OrDefault(uint64_t defaultValue = 0) const noexcept;
    long     GetLongOrDefault(long defaultValue = 0) const noexcept;
    ulong    GetULongOrDefault(ulong defaultValue = 0) const noexcept;
    float    GetSingleOrDefault(float defaultValue = 0) const noexcept;
    double   GetDoubleOrDefault(double defaultValue = 0) const noexcept;

    ArrayRef<int8_t>   GetInt8Array() const noexcept;
    ArrayRef<int16_t>  GetInt16Array() const noexcept;
    ArrayRef<int32_t>  GetInt32Array() const noexcept;
    ArrayRef<int64_t>  GetInt64Array() const noexcept;
    ArrayRef<uint8_t>  GetUInt8Array() const noexcept;
    ArrayRef<uint16_t> GetUInt16Array() const noexcept;
    ArrayRef<uint32_t> GetUInt32Array() const noexcept;
    ArrayRef<uint64_t> GetUInt64Array() const noexcept;
    ArrayRef<long>     GetLongArray() const noexcept;
    ArrayRef<ulong>    GetULongArray() const noexcept;
    ArrayRef<float>    GetSingleArray() const noexcept;
    ArrayRef<double>   GetDoubleArray() const noexcept;

    HRESULT GetInt8(_Out_ int8_t* value) const noexcept;
    HRESULT GetInt16(_Out_ int16_t* value) const noexcept;
    HRESULT GetInt32(_Out_ int32_t* value) const noexcept;
    HRESULT GetInt64(_Out_ int64_t* value) const noexcept;
    HRESULT GetUInt8(_Out_ uint8_t* value) const noexcept;
    HRESULT GetUInt16(_Out_ uint16_t* value) const noexcept;
    HRESULT GetUInt32(_Out_ uint32_t* value) const noexcept;
    HRESULT GetUInt64(_Out_ uint64_t* value) const noexcept;
    HRESULT GetLong(_Out_ long* value) const noexcept;
    HRESULT GetULong(_Out_ ulong* value) const noexcept;
    HRESULT GetSingle(_Out_ float* value) const noexcept;
    HRESULT GetDouble(_Out_ double* value) const noexcept;

    HRESULT SetInt8(int8_t value) noexcept;
    HRESULT SetInt16(int16_t value) noexcept;
    HRESULT SetInt32(int32_t value) noexcept;
    HRESULT SetInt64(int64_t value) noexcept;
    HRESULT SetUInt8(uint8_t value) noexcept;
    HRESULT SetUInt16(uint16_t value) noexcept;
    HRESULT SetUInt32(uint32_t value) noexcept;
    HRESULT SetUInt64(uint64_t value) noexcept;
    HRESULT SetLong(long value) noexcept;
    HRESULT SetULong(ulong value) noexcept;
    HRESULT SetSingle(float value) noexcept;
    HRESULT SetDouble(double value) noexcept;

    HRESULT SetBOOL(BOOL value);
    HRESULT SetGUID(GUID const& guid);
    HRESULT SetString(wchar_t const* string);
    HRESULT SetStringVector(wchar_t const** strings, ulong count);
    HRESULT SetUInt32Vector(ulong const* values, ulong count);

    template<typename T>
    HRESULT GetUnknown(_Outptr_result_maybenull_ T** ppv) const noexcept
    {
        return GetUnknown(__uuidof(T), reinterpret_cast<void**>(ppv));
    }

    template<typename T>
    HRESULT GetUnknown(ComPtrRef<T> ppv) const noexcept
    {
        return GetUnknown(__uuidof(T), ppv);
    }

    HRESULT GetUnknown(REFIID iid, _Outptr_result_maybenull_ void** ppv) const noexcept;
    HRESULT SetUnknown(_In_ IUnknown* ptr) noexcept;

    HRESULT CopyTo(PROPVARIANT& propVar) const noexcept;
    HRESULT CopyTo(PropVariant& propVar) const noexcept;

private:
    PropVariant(PropVariant const&);
    PropVariant& operator =(PropVariant const&);
    PROPVARIANT propVar;
};

inline
int8_t PropVariant::GetInt8OrDefault(int8_t defaultValue /*= 0*/) const noexcept
{
    return Type() == VT_I1 ? propVar.cVal : defaultValue;
}

inline
int16_t PropVariant::GetInt16OrDefault(int16_t defaultValue /*= 0*/) const noexcept
{
    return Type() == VT_I2 ? propVar.iVal : defaultValue;
}

inline
int32_t PropVariant::GetInt32OrDefault(int32_t defaultValue /*= 0*/) const noexcept
{
    return Type() == VT_INT ? propVar.bVal : defaultValue;
}

inline
int64_t PropVariant::GetInt64OrDefault(int64_t defaultValue /*= 0*/) const noexcept
{
    return Type() == VT_I8 ? propVar.hVal.QuadPart : defaultValue;
}

inline
uint8_t PropVariant::GetUInt8OrDefault(uint8_t defaultValue /*= 0*/) const noexcept
{
    return Type() == VT_UI1 ? propVar.bVal : defaultValue;
}

inline
uint16_t PropVariant::GetUInt16OrDefault(uint16_t defaultValue /*= 0*/) const noexcept
{
    return Type() == VT_UI2 ? propVar.uiVal : defaultValue;
}

inline
uint32_t PropVariant::GetUInt32OrDefault(uint32_t defaultValue /*= 0*/) const noexcept
{
    return Type() == VT_UINT ? propVar.uintVal : defaultValue;
}

inline
uint64_t PropVariant::GetUInt64OrDefault(uint64_t defaultValue /*= 0*/) const noexcept
{
    return Type() == VT_UI8 ? propVar.uhVal.QuadPart : defaultValue;
}

inline
long PropVariant::GetLongOrDefault(long defaultValue /*= 0*/) const noexcept
{
    return Type() == VT_I4 ? propVar.lVal : defaultValue;
}

inline
ulong PropVariant::GetULongOrDefault(ulong defaultValue /*= 0*/) const noexcept
{
    return Type() == VT_UI4 ? propVar.ulVal : defaultValue;
}

inline
float PropVariant::GetSingleOrDefault(float defaultValue /*= 0*/) const noexcept
{
    return Type() == VT_R4 ? propVar.fltVal : defaultValue;
}

inline
double PropVariant::GetDoubleOrDefault(double defaultValue /*= 0*/) const noexcept
{
    return Type() == VT_R8 ? propVar.dblVal : defaultValue;
}

inline
ArrayRef<int8_t> PropVariant::GetInt8Array() const noexcept
{
    if (Type() == (VT_I1 | VT_VECTOR) && propVar.cac.pElems)
        return { reinterpret_cast<signed char*>(propVar.cac.pElems),
                 static_cast<size_t>(propVar.cac.cElems) };
    return {};
}

inline
ArrayRef<int16_t> PropVariant::GetInt16Array() const noexcept
{
    if (Type() == (VT_I2 | VT_VECTOR) && propVar.cai.pElems)
        return { propVar.cai.pElems, propVar.cai.cElems };
    return {};
}

inline
ArrayRef<int32_t> PropVariant::GetInt32Array() const noexcept
{
    static_assert(sizeof(int32_t) == sizeof(long), "int32_t not compatible with long");
    if (Type() == (VT_INT | VT_VECTOR) && propVar.cal.pElems)
        return { reinterpret_cast<int32_t*>(propVar.cal.pElems), propVar.cal.cElems };
    return {};
}

inline
ArrayRef<int64_t> PropVariant::GetInt64Array() const noexcept
{
    static_assert(sizeof(int64_t) == sizeof(LARGE_INTEGER), "int64_t not compatible with LARGE_INTEGER");
    if (Type() == (VT_I8 | VT_VECTOR) && propVar.cah.pElems)
        return { reinterpret_cast<int64_t*>(propVar.cah.pElems), propVar.cah.cElems };
    return {};
}

inline
ArrayRef<uint8_t> PropVariant::GetUInt8Array() const noexcept
{
    if (Type() == (VT_UI1 | VT_VECTOR) && propVar.caub.pElems)
        return { propVar.caub.pElems, propVar.caub.cElems };
    return {};
}

inline
ArrayRef<uint16_t> PropVariant::GetUInt16Array() const noexcept
{
    if (Type() == (VT_UI2 | VT_VECTOR) && propVar.caui.pElems)
        return { propVar.caui.pElems, propVar.caui.cElems };
    return {};
}

inline
ArrayRef<uint32_t> PropVariant::GetUInt32Array() const noexcept
{
    static_assert(sizeof(uint32_t) == sizeof(unsigned long), "uint32_t not compatible with unsigned long");
    if (Type() == (VT_UINT | VT_VECTOR) && propVar.caul.pElems)
        return { reinterpret_cast<uint32_t*>(propVar.caul.pElems), propVar.caul.cElems };
    return {};
}

inline
ArrayRef<uint64_t> PropVariant::GetUInt64Array() const noexcept
{
    static_assert(sizeof(uint64_t) == sizeof(ULARGE_INTEGER), "uint64_t not compatible with ULARGE_INTEGER");
    if (Type() == (VT_UI8 | VT_VECTOR) && propVar.cauh.pElems)
        return { reinterpret_cast<uint64_t*>(propVar.cauh.pElems), propVar.cauh.cElems };
    return {};
}

inline
ArrayRef<long> PropVariant::GetLongArray() const noexcept
{
    if (Type() == (VT_I4 | VT_VECTOR) && propVar.cal.pElems)
        return { propVar.cal.pElems, propVar.cal.cElems };
    return {};
}

inline
ArrayRef<ulong> PropVariant::GetULongArray() const noexcept
{
    if (Type() == (VT_UI4 | VT_VECTOR) && propVar.caul.pElems)
        return { propVar.caul.pElems, propVar.caul.cElems };
    return {};
}

inline
ArrayRef<float> PropVariant::GetSingleArray() const noexcept
{
    if (Type() == (VT_R4 | VT_VECTOR) && propVar.caflt.pElems)
        return { propVar.caflt.pElems, propVar.caflt.cElems };
    return {};
}

inline
ArrayRef<double> PropVariant::GetDoubleArray() const noexcept
{
    if (Type() == (VT_R8 | VT_VECTOR) && propVar.cadbl.pElems)
        return { propVar.cadbl.pElems, propVar.cadbl.cElems };
    return {};
}

} // namespace ffmf
