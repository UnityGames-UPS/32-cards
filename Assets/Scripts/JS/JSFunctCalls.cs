using System.Runtime.InteropServices;
using UnityEngine;

public class JSFunctCalls : MonoBehaviour
{
  [DllImport("__Internal")] private static extern void SendLogToReactNative(string message);
  [DllImport("__Internal")] private static extern void SendPostMessage(string message);
  [DllImport("__Internal")] private static extern void RequestFullscreen();
  [DllImport("__Internal")] private static extern void ExitFullscreen();
  [DllImport("__Internal")] private static extern void RegisterFullscreenChangeListener(string gameObjectName);
  void OnEnable()
  {
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.logMessageReceived += HandleLog;
#endif
  }

  void OnDisable()
  {
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.logMessageReceived -= HandleLog;
#endif
  }

#if UNITY_WEBGL && !UNITY_EDITOR
  void HandleLog(string logString, string stackTrace, LogType type)
  {
    string formattedMessage = $"[{type}] {logString}";
    SendLogToReactNative(formattedMessage);
  }
#endif

  internal void SendCustomMessage(string message)
  {
#if UNITY_WEBGL && !UNITY_EDITOR
    SendPostMessage(message);
#endif
  }

  /// <summary>Requests browser fullscreen (expand).</summary>
  internal void RequestExpandGame()
  {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("[JS] Requesting fullscreen expand");
        RequestFullscreen();
#else
    Debug.Log("[JS] Would request fullscreen (editor mode)");
#endif
  }

  /// <summary>Exits browser fullscreen (shrink).</summary>
  internal void RequestShrinkGame()
  {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("[JS] Requesting exit fullscreen (shrink)");
        ExitFullscreen();
#else
    Debug.Log("[JS] Would exit fullscreen (editor mode)");
#endif
  }

  /// <summary>
  /// Registers a browser fullscreenchange listener that calls back into Unity
  /// on the given GameObject when the user exits fullscreen externally (e.g. Escape key).
  /// </summary>
  internal void RegisterFullscreenListener(string gameObjectName)
  {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[JS] Registering fullscreen change listener on '{gameObjectName}'");
        RegisterFullscreenChangeListener(gameObjectName);
#else
    Debug.Log("[JS] Fullscreen listener not registered (editor mode)");
#endif
  }
}
