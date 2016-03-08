#pragma once
#include "ffmf/Common/Support/Types.h"
#include "ffmf/Common/Windows.h"

#include <string>
#include <vector>

namespace ffmf
{

enum class RegistryValueKind
{
    None = -1,
    Unknown = 0,
    String = 1,
    ExpandString = 2,
    Binary = 3,
    DWord = 4,
    Link = 6,
    MultiString = 7,
    ResourceList = 8,
    ResourceDescriptor = 9,
    ResourceRequirementsList = 10,
    QWord = 11,
};

class RegKey
{
public:
    RegKey();
    ~RegKey();

    RegKey(RegKey&& source);
    RegKey& operator =(RegKey&& source);

    static HRESULT CreateKey(
        HKEY parentKey, wchar_t const* subKey, _Out_ RegKey* key);

    static HRESULT CreateKey(
        HKEY parentKey, wchar_t const* subKey, DWORD options, REGSAM samDesired,
        _Out_ RegKey* key);

    static HRESULT OpenKey(
        HKEY parentKey, wchar_t const* subKey, _Out_ RegKey* key);

    static HRESULT OpenKey(
        HKEY parentKey, wchar_t const* subKey, bool writable, _Out_ RegKey* key);

    static HRESULT OpenKey(
        HKEY parentKey, wchar_t const* subKey, DWORD options, REGSAM samDesired,
        _Out_ RegKey* key);

    bool IsValid() const;
    HKEY Get() const;
    HRESULT Close();

    HRESULT CreateSubKey(wchar_t const* subKeyName, _Out_ RegKey* subKey);

    HRESULT CreateSubKey(wchar_t const* subKeyName, DWORD options,
                         REGSAM samDesired, _Out_ RegKey* subKey);

    HRESULT DeleteSubKey(wchar_t const* subKeyName);
    HRESULT DeleteSubKey(wchar_t const* subKeyName, bool ignoreMissing);

    HRESULT GetSubKeyCount(_Out_ unsigned* pSubKeyCount) const;
    HRESULT GetValueCount(_Out_ unsigned* pValueCount) const;

    RegistryValueKind GetValueKind(wchar_t const* name);
    HRESULT GetValue(wchar_t const* name, DWORD* pValue);
    HRESULT GetValue(wchar_t const* name, uint32_t* pValue);
    HRESULT GetValue(wchar_t const* name, uint64_t* pValue);
    HRESULT GetValue(wchar_t const* name, std::wstring& str);
    HRESULT GetValue(wchar_t const* name, std::vector<uint8_t>& buffer);

    HRESULT GetDefaultValue(DWORD* pValue);
    HRESULT GetDefaultValue(uint32_t* pValue);
    HRESULT GetDefaultValue(uint64_t* pValue);
    HRESULT GetDefaultValue(std::wstring& str);
    HRESULT GetDefaultValue(std::vector<uint8_t>& buffer);

    void* InternalGetValue(wchar_t const* name, void* defaultValue, bool doNotExpand, bool checkSecurity);

    HRESULT SetDefaultValue(wchar_t const* value);
    HRESULT SetValue(wchar_t const* valueName, wchar_t const* value);

    HRESULT ForceDeleteDefaultValue();
    HRESULT DeleteDefaultValue();
    HRESULT ForceDeleteValue(wchar_t const* valueName);
    HRESULT DeleteValue(wchar_t const* valueName);

private:
    RegKey(RegKey const&);
    RegKey& operator =(RegKey const&);

    RegKey(HKEY hkey);

    HKEY hkey;
};

} // namespace ffmf
