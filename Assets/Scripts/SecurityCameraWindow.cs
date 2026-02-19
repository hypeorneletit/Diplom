using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Всплывающее окно просмотра камер наблюдения (как MonitorWindow, но под скриншоты).
/// Показывает картинку с камеры, текущие дату/время и номер камеры.
/// </summary>
public class SecurityCameraWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas windowCanvas;
    [SerializeField] private GameObject windowPanel;
    [SerializeField] private RawImage cameraImage;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button closeButton;
    [SerializeField] private XRSimpleInteractable closeButtonInteractable;

    [Header("Camera Data")]
    [SerializeField] private SecurityCameraFeed cameraFeed;
    [Tooltip("Необязательно. Если заполнено, можно задать красивые имена камер. Иначе будет 'Камера 1/2/3...'")]
    [SerializeField] private string[] cameraNames;

    [Header("Window Settings")]
    [SerializeField] private float windowDistance = 0.6f;
    [SerializeField] private Vector2 windowSize = new Vector2(0.65f, 0.45f);
    [SerializeField] private float windowHeight = 0.1f;
    [SerializeField] private float fontSize = 26f;

    [Header("Styling & Updates")]
    [SerializeField] private bool autoStyleInfoText = false;
    [SerializeField] private bool autoLayoutCameraImage = true;
    [SerializeField] private float timeUpdateIntervalSeconds = 1.0f;

    private bool isOpen;
    private Transform playerHead;
    private RectTransform canvasRect;
    private float nextTimeUpdate;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (windowPanel != null)
            windowPanel.SetActive(false);

        if (windowCanvas != null)
        {
            canvasRect = windowCanvas.GetComponent<RectTransform>();
            windowCanvas.gameObject.SetActive(false);
        }

        if (cameraImage != null)
            cameraImage.texture = null;

        if (infoText != null)
            infoText.text = string.Empty;
    }

    private void Start()
    {
        FindPlayerHead();
        SetupCanvas();
        ConfigureTexts();

        if (closeButton != null)
            SetupCloseButton();
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Time.time >= nextTimeUpdate)
        {
            UpdateInfoTextOnly();
            nextTimeUpdate = Time.time + Mathf.Max(0.1f, timeUpdateIntervalSeconds);
        }
    }

    private void FindPlayerHead()
    {
        if (Camera.main != null)
        {
            playerHead = Camera.main.transform;
            return;
        }

        var cameras = FindObjectsOfType<Camera>();
        foreach (var cam in cameras)
        {
            if (cam.enabled && cam.gameObject.activeInHierarchy)
            {
                playerHead = cam.transform;
                return;
            }
        }
    }

    private void SetupCanvas()
    {
        if (windowCanvas == null || canvasRect == null)
            return;

        windowCanvas.renderMode = RenderMode.WorldSpace;
        canvasRect.sizeDelta = windowSize * 1000f;
        canvasRect.localScale = Vector3.one * 0.001f;

        var scaler = windowCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 100;
        }
    }

    private void ConfigureTexts()
    {
        if (infoText != null)
        {
            if (autoStyleInfoText)
            {
                infoText.fontSize = fontSize;
                infoText.color = Color.white;
                infoText.alignment = TextAlignmentOptions.BottomLeft;
                infoText.enableWordWrapping = true;
            }

            var rect = infoText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 0.3f);
                rect.offsetMin = new Vector2(15, 10);
                rect.offsetMax = new Vector2(-15, 5);
            }
        }

        if (cameraImage != null && autoLayoutCameraImage)
        {
            var rect = cameraImage.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Картинка занимает верхние ~70% окна.
                rect.anchorMin = new Vector2(0f, 0.3f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.offsetMin = new Vector2(10f, 10f);
                rect.offsetMax = new Vector2(-10f, -10f);
            }
        }
    }

    private void SetupCloseButton()
    {
        if (closeButton == null)
        {
            Debug.LogWarning("[SecurityCameraWindow] Close button not assigned!");
            return;
        }

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(CloseWindow);

        var buttonTextTMP = closeButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonTextTMP != null)
        {
            buttonTextTMP.text = "✕ ЗАКРЫТЬ";
            buttonTextTMP.fontSize = 20f;
            buttonTextTMP.color = Color.white;
        }

        var colors = closeButton.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        closeButton.colors = colors;

        if (closeButtonInteractable == null)
            closeButtonInteractable = closeButton.GetComponent<XRSimpleInteractable>();

        if (closeButtonInteractable == null)
            closeButtonInteractable = closeButton.gameObject.AddComponent<XRSimpleInteractable>();

        var collider = closeButton.GetComponent<BoxCollider>();
        if (collider == null)
        {
            var rectTransform = closeButton.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                collider = closeButton.gameObject.AddComponent<BoxCollider>();
                Vector3 worldSize = rectTransform.TransformVector(
                    new Vector3(rectTransform.rect.width, rectTransform.rect.height, 100f));
                float width = Mathf.Max(Mathf.Abs(worldSize.x), 0.30f);
                float height = Mathf.Max(Mathf.Abs(worldSize.y), 0.08f);
                float depth = Mathf.Max(Mathf.Abs(worldSize.z), 0.30f);

                collider.size = new Vector3(width, height, depth);
                collider.isTrigger = true;
                collider.center = Vector3.zero;
            }
        }
        else
        {
            collider.size = new Vector3(
                Mathf.Max(collider.size.x, 0.3f),
                Mathf.Max(collider.size.y, 0.08f),
                Mathf.Max(collider.size.z, 0.3f)
            );
            collider.isTrigger = true;
            collider.enabled = true;
        }

        closeButton.gameObject.SetActive(true);
        closeButtonInteractable.enabled = true;
        closeButtonInteractable.interactionLayers = InteractionLayerMask.GetMask("Default");

        closeButtonInteractable.selectEntered.RemoveAllListeners();
        closeButtonInteractable.selectEntered.AddListener((_) => CloseWindow());
        closeButtonInteractable.activated.RemoveAllListeners();
        closeButtonInteractable.activated.AddListener((_) => CloseWindow());
    }

    private void PositionWindow()
    {
        if (playerHead == null || windowCanvas == null)
            return;

        Vector3 forward = playerHead.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 position = playerHead.position + forward * windowDistance;
        position.y = playerHead.position.y - windowHeight;

        windowCanvas.transform.position = position;
        windowCanvas.transform.LookAt(playerHead);
        windowCanvas.transform.Rotate(0, 180, 0);
    }

    public void OpenWindow()
    {
        if (isOpen)
            return;

        isOpen = true;

        // При просмотре через окно всегда листаем камеры только вручную,
        // чтобы номер камеры и картинка совпадали.
        if (cameraFeed != null)
        {
            cameraFeed.SetAutoCycle(false);
        }

        if (windowCanvas != null)
            windowCanvas.gameObject.SetActive(true);

        if (windowPanel != null)
            windowPanel.SetActive(true);

        if (playerHead == null)
            FindPlayerHead();

        if (playerHead != null)
            PositionWindow();

        UpdateDisplay();
        nextTimeUpdate = Time.time + Mathf.Max(0.1f, timeUpdateIntervalSeconds);
    }

    public void CloseWindow()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (windowPanel != null)
            windowPanel.SetActive(false);

        if (windowCanvas != null)
            windowCanvas.gameObject.SetActive(false);
    }

    public void ShowNextCamera()
    {
        if (cameraFeed != null)
            cameraFeed.Next();

        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (!isOpen)
            return;

        if (cameraImage != null && cameraFeed != null)
            cameraImage.texture = cameraFeed.GetCurrentFrame();

        UpdateInfoTextOnly();
    }

    private void UpdateInfoTextOnly()
    {
        if (infoText == null)
            return;

        int index = cameraFeed != null ? cameraFeed.CurrentIndex : 0;
        string cameraLabel;

        if (cameraNames != null && index >= 0 && index < cameraNames.Length && !string.IsNullOrEmpty(cameraNames[index]))
        {
            cameraLabel = cameraNames[index];
        }
        else
        {
            cameraLabel = $"Камера {index + 1}";
        }

        DateTime now = DateTime.Now;
        infoText.text =
            $"<size={fontSize + 2}><b>{cameraLabel}</b></size>\n" +
            $"<size={fontSize - 2}>{now:dd.MM.yyyy HH:mm:ss}</size>";
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();

        if (closeButtonInteractable != null)
        {
            closeButtonInteractable.selectEntered.RemoveAllListeners();
            closeButtonInteractable.activated.RemoveAllListeners();
        }
    }
}

