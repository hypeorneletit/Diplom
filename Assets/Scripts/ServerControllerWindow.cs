using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Окно управления сервером - отображает информацию и позволяет взаимодействовать с конкретным сервером
/// Открывается коротким нажатием, закрывается длительным нажатием (как у камер наблюдения)
/// </summary>
public class ServerControllerWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas windowCanvas;
    [SerializeField] private TextMeshProUGUI serverNameText;
    [SerializeField] private TextMeshProUGUI serverStatusText;
    [SerializeField] private TextMeshProUGUI serverDetailsText;
    [SerializeField] private TextMeshProUGUI actionHintText;
    [SerializeField] private GameObject windowPanel;

    [Header("Window Settings")]
    [SerializeField] private float windowDistance = 0.6f;
    [SerializeField] private Vector2 windowSize = new Vector2(0.55f, 0.75f);
    [SerializeField] private float windowHeight = 0.1f;
    [SerializeField] private float fontSize = 24f;

    [Header("Server Settings")]
    [SerializeField] private int serverIndex = 0; // Индекс сервера (0-3)
    [SerializeField] private string serverDisplayName = "Сервер";

    private bool isOpen = false;
    private Transform playerHead;
    private RectTransform canvasRect;
    private Coroutine updateCoroutine;

    // Состояние сервера
    private bool isServerOnline = true;
    private bool isServerRebooting = false;
    private float rebootProgress = 0f;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (windowPanel != null)
        {
            windowPanel.SetActive(false);
        }

        if (windowCanvas != null)
        {
            canvasRect = windowCanvas.GetComponent<RectTransform>();
            windowCanvas.gameObject.SetActive(false);
        }

        if (serverNameText != null)
        {
            serverNameText.gameObject.SetActive(false);
        }

        if (serverStatusText != null)
        {
            serverStatusText.gameObject.SetActive(false);
        }

        if (serverDetailsText != null)
        {
            serverDetailsText.gameObject.SetActive(false);
        }

        if (actionHintText != null)
        {
            actionHintText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        FindPlayerHead();
        SetupCanvas();
        ConfigureTexts();
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
        // Название сервера - центрированное сверху
        if (serverNameText != null)
        {
            serverNameText.fontSize = 30f;
            serverNameText.color = Color.white;
            serverNameText.alignment = TextAlignmentOptions.Top;
            serverNameText.enableWordWrapping = false;
            serverNameText.overflowMode = TextOverflowModes.Overflow;

            RectTransform rect = serverNameText.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Центрирование по горизонтали, верхняя часть окна
                rect.anchorMin = new Vector2(0.5f, 0.85f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(500f, 50f);
                rect.anchoredPosition = new Vector2(0f, 0f);
            }
        }

        // Статус сервера - слева сверху
        if (serverStatusText != null)
        {
            serverStatusText.fontSize = 26f;
            serverStatusText.color = Color.white;
            serverStatusText.alignment = TextAlignmentOptions.TopLeft;
            serverStatusText.enableWordWrapping = false;
            serverStatusText.overflowMode = TextOverflowModes.Overflow;

            RectTransform rect = serverStatusText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0.7f);
                rect.anchorMax = new Vector2(1f, 0.85f);
                rect.sizeDelta = Vector2.zero;
                rect.offsetMin = new Vector2(15f, 0f);
                rect.offsetMax = new Vector2(-15f, -5f);
            }
        }

        // Детали сервера - основная область
        if (serverDetailsText != null)
        {
            serverDetailsText.fontSize = 24f;
            serverDetailsText.color = Color.white;
            serverDetailsText.alignment = TextAlignmentOptions.TopLeft;
            serverDetailsText.enableWordWrapping = true;
            serverDetailsText.overflowMode = TextOverflowModes.Overflow;

            RectTransform rect = serverDetailsText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0.25f);
                rect.anchorMax = new Vector2(1f, 0.7f);
                rect.sizeDelta = Vector2.zero;
                rect.offsetMin = new Vector2(15f, 15f);
                rect.offsetMax = new Vector2(-15f, -5f);
            }
        }

        // Подсказка по действиям - снизу слева
        if (actionHintText != null)
        {
            actionHintText.fontSize = 20f;
            actionHintText.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
            actionHintText.alignment = TextAlignmentOptions.BottomLeft;
            actionHintText.enableWordWrapping = true;
            actionHintText.overflowMode = TextOverflowModes.Overflow;

            RectTransform rect = actionHintText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0.25f);
                rect.sizeDelta = Vector2.zero;
                rect.offsetMin = new Vector2(15f, 15f);
                rect.offsetMax = new Vector2(-15f, -5f);
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

        if (serverNameText != null)
        {
            serverNameText.gameObject.SetActive(true);
        }

        if (serverStatusText != null)
        {
            serverStatusText.gameObject.SetActive(true);
        }

        if (serverDetailsText != null)
        {
            serverDetailsText.gameObject.SetActive(true);
        }

        if (actionHintText != null)
        {
            actionHintText.gameObject.SetActive(true);
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
        StartUpdateCoroutine();

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

    public void CloseWindow()
    {
        if (!isOpen)
            return;

        Debug.Log($"[ServerControllerWindow] Closing window for server {serverIndex}");
        isOpen = false;

        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }

        if (windowPanel != null)
        {
            windowPanel.SetActive(false);
        }

        if (windowCanvas != null)
        {
            windowCanvas.gameObject.SetActive(false);
        }

        if (serverNameText != null)
        {
            serverNameText.gameObject.SetActive(false);
        }

        if (serverStatusText != null)
        {
            serverStatusText.gameObject.SetActive(false);
        }

        if (serverDetailsText != null)
        {
            serverDetailsText.gameObject.SetActive(false);
        }

        if (actionHintText != null)
        {
            actionHintText.gameObject.SetActive(false);
        }

        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnDataUpdated -= UpdateDisplay;
        }
    }

    private void StartUpdateCoroutine()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
        updateCoroutine = StartCoroutine(UpdateDisplayCoroutine());
    }

    private IEnumerator UpdateDisplayCoroutine()
    {
        while (isOpen)
        {
            UpdateDisplay();
            yield return new WaitForSeconds(0.5f); // Обновление каждые 0.5 секунды
        }
    }

    private void UpdateDisplay()
    {
        if (!isOpen)
            return;

        UpdateServerName();
        UpdateServerStatus();
        UpdateServerDetails();
        UpdateActionHint();
    }

    private void UpdateServerName()
    {
        if (serverNameText != null)
        {
            string name = string.IsNullOrEmpty(serverDisplayName) ? $"Сервер {serverIndex + 1}" : serverDisplayName;
            serverNameText.text = $"<size=30><b>{name}</b></size>";
        }
    }

    private void UpdateServerStatus()
    {
        if (serverStatusText == null)
            return;

        if (isServerRebooting)
        {
            serverStatusText.text = $"<size=26><color=#FFFF00><b>ПЕРЕЗАГРУЗКА... {rebootProgress:F0}%</b></color></size>";
        }
        else if (isServerOnline)
        {
            if (MonitoringDataService.Instance != null)
            {
                var serverData = MonitoringDataService.Instance.GetServerData(serverIndex);
                if (serverData != null)
                {
                    string statusColor = GetStatusColor(serverData.status);
                    string statusText = GetStatusText(serverData.status);
                    serverStatusText.text = $"<size=26><color={statusColor}><b>Статус: {statusText}</b></color></size>";
                }
                else
                {
                    serverStatusText.text = $"<size=26><color=#00FF00><b>Статус: Онлайн</b></color></size>";
                }
            }
            else
            {
                serverStatusText.text = $"<size=26><color=#00FF00><b>Статус: Онлайн</b></color></size>";
            }
        }
        else
        {
            serverStatusText.text = $"<size=26><color=#FF0000><b>Статус: Офлайн</b></color></size>";
        }
    }

    private void UpdateServerDetails()
    {
        if (serverDetailsText == null)
            return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (isServerRebooting)
        {
            sb.AppendLine($"<size=24><b>Перезагрузка сервера...</b></size>");
            sb.AppendLine($"<size=22>Пожалуйста, подождите...</size>");
        }
        else if (!isServerOnline)
        {
            sb.AppendLine($"<size=24><b>Сервер выключен</b></size>");
            sb.AppendLine($"<size=22>Нет данных для отображения</size>");
        }
        else if (MonitoringDataService.Instance != null)
        {
            var serverData = MonitoringDataService.Instance.GetServerData(serverIndex);
            if (serverData != null)
            {
                sb.AppendLine($"<size=24><b>Температура:</b> {serverData.temperature:F1}°C</size>");
                sb.AppendLine($"<size=24><b>Нагрузка CPU:</b> {serverData.cpuLoad:F0}%</size>");
                sb.AppendLine("");
                sb.AppendLine($"<size=22>Время работы: {Time.time:F0}с</size>");
                sb.AppendLine($"<size=22>Индекс: {serverIndex}</size>");
            }
            else
            {
                sb.AppendLine($"<size=24><b>Данные недоступны</b></size>");
            }
        }
        else
        {
            sb.AppendLine($"<size=24><b>Сервис мониторинга недоступен</b></size>");
        }

        serverDetailsText.text = sb.ToString();
    }

    private void UpdateActionHint()
    {
        if (actionHintText == null)
            return;

        if (isServerRebooting)
        {
            actionHintText.text = $"<size=20><i>Перезагрузка в процессе...</i></size>";
        }
        else if (isServerOnline)
        {
            actionHintText.text = $"<size=20><i>💡 Подсказка: Длительное нажатие для закрытия</i></size>";
        }
        else
        {
            actionHintText.text = $"<size=20><i>Сервер выключен</i></size>";
        }
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

        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }
}
