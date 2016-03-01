#include "RegKey.h"

#include "ffmf/Common/Diagnostics/Contracts.h"
#include "ffmf/Common/Debug.h"
#include "ffmf/Common/ErrorHandling.h"
#include "ffmf/Common/Support/ImplicitCast.h"
#include "ffmf/Common/Support/Types.h"

#include <utility>
#include <strsafe.h>

namespace ffmf
{

RegKey::RegKey()
    : hkey(nullptr)
{
}

RegKey::RegKey(HKEY hkey)
    : hkey(hkey)
{
}

RegKey::RegKey(RegKey&& source)
    : hkey(std::exchange(source.hkey, nullptr))
{
}

RegKey::~RegKey()
{
    Close();
}

RegKey& RegKey::operator =(RegKey&& source)
{
    FFMF_ASSERT(this != &source);
    Close();
    hkey = source.hkey;
    source.hkey = nullptr;
    return *this;
}

HRESULT RegKey::CreateKey(HKEY parentKey, wchar_t const* subKey, _Out_ RegKey* key)
{
    return CreateKey(parentKey, subKey, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, key);
}

HRESULT RegKey::CreateKey(HKEY parentKey, wchar_t const* subKey, DWORD options,
                          REGSAM samDesired, _Out_ RegKey* key)
{
    FFMF_CheckPointer(key, E_POINTER);

    HKEY hkey;
    HRESULT hr = HResultFromLSTATUS(RegCreateKeyExW(
        parentKey, subKey, 0, nullptr, options, samDesired, nullptr, &hkey, nullptr));

    if (SUCCEEDED(hr))
        *key = RegKey(hkey);

    return hr;
}

HRESULT RegKey::OpenKey(HKEY parentKey, wchar_t const* subKey, _Out_ RegKey* key)
{
    return OpenKey(parentKey, subKey, 0, implicit_cast<REGSAM>(KEY_READ), key);
}

HRESULT RegKey::OpenKey(
    HKEY parentKey, wchar_t const* subKey, bool writable, _Out_ RegKey* key)
{
    REGSAM sam = KEY_READ;
    if (writable)
        sam |= KEY_WRITE;
    return OpenKey(parentKey, subKey, 0, sam, key);
}

HRESULT RegKey::OpenKey(HKEY parentKey, wchar_t const* subKey,
                        DWORD options, REGSAM samDesired, _Out_ RegKey* key)
{
    FFMF_CheckPointer(key, E_POINTER);

    HKEY hkey;
    HRESULT hr = HResultFromLSTATUS(RegOpenKeyExW(
        parentKey, subKey, options, samDesired, &hkey));

    if (SUCCEEDED(hr))
        *key = RegKey(hkey);

    return hr;
}

bool RegKey::IsValid() const
{
    return hkey != nullptr;
}

HKEY RegKey::Get() const
{
    return hkey;
}

HRESULT RegKey::Close()
{
    if (hkey == NULL)
        return S_OK;

    return HResultFromLSTATUS(RegCloseKey(std::exchange(hkey, nullptr)));
}

HRESULT RegKey::CreateSubKey(wchar_t const* subKeyName, _Out_ RegKey* subKey)
{
    return CreateSubKey(subKeyName, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, subKey);
}

HRESULT RegKey::CreateSubKey(wchar_t const* subKeyName, DWORD options,
                             REGSAM samDesired, _Out_ RegKey* subKey)
{
    FFMF_CheckPointer(subKey, E_POINTER);
    if (hkey == nullptr)
        return E_UNEXPECTED;

    HKEY hsubKey;
    HRESULT hr = HResultFromLSTATUS(RegCreateKeyExW(
        hkey, subKeyName, 0, nullptr, options, samDesired, nullptr, &hsubKey, nullptr));

    if (SUCCEEDED(hr))
        *subKey = RegKey(hsubKey);

    return hr;
}

HRESULT RegKey::DeleteSubKey(wchar_t const* subKeyName)
{
    return DeleteSubKey(subKeyName, true);
}

HRESULT RegKey::DeleteSubKey(wchar_t const* subKeyName, bool ignoreMissing)
{
    if (hkey == nullptr)
        return E_UNEXPECTED;

    HRESULT hr = HResultFromLSTATUS(RegDeleteKeyW(hkey, subKeyName));

    if (ignoreMissing && hr == HResultFromLSTATUS(ERROR_FILE_NOT_FOUND))
        hr = S_OK;

    return hr;
}

HRESULT RegKey::GetSubKeyCount(_Out_ unsigned* pSubKeyCount) const
{
    if (hkey == nullptr)
        return E_UNEXPECTED;

    DWORD subKeyCount;
    HRESULT hr = HResultFromLSTATUS(RegQueryInfoKeyW(hkey, nullptr, nullptr,
      nullptr, &subKeyCount, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr));
    if (SUCCEEDED(hr))
        *pSubKeyCount = static_cast<unsigned>(subKeyCount);
    return hr;
}

HRESULT RegKey::GetValueCount(_Out_ unsigned* pValueCount) const
{
    if (hkey == nullptr)
        return E_UNEXPECTED;

    DWORD valueCount;
    HRESULT hr = HResultFromLSTATUS(RegQueryInfoKeyW(hkey, nullptr, nullptr,
      nullptr, nullptr, nullptr, nullptr, &valueCount, nullptr, nullptr, nullptr, nullptr));
    if (SUCCEEDED(hr))
        *pValueCount = static_cast<unsigned>(valueCount);
    return hr;
}

RegistryValueKind RegKey::GetValueKind(wchar_t const* name)
{
    if (hkey == nullptr)
        return RegistryValueKind::Unknown;

    DWORD type = 0;
    DWORD dataSize = 0;
    LSTATUS st = RegGetValueW(hkey, name, nullptr, RRF_RT_ANY, &type, nullptr, &dataSize);
    if (st != ERROR_SUCCESS)
        return RegistryValueKind::Unknown;

    switch (type) {
    case REG_NONE:                       return RegistryValueKind::None;
    case REG_SZ:                         return RegistryValueKind::String;
    case REG_EXPAND_SZ:                  return RegistryValueKind::ExpandString;
    case REG_BINARY:                     return RegistryValueKind::Binary;
    case REG_DWORD:                      return RegistryValueKind::DWord;
    case REG_DWORD_BIG_ENDIAN:           return RegistryValueKind::DWord;
    case REG_LINK:                       return RegistryValueKind::Link;
    case REG_MULTI_SZ:                   return RegistryValueKind::MultiString;
    case REG_RESOURCE_LIST:              return RegistryValueKind::ResourceList;
    case REG_FULL_RESOURCE_DESCRIPTOR:   return RegistryValueKind::ResourceDescriptor;
    case REG_RESOURCE_REQUIREMENTS_LIST: return RegistryValueKind::ResourceRequirementsList;
    case REG_QWORD:                      return RegistryValueKind::QWord;
    }

    return RegistryValueKind::Unknown;
}

HRESULT RegKey::GetValue(wchar_t const* name, DWORD* pValue)
{
    FFMF_CheckPointer(pValue, E_POINTER);

    DWORD type = 0;
    DWORD value = 0;
    DWORD dataSize = sizeof(value);
    LSTATUS st = RegGetValueW(hkey, name, nullptr, RRF_RT_REG_DWORD, &type,
                              reinterpret_cast<BYTE*>(&value), &dataSize);
    if (st != ERROR_SUCCESS)
        return HResultFromLSTATUS(st);

    *pValue = value;
    return S_OK;
}

HRESULT RegKey::GetValue(wchar_t const* name, uint32_t* pValue)
{
    FFMF_CheckPointer(pValue, E_POINTER);
    FFMF_Assume(sizeof(DWORD) == sizeof(uint32_t));

    DWORD type = 0;
    DWORD value = 0;
    DWORD dataSize = sizeof(value);
    LSTATUS st = RegGetValueW(hkey, name, nullptr, RRF_RT_REG_DWORD, &type,
                              reinterpret_cast<BYTE*>(&value), &dataSize);
    if (st != ERROR_SUCCESS)
        return HResultFromLSTATUS(st);

    *pValue = static_cast<uint32_t>(value);
    return S_OK;
}

HRESULT RegKey::GetValue(wchar_t const* name, uint64_t* pValue)
{
    FFMF_CheckPointer(pValue, E_POINTER);

    DWORD type = 0;
    uint64_t value = 0;
    DWORD dataSize = sizeof(value);
    LSTATUS st = RegGetValueW(hkey, name, nullptr, RRF_RT_REG_QWORD, &type,
                              reinterpret_cast<BYTE*>(&value), &dataSize);
    if (st != ERROR_SUCCESS)
        return HResultFromLSTATUS(st);

    *pValue = value;
    return S_OK;
}

HRESULT RegKey::GetValue(wchar_t const* name, std::wstring& str)
{
    LSTATUS st;

    DWORD type = 0;
    DWORD dataSize = 0;
    DWORD flags = RRF_RT_REG_SZ | RRF_RT_REG_EXPAND_SZ;

    // Retrieve the string size.
    st = RegGetValueW(hkey, name, nullptr, flags, &type, nullptr, &dataSize);
    if (st != ERROR_SUCCESS)
        return HResultFromLSTATUS(st);

    str.resize(dataSize);

    // Retrieve the string data.
    st = RegGetValueW(hkey, name, nullptr, flags, &type,
                      &str[0], &dataSize);
    if (st != ERROR_SUCCESS) {
        str.clear();
        return HResultFromLSTATUS(st);
    }

    return S_OK;
}

HRESULT RegKey::GetValue(wchar_t const* name, std::vector<uint8_t>& buffer)
{
    LSTATUS st;

    DWORD type = 0;
    DWORD dataSize = 0;
    DWORD flags = RRF_RT_REG_BINARY;

    // Retrieve the buffer size.
    st = RegGetValueW(hkey, name, nullptr, flags, &type, nullptr, &dataSize);
    if (st != ERROR_SUCCESS)
        return HResultFromLSTATUS(st);

    buffer.resize(dataSize);

    // Retrieve the buffer data.
    st = RegGetValueW(hkey, name, nullptr, flags, &type,
                      buffer.data(), &dataSize);
    if (st != ERROR_SUCCESS) {
        buffer.clear();
        return HResultFromLSTATUS(st);
    }

    return S_OK;
}

HRESULT RegKey::GetDefaultValue(DWORD* pValue)
{
    return GetValue(nullptr, pValue);
}

HRESULT RegKey::GetDefaultValue(uint32_t* pValue)
{
    return GetValue(nullptr, pValue);
}

HRESULT RegKey::GetDefaultValue(uint64_t* pValue)
{
    return GetValue(nullptr, pValue);
}

HRESULT RegKey::GetDefaultValue(std::wstring& str)
{
    return GetValue(nullptr, str);
}

HRESULT RegKey::GetDefaultValue(std::vector<uint8_t>& buffer)
{
    return GetValue(nullptr, buffer);
}

HRESULT RegKey::SetDefaultValue(wchar_t const* value)
{
    return SetValue(nullptr, value);
}

HRESULT RegKey::SetValue(wchar_t const* valueName, wchar_t const* value)
{
    if (hkey == nullptr)
        return E_UNEXPECTED;

    size_t valueLength = 0;
    ENSURE_HR(StringCchLengthW(value, STRSAFE_MAX_CCH, &valueLength));

    // Include the NULL terminator.
    DWORD byteCount = (static_cast<DWORD>(valueLength) + 1) * sizeof(wchar_t);

    return HResultFromLSTATUS(RegSetValueExW(
        hkey, valueName, 0, REG_SZ, reinterpret_cast<BYTE const*>(value), byteCount));
}

HRESULT RegKey::ForceDeleteDefaultValue()
{
    return ForceDeleteValue(nullptr);
}

HRESULT RegKey::DeleteDefaultValue()
{
    return DeleteValue(nullptr);
}

HRESULT RegKey::ForceDeleteValue(wchar_t const* valueName)
{
    if (hkey == nullptr)
        return E_UNEXPECTED;
    LSTATUS st = RegDeleteKeyValueW(hkey, nullptr, valueName);
    if (st == ERROR_SUCCESS || st == ERROR_FILE_NOT_FOUND)
        return S_OK;
    return HResultFromLSTATUS(st);
}

HRESULT RegKey::DeleteValue(wchar_t const* valueName)
{
    if (hkey == nullptr)
        return E_UNEXPECTED;
    return HResultFromLSTATUS(RegDeleteKeyValueW(hkey, nullptr, valueName));
}

} // namespace ffmf
