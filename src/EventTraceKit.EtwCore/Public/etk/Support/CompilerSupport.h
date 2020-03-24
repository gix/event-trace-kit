#pragma once
#include "etk/Support/CompilerConfig.h"

#ifdef ETK_CLANG
#define ETK_GCC_DIAG_DO_PRAGMA(x) _Pragma(#x)
#define ETK_GCC_DIAG_PRAGMA(x) ETK_GCC_DIAG_DO_PRAGMA(GCC diagnostic x)

#define ETK_DIAGNOSTIC_PUSH() ETK_GCC_DIAG_PRAGMA(push)
#define ETK_DIAGNOSTIC_POP() ETK_GCC_DIAG_PRAGMA(pop)
#define ETK_DIAGNOSTIC_DISABLE_CLANG(x) ETK_GCC_DIAG_PRAGMA(ignored x)
#define ETK_DIAGNOSTIC_DISABLE_MSVC(n)
#define ETK_DIAGNOSTIC_PUSH_MSVC()
#define ETK_DIAGNOSTIC_POP_MSVC()
#define ETK_DIAGNOSTIC_PUSH_DISABLE_MSVC(n)
#elif defined(ETK_MSVC)
#define ETK_DIAGNOSTIC_PUSH() __pragma(warning(push))
#define ETK_DIAGNOSTIC_POP() __pragma(warning(pop))
#define ETK_DIAGNOSTIC_DISABLE_MSVC(n) __pragma(warning(disable : n))
#define ETK_DIAGNOSTIC_DISABLE_CLANG(x)

#define ETK_DIAGNOSTIC_PUSH_MSVC() ETK_DIAGNOSTIC_PUSH()
#define ETK_DIAGNOSTIC_POP_MSVC() ETK_DIAGNOSTIC_POP()
#define ETK_DIAGNOSTIC_PUSH_DISABLE_MSVC(n)                                              \
    ETK_DIAGNOSTIC_PUSH_MSVC()                                                           \
    ETK_DIAGNOSTIC_DISABLE_MSVC(n)
#else
#define ETK_DIAGNOSTIC_PUSH()
#define ETK_DIAGNOSTIC_POP()
#define ETK_DIAGNOSTIC_DISABLE_MSVC(n)
#define ETK_DIAGNOSTIC_DISABLE_CLANG(x)

#define ETK_DIAGNOSTIC_PUSH_MSVC()
#define ETK_DIAGNOSTIC_POP_MSVC()
#define ETK_DIAGNOSTIC_PUSH_DISABLE_MSVC(n)
#endif

/// <summary>
///   Allows <c>this</c> to be passed as an argument in constructor initializer
///   lists.
/// </summary>
/// <example>
///   <code>
///     Foo::Foo()
///         : x(nullptr)
///         , ETK_ALLOW_THIS_IN_INITIALIZER_LIST(y(this))
///         , z(3)
///     { }
///   </code>
/// </example>
/// <remarks>
///   Suppresses "Compiler warning C4355: 'this': used in base member initializer list":
/// </remarks>
#define ETK_ALLOW_THIS_IN_INITIALIZER_LIST(code)                                         \
    ETK_DIAGNOSTIC_PUSH_DISABLE_MSVC(4355)                                               \
    code ETK_DIAGNOSTIC_POP_MSVC()

#define ETK_MULTILINE_MACRO_BEGIN do {

#define ETK_MULTILINE_MACRO_END                                                          \
    ETK_DIAGNOSTIC_PUSH_DISABLE_MSVC(4127)                                               \
    }                                                                                    \
    while (false)                                                                        \
    ETK_DIAGNOSTIC_POP_MSVC()

#ifdef ETK_CLANG
#define ETK_ALWAYS_INLINE inline __attribute__((always_inline))
#elif defined(ETK_MSVC)
#define ETK_ALWAYS_INLINE __forceinline
#else
#error Always inline not supported.
#endif

#ifdef ETK_CLANG
#define ETK_NOINLINE __attribute__((noinline))
#elif defined(ETK_MSVC)
#define ETK_NOINLINE __declspec(noinline)
#else
#error noinline not supported.
#endif

#if defined(ETK_CLANG) || defined(ETK_MSVC)
#define ETK_CLSID(id) __declspec(uuid(id))
#define ETK_IID(id) __declspec(uuid(id))
#else
#error Not supported.
#endif

#if defined(ETK_CLANG) || defined(ETK_MSVC)
#define ETK_NOVTABLE __declspec(novtable)
#else
#error Not supported.
#endif

#ifdef ETK_CLANG
#define ETK_NORETURN __attribute__((noreturn))
#elif defined(ETK_MSVC)
#define ETK_NORETURN __declspec(noreturn)
#else
#error Not supported.
#endif

#if defined(ETK_CLANG)
#define ETK_BUILTIN_UNREACHABLE __builtin_unreachable()
#elif defined(ETK_MSVC)
#define ETK_BUILTIN_UNREACHABLE __assume(false)
#else
#error Not supported.
#endif
