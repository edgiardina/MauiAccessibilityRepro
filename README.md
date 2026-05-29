# MAUI 10.0.70 Accessibility Crash Repro

Minimal reproduction for a crash introduced in **Microsoft.Maui.Controls 10.0.70** affecting Android Release builds when any accessibility service is active.

## Bug summary

`System.MissingMethodException: Method not found: void AndroidX.Core.View.Accessibility.AccessibilityNodeInfoCompat.set_Checked(bool)`

The IL linker strips `AccessibilityNodeInfoCompat.set_Checked` in Release builds because the call site inside MAUI's accessibility delegate is reached only via a JNI callback, which is invisible to static linker analysis. The crash occurs for **any user with an accessibility service active** (Voice Access, TalkBack, password managers, screen readers).

This crash does **not** occur with `Microsoft.Maui.Controls 10.0.51`.

## Steps to reproduce

1. Clone this repo
2. Connect an Android device or start an emulator
3. Enable an accessibility service:
   ```
   adb shell settings put secure enabled_accessibility_services com.google.android.apps.accessibility.voiceaccess/.JustSpeakService
   adb shell settings put secure accessibility_enabled 1
   ```
4. Build and deploy in **Release** mode:
   ```
   dotnet build -t:Run -f net10.0-android -c Release -p:AndroidAttachDebugger=false
   ```
5. Launch the app

## Expected behavior

App launches without crashing.

## Actual behavior

App crashes immediately with:

```
E AndroidRuntime: android.runtime.JavaProxyThrowable: [System.MissingMethodException]: Method not found: void AndroidX.Core.View.Accessibility.AccessibilityNodeInfoCompat.set_Checked(bool)
```

Full logcat:
```
E AndroidRuntime: FATAL EXCEPTION: main
E AndroidRuntime: android.runtime.JavaProxyThrowable: [System.MissingMethodException]: Method not found: void AndroidX.Core.View.Accessibility.AccessibilityNodeInfoCompat.set_Checked(bool)
E AndroidRuntime:   at androidx.core.view.AccessibilityDelegateCompat$AccessibilityDelegateAdapter.onInitializeAccessibilityNodeInfo(AccessibilityDelegateCompat.java:90)
E AndroidRuntime:   at android.view.View.onInitializeAccessibilityNodeInfo(View.java:9835)
E AndroidRuntime:   at android.view.View.createAccessibilityNodeInfoInternal(View.java:9796)
E AndroidRuntime:   at android.view.View$AccessibilityDelegate.createAccessibilityNodeInfo(View.java:34809)
E AndroidRuntime:   at android.view.View.createAccessibilityNodeInfo(View.java:9779)
E AndroidRuntime:   at android.view.AccessibilityInteractionController$AccessibilityNodePrefetcher.prefetchDescendantsOfRealNode(AccessibilityInteractionController.java:1622)
```

## Environment

- `Microsoft.Maui.Controls` 10.0.70 (broken), 10.0.51 (working)
- Target: `net10.0-android`
- Build configuration: **Release** (Debug builds are not affected — IL linking is disabled)
- Android API 21+, confirmed on Android 16 (API 36)

## Root cause analysis

Verbose build output (`-v:detailed`) shows the build warning:

```
Marshal method 'n_OnInitializeAccessibilityNodeInfo...' for architecture 'X86_64'
should be declared in type 'Microsoft.Maui.Platform.MauiAccessibilityDelegateCompat',
but instead was declared in 'Microsoft.Maui.Controls.Platform.ControlsAccessibilityDelegate'
```

MAUI 10.0.70 changed the accessibility delegate hierarchy so that `ControlsAccessibilityDelegate` handles the JNI callback for `OnInitializeAccessibilityNodeInfo`. The call to `AccessibilityNodeInfoCompat.set_Checked` within that override is not reachable via static analysis, so the IL linker strips it from the `Xamarin.AndroidX.Core` assembly.

None of the standard linker preservation mechanisms resolve this:
- `AndroidLinkDescription` XML (old Xamarin format — ignored by .NET 10 ILLink)
- `AndroidLinkSkip` — does not prevent the stripping
- `TrimmerRootDescriptor` XML with `preserve="nothing"` + explicit method
- `TrimmerRootDescriptor` XML with `preserve="all"` on the type
- `[DynamicDependency]` attribute on a guaranteed-live method

## Workaround

Downgrade to `Microsoft.Maui.Controls` 10.0.51.
