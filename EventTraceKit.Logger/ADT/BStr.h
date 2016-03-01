#pragma once
#include "ffmf/Common/Support/CompilerSupport.h"
#include "ffmf/Common/Support/Types.h"
#include <cstddef>
#include <WTypes.h>

namespace ffmf
{

/// <summary>
///   Encapsulates a <see cref="BSTR"/>.
/// </summary>
class BStr
{
public:
    BStr() noexcept;
    ~BStr() noexcept;

    BStr(_In_ BStr const& source);
    BStr(_Inout_ BStr&& source);

    explicit BStr(_In_ unsigned int nSize);
    BStr(_In_opt_z_ LPCOLESTR pSrc);
    BStr(_In_ unsigned int nSize, _In_reads_opt_(nSize) LPCOLESTR sz);
    BStr(_In_opt_z_ LPCSTR pSrc);
    BStr(_In_ unsigned int nSize, _In_reads_opt_(nSize) LPCSTR sz);

    unsigned int Length() const noexcept;
    unsigned int ByteLength() const noexcept;
    operator BSTR() const noexcept;

    BStr& operator =(_In_ BStr const& source);
    BStr& operator =(_Inout_ BStr&& source);
    BStr& operator =(_In_opt_z_ LPCOLESTR pSrc);
    BStr& operator =(_In_opt_z_ LPCSTR pSrc);

    bool operator ==(_In_ BStr const& other) const noexcept;
    bool operator !=(_In_ BStr const& other) const noexcept;

    bool operator ==(std::nullptr_t) const noexcept;
    bool operator !=(std::nullptr_t) const noexcept;

    bool operator ==(_In_z_ LPCOLESTR other) const;
    bool operator !=(_In_z_ LPCOLESTR other) const;

    bool operator ==(_In_z_ LPOLESTR other) const;
    bool operator !=(_In_z_ LPOLESTR other) const;

    //bool operator ==(_In_opt_z_ LPCSTR pszSrc) const;
    //bool operator !=(_In_opt_z_ LPCSTR pszSrc) const;

    bool operator <(_In_ BStr const& other) const noexcept;
    bool operator >(_In_ BStr const& other) const noexcept;

    bool operator <(_In_z_ LPCOLESTR other) const;
    bool operator >(_In_z_ LPCOLESTR other) const;

    bool operator <(_In_z_ LPOLESTR other) const;
    bool operator >(_In_z_ LPOLESTR other) const;

    //bool operator <(_In_opt_z_ LPCSTR other) const;
    //bool operator >(_In_opt_z_ LPCSTR other) const;

    bool operator!() const noexcept;

    _Ret_maybenull_z_ BSTR Copy() const noexcept;
    _Check_return_ HRESULT CopyTo(_Outptr_result_maybenull_ _Result_nullonfailure_ BSTR* pbstr) const noexcept;

    void Attach(_In_opt_z_ BSTR src) noexcept;
    _Ret_maybenull_z_ BSTR Detach() noexcept;

    void Empty() noexcept;

    _Check_return_ HRESULT Append(_In_ BStr const& str) noexcept;
    _Check_return_ HRESULT Append(_In_z_ LPCOLESTR str) noexcept;
    //_Check_return_ HRESULT Append(_In_opt_z_ LPCSTR str) noexcept;
    _Check_return_ HRESULT Append(_In_reads_opt_(nLen) LPCOLESTR lpsz, _In_ size_t nLen);
    _Check_return_ HRESULT Append(_In_ char ch) noexcept;
    _Check_return_ HRESULT Append(_In_ wchar_t ch) noexcept;
    _Check_return_ HRESULT AppendBytes(_In_reads_opt_(nLen) char const* lpsz, _In_ size_t nLen);

    // a BSTR is just a LPCOLESTR so we need a special version to signify
    // that we are appending a BSTR
    _Check_return_ HRESULT AppendBSTR(_In_opt_z_ BSTR p) noexcept;

    _Check_return_ HRESULT AssignBSTR(_In_opt_z_ BSTR const bstrSrc) noexcept;

    BStr& operator +=(_In_ BStr const& str);
    BStr& operator +=(_In_z_ LPCOLESTR str);

private:
    BSTR bstr;
};

inline bool BStr::operator !=(_In_ BStr const& other) const noexcept
{
    return !operator==(other);
}

inline bool BStr::operator !=(std::nullptr_t) const noexcept
{
    return bstr != nullptr;
}

inline bool BStr::operator !=(_In_z_ LPCOLESTR other) const
{
    return !operator==(other);
}

inline bool BStr::operator !=(_In_z_ LPOLESTR other) const
{
    return operator !=(const_cast<LPCOLESTR>(other));
}

//inline bool BStr::operator !=(_In_opt_z_ LPCSTR other) const
//{
//    return !operator==(other);
//}

} // namespace ffmf
