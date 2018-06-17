#pragma once
#include <boost/config.hpp>

#if defined(BOOST_CLANG)
  #define ETK_CLANG __clang__
#elif defined(BOOST_MSVC)
  #define ETK_MSVC _MSC_VER
#elif defined(BOOST_GCC)
  #define ETK_GCC BOOST_GCC
#endif

#ifdef _WIN64
  #define ETK_X64
#else
  #define ETK_X86
#endif
