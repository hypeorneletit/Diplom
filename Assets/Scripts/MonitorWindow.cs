using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Окно монитора.
/// </summary>
public class MonitorWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas windowCanvas;
    [SerializeField] private TextMeshProUGUI serverRoomTempText;
    [SerializeField] private TextMeshProUGUI serverListText;
    [SerializeField] private GameObject windowPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private XRSimpleInteractable closeButtonInteractable;

    [Header("Window Settings")]
    [SerializeField] private float windowDistance = 0.6f;
    [SerializeField] private Vector2 windowSize = new Vector2(0.5f, 0.65f);
    [SerializeField] private float windowHeight = 0.1f;
    [SerializeField] private float fontSize = 24f;

    private bool isOpen = false;
    private Transform playerHead;
    private RectTransform canvasRect;
    private BoxCollider buttonCollider;

    private void Awake()
    {
        if (windowPanel != null)
        {
            windowPanel.SetActive(false);
        }

        if (windowCanvas != null)
        {
            canvasRect = windowCanvas.GetComponent<RectTransform>();
            // Отключаем Canvas при инициализации, чтобы окно было скрыто
            windowCanvas.gameObject.SetActive(false);
        }

        if (serverRoomTempText != null)
        {
            serverRoomTempText.gameObject.SetActive(false);
        }

        if (serverListText != null)
        {
            serverListText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        FindPlayerHead();
        SetupCanvas();
        ConfigureTexts();

        // Кнопка закрытия необязательна: окно закрывается повторным нажатием на монитор
        if (closeButton != null)
        {
            SetupCloseButton();
        }
    }

    private void SetupCloseButton()
    {
        if (closeButton == null)
        {
            Debug.LogWarning("[MonitorWindow] Close button not assigned!");
            return;
        }

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() =>
        {
            Debug.Log("[MonitorWindow] Close button clicked via UI!");
            CloseWindow();
        });

        var buttonTextTMP = closeButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonTextTMP != null)
        {
            buttonTextTMP.text = "✕ ЗАКРЫТЬ";
            buttonTextTMP.fontSize = 20f;
            buttonTextTMP.color = Color.white;
        }
        else
        {
            var buttonTextLegacy = closeButton.GetComponentInChildren<UnityEngine.UI.Text>();
            if (buttonTextLegacy != null)
            {
                buttonTextLegacy.text = "✕ ЗАКРЫТЬ";
                buttonTextLegacy.fontSize = 20;
                buttonTextLegacy.color = Color.white;
            }
        }

        var colors = closeButton.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        closeButton.colors = colors;

        if (closeButtonInteractable == null)
        {
            closeButtonInteractable = closeButton.GetComponent<XRSimpleInteractable>();
        }

        if (closeButtonInteractable == null)
        {
            closeButtonInteractable = closeButton.gameObject.AddComponent<XRSimpleInteractable>();
            Debug.Log("[MonitorWindow] Added XRSimpleInteractable to close button");
        }

        // Коллайдер для XR
        buttonCollider = closeButton.GetComponent<BoxCollider>();
        if (buttonCollider == null)
        {
            var anyCollider = closeButton.GetComponent<Collider>();
            if (anyCollider != null)
            {
                Debug.LogWarning($"[MonitorWindow] Found non-BoxCollider: {anyCollider.GetType().Name}, removing it");
                Destroy(anyCollider);
            }

            var rectTransform = closeButton.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                buttonCollider = closeButton.gameObject.AddComponent<BoxCollider>();

                Vector3 worldSize = rectTransform.TransformVector(new Vector3(rectTransform.rect.width, rectTransform.rect.height, 100f));
                float width = Mathf.Max(Mathf.Abs(worldSize.x), 0.30f);
                float height = Mathf.Max(Mathf.Abs(worldSize.y), 0.08f);
                float depth = Mathf.Max(Mathf.Abs(worldSize.z), 0.30f);

                buttonCollider.size = new Vector3(width, height, depth);
                buttonCollider.isTrigger = true;
                buttonCollider.center = Vector3.zero;

                Debug.Log($"[MonitorWindow] Added BoxCollider to close button: size={buttonCollider.size}");
            }
        }
        else
        {
            buttonCollider.size = new Vector3(
                Mathf.Max(buttonCollider.size.x, 0.3f),
                Mathf.Max(buttonCollider.size.y, 0.08f),
                Mathf.Max(buttonCollider.size.z, 0.3f)
            );
            buttonCollider.isTrigger = true;
            buttonCollider.enabled = true;
        }

        closeButton.gameObject.SetActive(true);
        closeButtonInteractable.enabled = true;
        closeButtonInteractable.interactionLayers = InteractionLayerMask.GetMask("Default");

        closeButtonInteractable.selectEntered.RemoveAllListeners();
        closeButtonInteractable.selectEntered.AddListener((args) =>
        {
            Debug.Log("[MonitorWindow] Close button selectEntered!");
            CloseWindow();
        });

        closeButtonInteractable.activated.RemoveAllListeners();
        closeButtonInteractable.activated.AddListener((args) =>
        {
            Debug.Log("[MonitorWindow] Close button activated!");
            CloseWindow();
        });

        closeButtonInteractable.hoverEntered.RemoveAllListeners();
        closeButtonInteractable.hoverEntered.AddListener((args) =>
        {
            Debug.Log("[MonitorWindow] Close button hoverEntered!");
        });

        var finalCheckCollider = closeButton.GetComponent<Collider>();
        Debug.Log($"[MonitorWindow] Close button setup complete. Collider exists: {finalCheckCollider != null}, Collider enabled: {(finalCheckCollider != null ? finalCheckCollider.enabled : false)}, Collider type: {(finalCheckCollider != null ? finalCheckCollider.GetType().Name : "None")}, Interactable enabled: {closeButtonInteractable.enabled}");
    }

    private void FindPlayerHead()
    {
        if (Camera.main != null)
        {
            playerHead = Camera.main.transform;
            return;
        }

        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in cameras)
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
        if (serverRoomTempText != null)
        {
            serverRoomTempText.fontSize = fontSize + 4;
            serverRoomTempText.color = Color.white;
            serverRoomTempText.alignment = TextAlignmentOptions.TopLeft;
            serverRoomTempText.enableWordWrapping = false;
            serverRoomTempText.overflowMode = TextOverflowModes.Overflow;

            RectTransform rect = serverRoomTempText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 0.8f);
                rect.anchorMax = new Vector2(1, 1f);
                rect.offsetMin = new Vector2(15, 0);
                rect.offsetMax = new Vector2(-15, -5);
            }
        }

        if (serverListText != null)
        {
            serverListText.fontSize = fontSize;
            serverListText.color = Color.white;
            serverListText.alignment = TextAlignmentOptions.TopLeft;
            serverListText.enableWordWrapping = true;
            serverListText.overflowMode = TextOverflowModes.Overflow;

            RectTransform rect = serverListText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 0.15f);
                rect.anchorMax = new Vector2(1, 0.8f);
                rect.offsetMin = new Vector2(15, 15);
                rect.offsetMax = new Vector2(-15, -5);
            }
        }
    }

    public void OpenWindow()
    {
        if (isOpen)
            return;

        isOpen = true;

        if (windowCanvas != null)
        {
            windowCanvas.gameObject.SetActive(true);
        }

        if (windowPanel != null)
        {
            windowPanel.SetActive(true);
        }

        if (serverRoomTempText != null)
        {
            serverRoomTempText.gameObject.SetActive(true);
        }

        if (serverListText != null)
        {
            serverListText.gameObject.SetActive(true);
        }

        if (playerHead != null && windowCanvas != null)
        {
            PositionWindow();
        }
        else
        {
            FindPlayerHead();
            if (playerHead != null)
            {
                PositionWindow();
            }
        }

        UpdateDisplay();

        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnDataUpdated += UpdateDisplay;
        }
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

    public void CloseWindow(SelectEnterEventArgs args = null)
    {
        CloseWindow();
    }

    public bool IsOpen => isOpen;

    public void CloseWindow()
    {
        if (!isOpen)
            return;

        Debug.Log("[MonitorWindow] Closing window");
        isOpen = false;

        if (windowPanel != null)
        {
            windowPanel.SetActive(false);
        }

        if (windowCanvas != null)
        {
            windowCanvas.gameObject.SetActive(false);
        }

        if (serverRoomTempText != null)
        {
            serverRoomTempText.gameObject.SetActive(false);
        }

        if (serverListText != null)
        {
            serverListText.gameObject.SetActive(false);
        }

        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnDataUpdated -= UpdateDisplay;
        }
    }

    private void UpdateDisplay()
    {
        if (!isOpen)
            return;

        UpdateServerRoomTemperature();
        UpdateServers();
    }

    private void UpdateServerRoomTemperature()
    {
        if (serverRoomTempText != null && MonitoringDataService.Instance != null)
        {
            float temp = MonitoringDataService.Instance.GetServerRoomTemperature();
            serverRoomTempText.text = $"<size={fontSize + 4}><b>Температура серверной: {temp:F1}°C</b></size>";
        }
    }

    private void UpdateServers()
    {
        if (serverListText == null || MonitoringDataService.Instance == null)
            return;

        MonitoringDataService.ServerData[] servers = MonitoringDataService.Instance.GetAllServerData();

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<size={fontSize}><b>СЕРВЕРЫ:</b></size>");
        sb.AppendLine("");

        for (int i = 0; i < servers.Length; i++)
        {
            var server = servers[i];
            string statusText = GetStatusText(server.status);
            string statusColor = GetStatusColor(server.status);

            sb.AppendLine($"<size={fontSize - 2}>Сервер {i + 1}: {server.temperature:F1}°C | CPU: {server.cpuLoad:F0}% | <color={statusColor}>{statusText}</color></size>");
        }

        serverListText.text = sb.ToString();
    }

    private string GetStatusText(MonitoringDataService.ServerStatus status)
    {
        switch (status)
        {
            case MonitoringDataService.ServerStatus.Normal:
                return "Норма";
            case MonitoringDataService.ServerStatus.Warning:
                return "Предупреждение";
            case MonitoringDataService.ServerStatus.Critical:
                return "Критично";
            default:
                return "Неизвестно";
        }
    }

    private string GetStatusColor(MonitoringDataService.ServerStatus status)
    {
        switch (status)
        {
            case MonitoringDataService.ServerStatus.Normal:
                return "#00FF00";
            case MonitoringDataService.ServerStatus.Warning:
                return "#FFFF00";
            case MonitoringDataService.ServerStatus.Critical:
                return "#FF0000";
            default:
                return "#FFFFFF";
        }
    }

    private void OnDestroy()
    {
        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnDataUpdated -= UpdateDisplay;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
        }

        if (closeButtonInteractable != null)
        {
            closeButtonInteractable.selectEntered.RemoveAllListeners();
            closeButtonInteractable.activated.RemoveAllListeners();
            closeButtonInteractable.hoverEntered.RemoveAllListeners();
        }
    }
}
