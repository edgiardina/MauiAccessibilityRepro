# MAUI 10.0.70 + Xamarin.AndroidX.Core 1.17.0.2 Accessibility Crash Repro

Reproduces a crash in **Microsoft.Maui.Controls 10.0.70** on Android Release builds when `Xamarin.AndroidX.Core` is resolved to `1.17.0.2` or later.

When an accessibility service (Voice Access, TalkBack, etc.) is active and the user navigates to a page containing a `CollectionView` with `SelectionMode` != `None`, the app crashes with:

```
System.MissingMethodException: Method not found: void AndroidX.Core.View.Accessibility.AccessibilityNodeInfoCompat.set_Checked(bool)
  at Microsoft.Maui.Platform.MauiAccessibilityDelegateCompat.OnInitializeAccessibilityNodeInfo
```

## Root cause

`Xamarin.AndroidX.Core 1.17.0.2` changed `AccessibilityNodeInfoCompat.Checked` from a `bool` property to an `int` property. This is a binary breaking change:

- `1.16.0.3`: `bool Checked { get; set; }` → `void set_Checked(bool)`
- `1.17.0.2`: `int Checked { get; set; }` → `void set_Checked(int)` (+ `SetChecked(bool)`)

MAUI 10.0.70's `MauiAccessibilityDelegateCompat.OnInitializeAccessibilityNodeInfo` was compiled against `1.16.0.3` and calls `set_Checked(bool)`. When any transitive dependency in your app requires `Xamarin.AndroidX.Core >= 1.17.0.2`, NuGet resolves to `1.17.0.2` and `set_Checked(bool)` no longer exists at runtime.

This is **not** an IL linker issue — the method signature no longer exists in `1.17.0.2`.

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
E AndroidRuntime:   at Microsoft.Maui.Platform.MauiAccessibilityDelegateCompat.OnInitializeAccessibilityNodeInfo + 0x43(Unknown Source)
E AndroidRuntime:   at AndroidX.Core.View.AccessibilityDelegateCompat.n_OnInitializeAccessibilityNodeInfo_Landroid_view_View_Landroidx_core_view_accessibility_AccessibilityNodeInfoCompat_ + 0x25(Unknown Source)
E AndroidRuntime:   at crc6452ffdc5b34af3a0f.MauiAccessibilityDelegateCompat.n_onInitializeAccessibilityNodeInfo(Native Method)
E AndroidRuntime:   at crc6452ffdc5b34af3a0f.MauiAccessibilityDelegateCompat.onInitializeAccessibilityNodeInfo(MauiAccessibilityDelegateCompat.java:36)
E AndroidRuntime:   at androidx.core.view.AccessibilityDelegateCompat$AccessibilityDelegateAdapter.onInitializeAccessibilityNodeInfo(AccessibilityDelegateCompat.java:90)
E AndroidRuntime:   at android.view.View.onInitializeAccessibilityNodeInfo(View.java:9835)
E AndroidRuntime:   at android.view.AccessibilityInteractionController$AccessibilityNodePrefetcher.prefetchDescendantsOfRealNode(AccessibilityInteractionController.java:1622)
```

## Environment

- `Microsoft.Maui.Controls` 10.0.70
- `Xamarin.AndroidX.Core` 1.17.0.2 (pinned in csproj to trigger the conflict; in production apps this version is pulled in transitively by other packages)
- `net10.0-android`, minSdk 24, Release build
- Confirmed on Android 16 (API 36, Samsung Galaxy S23)

## Fix

MAUI's `MauiAccessibilityDelegateCompat.OnInitializeAccessibilityNodeInfo` must be updated to use the `SetChecked(bool)` method (available in both `1.16.0.3` and `1.17.0.2`) instead of the `bool Checked` property setter.

## Workaround

Either:
- Downgrade to `Microsoft.Maui.Controls` 10.0.51
- Or pin `Xamarin.AndroidX.Core` to `1.16.0.3` in your csproj
