# MAUI 10.0.70 Accessibility Crash Repro

Reproduces a crash introduced in **Microsoft.Maui.Controls 10.0.70** on Android Release builds.

When an accessibility service (Voice Access, TalkBack, etc.) is active and the user navigates to a page containing a `CollectionView` with `SelectionMode="Single"`, the app crashes with:

```
System.MissingMethodException: Method not found: void AndroidX.Core.View.Accessibility.AccessibilityNodeInfoCompat.set_Checked(bool)
```

The accessibility service calls `onInitializeAccessibilityNodeInfo` on each collection item to determine whether it is selected (checked). MAUI's `ControlsAccessibilityDelegate` handles this callback and calls `set_Checked` — but in Release builds the IL linker strips that method because the call site is invisible to static analysis (it is reached only via a JNI callback).

This crash does **not** occur with `Microsoft.Maui.Controls 10.0.51`.

## Steps to reproduce

1. Clone this repo
2. Connect an Android device
3. Enable Voice Access:
   ```
   adb shell settings put secure enabled_accessibility_services com.google.android.apps.accessibility.voiceaccess/.JustSpeakService
   adb shell settings put secure accessibility_enabled 1
   ```
4. Build and deploy in **Release** mode:
   ```
   dotnet build -t:Run -f net10.0-android -c Release -p:AndroidAttachDebugger=false
   ```
5. Tap **"Navigate to CollectionView page"**

## Expected behavior

Navigation succeeds; the list of items is displayed.

## Actual behavior

App crashes:

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
- `net10.0-android`, Release build only (IL linking is disabled in Debug)
- Confirmed on Android 16 (API 36) and Android emulator API 35

## Root cause

A verbose build (`-v:detailed`) shows the following warning in 10.0.70:

```
Marshal method 'n_OnInitializeAccessibilityNodeInfo...' for architecture 'X86_64'
should be declared in type 'Microsoft.Maui.Platform.MauiAccessibilityDelegateCompat',
but instead was declared in 'Microsoft.Maui.Controls.Platform.ControlsAccessibilityDelegate'
```

10.0.70 changed the accessibility delegate hierarchy so that `ControlsAccessibilityDelegate` owns the JNI callback for `OnInitializeAccessibilityNodeInfo`. The call to `AccessibilityNodeInfoCompat.set_Checked` inside that override is not reachable via static analysis, so the IL linker strips it from `Xamarin.AndroidX.Core`.

The fix belongs in MAUI: the call site in `ControlsAccessibilityDelegate.OnInitializeAccessibilityNodeInfo` needs a `[DynamicDependency]` or `[Preserve]` annotation. No app-side workaround (TrimmerRootDescriptor, AndroidLinkSkip, DynamicDependency from app code) resolves the crash.

## Workaround

Downgrade to `Microsoft.Maui.Controls` 10.0.51.
