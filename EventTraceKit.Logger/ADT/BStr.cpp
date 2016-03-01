#include "BStr.h"

#include "ffmf/Common/Diagnostics/Contracts.h"
#include "ffmf/Common/Debug.h"
#include "ffmf/Common/HResultException.h"
#include "ffmf/Common/Support/ImplicitCast.h"

FFMF_DIAGNOSTIC_PUSH()
FFMF_DIAGNOSTIC_DISABLE_CLANG(invalid-noreturn)
#include "ffmf/Common/SafeInt.h"
FFMF_DIAGNOSTIC_POP()
#include "ffmf/Common/Windows.h"

#include <OleAuto.h>


namespace ffmf
{

BStr::BStr() noexcept
    : bstr(nullptr)
{
}

BStr::~BStr() noexcept
{
    SysFreeString(bstr);
}

//BStr::BStr(std::nullptr_t)
//    : bstr(nullptr)
//{
//}

BStr::BStr(_In_ unsigned int nSize)
{
    if (nSize == 0) {
        bstr = nullptr;
    } else {
        bstr = ::SysAllocStringLen(nullptr, nSize);
        if (!*this)
            throw HResultException(E_OUTOFMEMORY);
    }
}

BStr::BStr(_In_ unsigned int nSize, _In_reads_opt_(nSize) LPCOLESTR sz)
{
    if (nSize == 0) {
        bstr = nullptr;
    } else {
        bstr = ::SysAllocStringLen(sz, nSize);
        if (!*this)
            throw HResultException(E_OUTOFMEMORY);
    }
}

BStr::BStr(_In_opt_z_ LPCOLESTR source)
{
    if (source == nullptr)
        bstr = nullptr;
    else {
        bstr = ::SysAllocString(source);
        if (!*this)
            throw HResultException(E_OUTOFMEMORY);
    }
}

BStr::BStr(_In_ BStr const& source)
{
    bstr = source.Copy();
    if (!!source && !*this) {
        throw HResultException(E_OUTOFMEMORY);
    }
}

//BStr& BStr::operator =(std::nullptr_t)
//{
//    ::SysFreeString(bstr);
//    bstr = nullptr;
//    return *this;
//}

BStr& BStr::operator =(_In_ BStr const& src)
{
    if (bstr != src.bstr) {
        ::SysFreeString(bstr);
        bstr = src.Copy();
        if (!!src && !*this)
            throw HResultException(E_OUTOFMEMORY);
    }
    return *this;
}

BStr& BStr::operator =(_In_opt_z_ LPCOLESTR pSrc)
{
    if (pSrc != bstr) {
        ::SysFreeString(bstr);
        if (pSrc != nullptr) {
            bstr = ::SysAllocString(pSrc);
            if (!*this)
                throw HResultException(E_OUTOFMEMORY);
        } else {
            bstr = nullptr;
        }
    }
    return *this;
}

BStr::BStr(_Inout_ BStr&& source)
    : bstr(source.bstr)
{
    source.bstr = nullptr;
}

BStr& BStr::operator =(_Inout_ BStr&& source)
{
    FFMF_ASSERT(this != &source);
    ::SysFreeString(bstr);

    bstr = source.bstr;
    source.bstr = nullptr;

    return *this;
}

unsigned int BStr::Length() const noexcept
{
    return ::SysStringLen(bstr);
}

unsigned int BStr::ByteLength() const noexcept
{
    return ::SysStringByteLen(bstr);
}

BStr::operator BSTR() const noexcept
{
    return bstr;
}

_Ret_maybenull_z_ BSTR BStr::Copy() const noexcept
{
    if (!*this)
        return nullptr;
    if (bstr != nullptr)
        return ::SysAllocStringByteLen((char*)bstr, ::SysStringByteLen(bstr));
    return ::SysAllocStringByteLen(nullptr, 0);
}

_Check_return_ HRESULT BStr::CopyTo(_Outptr_result_maybenull_ _Result_nullonfailure_ BSTR* pbstr) const noexcept
{
    FFMF_ASSERT(pbstr != nullptr);
    FFMF_CheckPointer(pbstr, E_POINTER);
    *pbstr = Copy();

    if (*pbstr == nullptr && bstr != nullptr)
        return FFMF_TRACE_HR(E_OUTOFMEMORY);
    return S_OK;
}

void BStr::Attach(_In_opt_z_ BSTR src) noexcept
{
    if (bstr != src) {
        ::SysFreeString(bstr);
        bstr = src;
    }
}

_Ret_maybenull_z_ BSTR BStr::Detach() noexcept
{
    BSTR s = bstr;
    bstr = nullptr;
    return s;
}

void BStr::Empty() noexcept
{
    ::SysFreeString(bstr);
    bstr = nullptr;
}

bool BStr::operator !() const noexcept
{
    return (bstr == nullptr);
}

_Check_return_ HRESULT BStr::Append(_In_ BStr const& bstrSrc) noexcept
{
    return AppendBSTR(bstrSrc.bstr);
}

_Check_return_ HRESULT BStr::Append(_In_z_ LPCOLESTR lpsz) noexcept
{
    return Append(lpsz, wcslen(lpsz));
}

// a BSTR is just a LPCOLESTR so we need a special version to signify
// that we are appending a BSTR
_Check_return_ HRESULT BStr::AppendBSTR(_In_opt_z_ BSTR p) noexcept
{
    if (::SysStringLen(p) == 0)
        return S_OK;
    BSTR bstrNew = nullptr;
    HRESULT hr;
    //_Analysis_assume_(p);
    hr = VarBstrCat(bstr, p, &bstrNew);
    if (SUCCEEDED(hr)) {
        ::SysFreeString(bstr);
        bstr = bstrNew;
    }
    return hr;
}

_Check_return_ HRESULT BStr::Append(_In_reads_opt_(nLen) LPCOLESTR lpsz, _In_ size_t nLen)
{
    if (lpsz == nullptr || (bstr != nullptr && nLen == 0))
        return S_OK;

    SafeInt<unsigned int> const n1 = Length();

    SafeInt<unsigned int> nSize = 0;
    SafeInt<unsigned int> nSizeBytes = 0;
    SafeInt<unsigned int> n1Bytes = 0;

    try {
        nSize = n1 + nLen;
        nSizeBytes = nSize * sizeof(OLECHAR);
        n1Bytes = n1 * sizeof(OLECHAR);
    } catch (SafeIntException const&) {
        return HResultFromWin32(ERROR_ARITHMETIC_OVERFLOW);
    }

    BSTR b = ::SysAllocStringLen(nullptr, nSize);
    if (b == nullptr)
        return FFMF_TRACE_HR(E_OUTOFMEMORY);

    if (::SysStringLen(bstr) > 0) {
        //_Analysis_assume_(bstr); // ::SysStringLen(bstr) guarantees that bstr != nullptr
        memcpy_s(b, nSizeBytes, bstr, n1Bytes);
    }

    memcpy_s(b + implicit_cast<unsigned int>(n1), nLen * sizeof(OLECHAR), lpsz, nLen * sizeof(OLECHAR));
    b[nSize] = '\0';
    SysFreeString(bstr);
    bstr = b;
    return S_OK;
}

_Check_return_ HRESULT BStr::Append(_In_ char ch) noexcept
{
    OLECHAR chO = static_cast<unsigned char>(ch);
    return Append(&chO, 1);
}

_Check_return_ HRESULT BStr::Append(_In_ wchar_t ch) noexcept
{
    return Append(&ch, 1);
}

_Check_return_ HRESULT BStr::AppendBytes(
    _In_reads_opt_(nLen) char const* lpsz,
    _In_ size_t nLen)
{
    if (lpsz == nullptr || nLen == 0)
        return S_OK;

    SafeInt<unsigned int> const n1 = ByteLength();
    SafeInt<unsigned int> nSize = 0;
    try {
        nSize = n1 + nLen;
    } catch (SafeIntException const& /*ex*/) {
        return HResultFromWin32(ERROR_ARITHMETIC_OVERFLOW);
    }

    BSTR b = ::SysAllocStringByteLen(nullptr, nSize);
    if (b == nullptr) {
        return FFMF_TRACE_HR(E_OUTOFMEMORY);
    }

    memcpy_s(b, nSize, bstr, n1);
    memcpy_s(((char*)b) + implicit_cast<unsigned int>(n1), nLen, lpsz, nLen);

    *((OLECHAR*)(((char*)b) + implicit_cast<unsigned int>(nSize))) = '\0';
    SysFreeString(bstr);
    bstr = b;
    return S_OK;
}

_Check_return_ HRESULT BStr::AssignBSTR(_In_opt_z_ BSTR const source) noexcept
{
    HRESULT hr = S_OK;
    if (bstr != source) {
        ::SysFreeString(bstr);
        if (source != nullptr) {
            bstr = ::SysAllocStringByteLen((char*)source, ::SysStringByteLen(source));
            if (!*this)
                hr = E_OUTOFMEMORY;
        } else {
            bstr = nullptr;
        }
    }

    return hr;
}

BStr& BStr::operator +=(_In_ BStr const& bstrSrc)
{
    HRESULT hr;
    hr = AppendBSTR(bstrSrc.bstr);
    if (FAILED(hr))
        throw HResultException(hr);
    return *this;
}

BStr& BStr::operator +=(_In_z_ LPCOLESTR pszSrc)
{
    HRESULT hr;
    hr = Append(pszSrc);
    if (FAILED(hr))
        throw HResultException(hr);
    return *this;
}

bool BStr::operator <(_In_ BStr const& other) const noexcept
{
    return VarBstrCmp(bstr, other.bstr, LOCALE_USER_DEFAULT, 0) == static_cast<HRESULT>(VARCMP_LT);
}

bool BStr::operator >(_In_ BStr const& other) const noexcept
{
    return VarBstrCmp(bstr, other.bstr, LOCALE_USER_DEFAULT, 0) == static_cast<HRESULT>(VARCMP_GT);
}

bool BStr::operator <(_In_z_ LPCOLESTR other) const
{
    return operator <(BStr(other));
}

bool BStr::operator >(_In_z_ LPCOLESTR other) const
{
    return operator >(BStr(other));
}

bool BStr::operator <(_In_z_ LPOLESTR other) const
{
    return operator <(const_cast<LPCOLESTR>(other));
}

bool BStr::operator >(_In_z_ LPOLESTR other) const
{
    return operator >(const_cast<LPCOLESTR>(other));
}

//bool BStr::operator <(_In_opt_z_ LPCSTR other) const
//{
//    return operator <(BStr(other));
//}

//bool BStr::operator >(_In_opt_z_ LPCSTR other) const
//{
//    return operator >(BStr(other));
//}

bool BStr::operator ==(_In_ BStr const& other) const noexcept
{
    return VarBstrCmp(bstr, other.bstr, LOCALE_USER_DEFAULT, 0) == static_cast<HRESULT>(VARCMP_EQ);
}

bool BStr::operator ==(std::nullptr_t) const noexcept
{
    return bstr == nullptr;
}

bool BStr::operator ==(_In_z_ LPCOLESTR other) const
{
    return operator ==(BStr(other));
}

bool BStr::operator ==(_In_z_ LPOLESTR other) const
{
    return operator ==(const_cast<LPCOLESTR>(other));
}

//BStr::BStr(_In_opt_z_ LPCSTR pSrc)
//{
//    if (pSrc != nullptr) {
//        bstr = A2WBSTR(pSrc);
//        if (!*this)
//            throw HResultException(E_OUTOFMEMORY);
//    } else {
//        bstr = nullptr;
//    }
//}

//BStr::BStr(_In_ int nSize, _In_reads_opt_(nSize) LPCSTR sz)
//{
//    if (nSize < 0)
//        throw HResultException(E_INVALIDARG);

//    if (nSize != 0 && sz == nullptr) {
//        bstr = ::SysAllocStringLen(nullptr, nSize);
//        if (!*this)
//            throw HResultException(E_OUTOFMEMORY);
//        return;
//    }
//    bstr = A2WBSTR(sz, nSize);
//    if (!*this && nSize != 0)
//        throw HResultException(E_OUTOFMEMORY);
//}

//bool BStr::operator ==(_In_opt_z_ LPCSTR other) const
//{
//    BStr bstr2(pszSrc);
//    return operator ==(bstr2);
//}

//_Check_return_ HRESULT BStr::Append(_In_opt_z_ LPCSTR lpsz) noexcept
//{
//    if (lpsz == nullptr)
//        return S_OK;

//    BStr bstrTemp;
//    try {
//        bstrTemp = lpsz;
//    } catch (...) {
//    }

//    if (!bstrTemp)
//        return FFMF_TRACE_HR(E_OUTOFMEMORY);
//    return Append(bstrTemp);
//}

//BStr& BStr::operator =(_In_opt_z_ LPCSTR pSrc)
//{
//    ::SysFreeString(bstr);
//    bstr = A2WBSTR(pSrc);
//    if (!*this && pSrc != nullptr)
//        throw HResultException(E_OUTOFMEMORY);
//    return *this;
//}


} // namespace ffmf
