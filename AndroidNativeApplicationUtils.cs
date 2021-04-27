#if UNITY_ANDROID
//
// Created by Eduardo Coelho <dev@educoelho.com>
// Copyright (c) 2015 Redstone Games. All rights reserved.
//
using System;
using UnityEngine;

namespace Redstone.Common.NativeUtils
{
    public static class AndroidNativeApplicationUtils
    {
        #region Public methods

        public static string GetAppVersion ()
        {
            if (Application.platform == RuntimePlatform.Android) {
                try {
                    return nativeProxy.GetAppVersion ();
                } catch (Exception ex) {
                    Debug.LogException (ex);
                }
            }

            return string.Empty;
        }

        public static void ShowStatusBar ()
        {
            if (Application.platform == RuntimePlatform.Android) {
                try {
                    nativeProxy.ShowStatusBar ();
                } catch (Exception ex) {
                    Debug.LogException (ex);
                }
            }
        }

        #endregion

        static AndroidNativeApplicationUtilsProxy _nativeProxy;
        static AndroidNativeApplicationUtilsProxy nativeProxy {
            get {
                if (_nativeProxy == null)
                    _nativeProxy = new AndroidNativeApplicationUtilsProxy ();

                return _nativeProxy;
            }
        }
    }

    class AndroidNativeApplicationUtilsProxy
    {
        #region Public

        public string GetAppVersion ()
        {
            if (Application.platform == RuntimePlatform.Android) {
                using (var actClass = new AndroidJavaClass ("com.unity3d.player.UnityPlayer")) {
                    var activity = actClass.GetStatic<AndroidJavaObject> ("currentActivity");
                    var packageManager = activity.Call<AndroidJavaObject> ("getPackageManager");

                    var packageName = activity.Call<string> ("getPackageName");
                    const int flags = 0;

                    var packageInfo = packageManager.Call<AndroidJavaObject> ("getPackageInfo", packageName, flags);
                    var versionName = packageInfo.Get<string> ("versionName");

                    return versionName;
                }
            }

            return string.Empty;
        }

        public void ShowStatusBar ()
        {
            if (Application.platform == RuntimePlatform.Android) {
                using (var actClass = new AndroidJavaClass ("com.unity3d.player.UnityPlayer")) {
                    var activity = actClass.GetStatic<AndroidJavaObject> ("currentActivity");
                    activity.Call ("runOnUiThread", new AndroidJavaRunnable (ShowStatusBarOnUiThread));
                }                
            }
        }

        #endregion

        #region Private

        // https://docs.unity3d.com/ScriptReference/AndroidJavaRunnable.html
        void ShowStatusBarOnUiThread ()
        {
            int sdkInt = GetAndroidVersionSDKInt ();
            if (sdkInt < 30) {
                using (var actClass = new AndroidJavaClass ("com.unity3d.player.UnityPlayer")) {
                    var activity = actClass.GetStatic<AndroidJavaObject> ("currentActivity");
                    var window = activity.Call<AndroidJavaObject> ("getWindow");

                    // - https://stackoverflow.com/a/32693915
                    // - https://developer.android.com/reference/android/view/WindowManager.LayoutParams.html#FLAG_FORCE_NOT_FULLSCREEN
                    // - https://developer.android.com/reference/android/view/WindowManager.LayoutParams.html#FLAG_FULLSCREEN
                    window.Call ("addFlags", 2048);
                    window.Call ("clearFlags", 1024);
                }
            } else {
                // Android 11 and later
                using (var actClass = new AndroidJavaClass ("com.unity3d.player.UnityPlayer")) {
                    var activity = actClass.GetStatic<AndroidJavaObject> ("currentActivity");
                    var window = activity.Call<AndroidJavaObject> ("getWindow");

                    window.Call ("setDecorFitsSystemWindows", false);

                    var windowInsetsController = window.Call<AndroidJavaObject> ("getInsetsController");
                    if (windowInsetsController != null) {
                        var windowInsetsType = new AndroidJavaClass ("android.view.WindowInsets$Type");
                        var statusBarsType = windowInsetsType.CallStatic<int> ("statusBars");
                        var navigationBarsType = windowInsetsType.CallStatic<int> ("navigationBars");

                        windowInsetsController.Call ("show", statusBarsType);
                        windowInsetsController.Call ("hide", navigationBarsType);

                        // Color.TRANSPARENT: https://developer.android.com/reference/android/R.color#transparent
                        window.Call ("setStatusBarColor", 0x00000000);
                    }
                }
            }
        }

        int GetAndroidVersionSDKInt ()
        {
            // - http://answers.unity.com/answers/976606/view.html
            // - https://developer.android.com/reference/android/os/Build.VERSION#SDK_INT
            // - https://developer.android.com/reference/android/os/Build.VERSION_CODES
            using (var version = new AndroidJavaClass ("android.os.Build$VERSION")) {
                return version.GetStatic<int> ("SDK_INT");
            }
        }

        #endregion
    }
}

#endif