#pragma once

#ifdef ETK_LOGGER_STATIC
#define ETK_EXPORT
#elif defined(ETK_LOGGER_BUILD)
#define ETK_EXPORT __declspec(dllexport)
#else
#define ETK_EXPORT __declspec(dllimport)
#endif
