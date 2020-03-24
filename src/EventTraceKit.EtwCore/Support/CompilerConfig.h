#pragma once
#if defined(__clang__)
  #define ETK_CLANG __clang__
#elif defined(_MSC_VER)
  #define ETK_MSVC _MSC_VER
#elif defined(__GNUC__)
  #define ETK_GCC __GNUC__
#endif

#ifdef _WIN64
  #define ETK_X64
#else
  #define ETK_X86
#endif
