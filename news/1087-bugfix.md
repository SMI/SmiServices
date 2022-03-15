[CI] Misc. CI fixes
-  Fixes the build scripts to respect any intermediate non-zero return codes
-  Fixes the build scripts to only build `linux-x64` and `win-x64`
-  Fixes the build scripts to select the correct build configuration
-  Removes a bogus test leftover from #1089
-  Temporarily disables a few tests requiring a fix for the `leptonica` libs
