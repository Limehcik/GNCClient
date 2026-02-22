using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System;

namespace GNC;

[BepInAutoPlugin]
[BepInProcess("Among Us.exe")]
public partial class VideoMod : BasePlugin
{
    public Harmony Harmony { get; } = new(Id);

    private const string VideoUrl = "https://github.com/Limehcik/GNCClient/raw/refs/heads/master/assets/video.mp4";

    private static GameObject? _uiRoot;
    private static VideoPlayer? _preloader;
    private static RawImage? _displayImage;

    public override void Load()
    {
        Log.LogInfo("GNC System: RAM-Streamer starting...");

        ClassInjector.RegisterTypeInIl2Cpp<KeybindListener>();
        ClassInjector.RegisterTypeInIl2Cpp<VideoDragger>();
        ClassInjector.RegisterTypeInIl2Cpp<VideoResizer>();

        Harmony.PatchAll();

        var holder = new GameObject("GNC_MemoryBuffer");
        UnityEngine.Object.DontDestroyOnLoad(holder);
        _preloader = holder.AddComponent<VideoPlayer>();
        _preloader.url = VideoUrl;
        _preloader.playOnAwake = false;
        _preloader.isLooping = true;
        _preloader.renderMode = VideoRenderMode.APIOnly;

        _preloader.prepareCompleted += (Action<VideoPlayer>)(p => {
            if (_displayImage != null) _displayImage.texture = p.texture;
            Log.LogInfo("GNC: Video buffer is ready in RAM.");
        });

        _preloader.Prepare();

        AddComponent<KeybindListener>().Plugin = this;
    }

    public void Toggle()
    {
        if (_uiRoot != null)
        {
            UnityEngine.Object.Destroy(_uiRoot);
            _uiRoot = null;
            _displayImage = null;
        }
        else Create();
    }

    private void Create()
    {
        _uiRoot = new GameObject("GNC_UI");
        UnityEngine.Object.DontDestroyOnLoad(_uiRoot);

        var canvas = _uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        _uiRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _uiRoot.AddComponent<GraphicRaycaster>();

        var window = new GameObject("GNC_VideoWindow");
        window.transform.SetParent(_uiRoot.transform, false);
        _displayImage = window.AddComponent<RawImage>();
        _displayImage.raycastTarget = true;

        var rt = window.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(480, 270);

        if (_preloader != null)
        {
            var au = window.AddComponent<AudioSource>();
            _preloader.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _preloader.SetTargetAudioSource(0, au);
            au.volume = 1.0f;

            if (_preloader.isPrepared) _displayImage.texture = _preloader.texture;

            _preloader.Play();
        }

        var handle = new GameObject("ResizeGrip");
        handle.transform.SetParent(window.transform, false);
        handle.AddComponent<Image>().color = new Color(1, 1, 1, 0.1f);
        var hr = handle.GetComponent<RectTransform>();
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(1, 0);
        hr.sizeDelta = new Vector2(25, 25);

        window.AddComponent<VideoDragger>().ResizeZone = hr;
        handle.AddComponent<VideoResizer>().Target = rt;
    }

    public class KeybindListener : MonoBehaviour
    {
        public VideoMod Plugin { get; set; } = null!;
        void Update() { if (Input.GetKeyDown(KeyCode.Delete)) Plugin.Toggle(); }
    }

    public class VideoDragger : MonoBehaviour
    {
        public RectTransform? ResizeZone;
        private RectTransform _rt = null!;
        private Canvas _canvas = null!;
        private Vector2 _lastPos;
        private bool _isDragging;
        void Awake() { _rt = GetComponent<RectTransform>(); _canvas = GetComponentInParent<Canvas>(); }
        void Update()
        {
            Vector2 m = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                if (ResizeZone != null && RectTransformUtility.RectangleContainsScreenPoint(ResizeZone, m)) return;
                if (RectTransformUtility.RectangleContainsScreenPoint(_rt, m)) { _isDragging = true; _lastPos = m; }
            }
            if (Input.GetMouseButtonUp(0)) _isDragging = false;
            if (_isDragging)
            {
                _rt.anchoredPosition += (m - _lastPos) / _canvas.scaleFactor;
                _lastPos = m;
            }
        }
    }

    public class VideoResizer : MonoBehaviour
    {
        public RectTransform Target { get; set; } = null!;
        private Canvas _canvas = null!;
        private bool _isResizing;
        private Vector2 _lastPos;
        void Awake() => _canvas = GetComponentInParent<Canvas>();
        void Update()
        {
            if (Target == null) return;
            Vector2 m = Input.mousePosition;
            if (Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), m))
            {
                _isResizing = true; _lastPos = m;
            }
            if (Input.GetMouseButtonUp(0)) _isResizing = false;
            if (_isResizing)
            {
                float newW = Mathf.Max(200f, Target.sizeDelta.x + (m.x - _lastPos.x) / _canvas.scaleFactor);
                Target.sizeDelta = new Vector2(newW, newW * 0.5625f);
                _lastPos = m;
            }
        }
    }
}