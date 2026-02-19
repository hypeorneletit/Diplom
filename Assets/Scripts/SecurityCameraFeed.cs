using System;
using UnityEngine;

/// <summary>
/// Простая "камера наблюдения" на базе набора скриншотов (Texture2D).
/// Вешается на объект монитора/экрана и подставляет текстуру в материал Renderer.
/// </summary>
public class SecurityCameraFeed : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Renderer screenRenderer;

    [Header("Camera Screenshots")]
    [Tooltip("Скриншоты камер (например security_camera1..4)")]
    [SerializeField] private Texture2D[] cameraFrames;

    [Header("Material Texture Property")]
    [Tooltip("Имя свойства текстуры. Для URP обычно _BaseMap, для Standard - _MainTex.")]
    [SerializeField] private string textureProperty = "_BaseMap";

    [Header("Behaviour")]
    [SerializeField] private int startIndex = 0;
    [SerializeField] private bool loop = true;

    [Header("Auto Cycle (Optional)")]
    [SerializeField] private bool autoCycle = false;
    [SerializeField] private float autoCycleIntervalSeconds = 2.0f;

    public event Action<int> OnCameraChanged;

    private MaterialPropertyBlock mpb;
    private int currentIndex;
    private float nextAutoSwitchTime;

    public int CurrentIndex => currentIndex;
    public int CameraCount => cameraFrames != null ? cameraFrames.Length : 0;

    private void Awake()
    {
        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        if (screenRenderer == null)
            screenRenderer = GetComponentInChildren<Renderer>();

        if (screenRenderer == null)
            Debug.LogWarning($"[SecurityCameraFeed] Screen Renderer not set on {name}");

        currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, CameraCount - 1));

        if (string.IsNullOrWhiteSpace(textureProperty))
            textureProperty = "_BaseMap";
    }

    private void OnEnable()
    {
        ApplyCurrentFrame();
        ScheduleNextAutoSwitch();
    }

    private void Update()
    {
        if (!autoCycle || CameraCount <= 1)
            return;

        if (Time.time >= nextAutoSwitchTime)
        {
            Next();
            ScheduleNextAutoSwitch();
        }
    }

    public void Next()
    {
        if (CameraCount == 0)
            return;

        int next = currentIndex + 1;
        if (next >= CameraCount)
            next = loop ? 0 : CameraCount - 1;

        SetIndex(next);
    }

    public void Prev()
    {
        if (CameraCount == 0)
            return;

        int prev = currentIndex - 1;
        if (prev < 0)
            prev = loop ? CameraCount - 1 : 0;

        SetIndex(prev);
    }

    public void SetIndex(int index)
    {
        if (CameraCount == 0)
            return;

        int clamped = Mathf.Clamp(index, 0, CameraCount - 1);
        if (clamped == currentIndex)
            return;

        currentIndex = clamped;
        ApplyCurrentFrame();
        OnCameraChanged?.Invoke(currentIndex);
    }

    public void SetAutoCycle(bool enabled)
    {
        autoCycle = enabled;
        ScheduleNextAutoSwitch();
    }

    private void ScheduleNextAutoSwitch()
    {
        if (!autoCycle)
            return;

        float interval = Mathf.Max(0.1f, autoCycleIntervalSeconds);
        nextAutoSwitchTime = Time.time + interval;
    }

    public Texture2D GetCurrentFrame()
    {
        if (cameraFrames == null || CameraCount == 0)
            return null;

        int idx = Mathf.Clamp(currentIndex, 0, CameraCount - 1);
        return cameraFrames[idx];
    }

    private void ApplyCurrentFrame()
    {
        if (screenRenderer == null)
            return;

        Texture2D frame = (cameraFrames != null && currentIndex >= 0 && currentIndex < cameraFrames.Length)
            ? cameraFrames[currentIndex]
            : null;

        screenRenderer.GetPropertyBlock(mpb);

        // Пытаемся по указанному свойству, а если его нет — откатываемся на _MainTex.
        if (!string.IsNullOrEmpty(textureProperty))
        {
            mpb.SetTexture(textureProperty, frame);
        }

        // Многие материалы/шейдеры всё ещё используют _MainTex.
        mpb.SetTexture("_MainTex", frame);

        screenRenderer.SetPropertyBlock(mpb);
    }
}

