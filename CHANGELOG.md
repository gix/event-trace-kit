# Changelog
All notable changes to this project will be documented in this file.

Abbreviations:
- VS: VisualStudio extension
- EMC: Event Manifest Compiler and accompanying libraries

## [0.3.0] - 2020-03-24
### Added
- VS: Support for VisualBasic.NET projects
- VS: Support for Visual Studio 2019
- VS: Support for decoding TraceLogging events
- EMC: Support for Event Template version 5 (used since Windows 10 16299)
- EMC: Support for ETW provider traits (provider name, control GUID, group GUID)
  and event attributes
- EMC: cxx code generator uses the namespace attribute of ETW providers
- EMC: Support for a custom provider trait to include the process name with each event.
- EMC: Experimental support for event providers in static libraries.

### Changed
- VS: Increased duration of long help tooltips
- EMC: Merged the EventManifestFramework assembly and most bits of the manifest
  compiler into the new assembly EventTraceKit.EventTracing. Compilation and
  decompilation can now be consumed as a library.
- EMC: Updated code generators to match Microsoft's Manifest Compiler (mc) version 18362
- EMC: Stricter event manifest validation matching the TraceDataHelper (tdh.h)
  API and Microsoft's Manifest Compiler (mc):
  - Providers must specify resourceFileName and messageFileName attributes.
  - Events logged to admin channels must have a message and a non-verbose log
    level.

### Fixed
- VS: Crash when projects are in solution folders.
- VS: Projects in solution folders are not visible in settings dialog.
- VS: StartupProject dropdown does not show the current value.

## [0.2.0.6302901] - 2018-06-29

Initial release.
