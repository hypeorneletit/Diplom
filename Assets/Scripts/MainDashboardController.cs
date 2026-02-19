using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Контроллер главного экрана - ВСЕГДА ВИДИМАЯ ВЕРСИЯ КАК МОНИТОР.
/// </summary>
public class MainDashboardController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI serverRoomTempText;
    [SerializeField] private TextMeshProUGUI serverListText;

    [Header("Display Settings")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float fontSize = 32f;
    [SerializeField] private bool forceTextColor = true;

    private bool didAutobind = false;
    private Canvas mainCanvas;

    [Header("Trend Graph")]
    [SerializeField] private RectTransform temperatureGraphContainer;
    [SerializeField] private int maxTemperaturePoints = 120;
    [SerializeField] private float graphWidth = 800f;
    [SerializeField] private float graphHeight = 300f;
    [SerializeField] private Color axisColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color gridColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private int gridLinesX = 5;
    [SerializeField] private int gridLinesY = 5;
    [SerializeField] private float dataUpdateInterval = 1f; // Интервал обновления данных в секундах

    [Header("Font Settings")]
    [SerializeField] private TMP_FontAsset cyrillicFontOverride = null;

    private readonly List<float> temperatureHistory = new List<float>();
    private LineRenderer temperatureLineRenderer;
    private LineRenderer axisXLineRenderer;
    private LineRenderer axisYLineRenderer;
    private readonly List<LineRenderer> gridLineRenderers = new List<LineRenderer>();

    private TextMeshProUGUI graphTitleText;
    private TextMeshProUGUI axisXLabelText;
    private TextMeshProUGUI axisYLabelText;
    private readonly List<TextMeshProUGUI> axisXValueTexts = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> axisYValueTexts = new List<TextMeshProUGUI>();

    private GameObject graphParentObject;

    private float lastRoomTemperature = 0f;
    private float minTempDisplay = 0f;
    private float maxTempDisplay = 100f;

    private TMP_FontAsset cyrillicFontAsset = null;

    private void Start()
    {
        Debug.Log("[MainDashboardController] Start called");
        FixCanvasSize();
        AutoBindIfNeeded();
        ForceTextColors();
        FindCyrillicFont();
        InitializeTemperatureGraph();

        // НЕМЕДЛЕННОЕ обновление
        UpdateDisplay();

        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnDataUpdated += UpdateDisplay;
            Debug.Log("[MainDashboardController] Subscribed to MonitoringDataService updates");
        }
        else
        {
            Debug.LogWarning("[MainDashboardController] MonitoringDataService.Instance is null. Retrying in 1 second...");
            Invoke(nameof(RetryInitialization), 1f);
        }
    }

    private void FixCanvasSize()
    {
        mainCanvas = GetComponentInChildren<Canvas>(true);

        if (mainCanvas == null)
            mainCanvas = GetComponent<Canvas>();
        if (mainCanvas == null)
            mainCanvas = GetComponentInParent<Canvas>();

        if (mainCanvas == null)
        {
            Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
            foreach (var canvas in allCanvases)
            {
                string n = canvas.name.ToLowerInvariant();
                if (n.Contains("bigscreen") || n.Contains("big_screen") || n.Contains("screen") || n.Contains("canvas"))
                {
                    mainCanvas = canvas;
                    break;
                }
            }
        }

        if (mainCanvas == null)
        {
            Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
            if (allCanvases.Length > 0)
                mainCanvas = allCanvases[0];
        }

        if (mainCanvas == null)
        {
            Debug.LogError("[MainDashboardController] Canvas not found!");
            return;
        }

        mainCanvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            if (canvasRect.sizeDelta.y < 500f || canvasRect.sizeDelta.y > 2000f)
            {
                canvasRect.sizeDelta = new Vector2(1600f, 900f);
                Debug.Log("[MainDashboardController] Fixed Canvas sizeDelta to (1600, 900)");
            }

            Vector3 currentScale = canvasRect.localScale;
            if (currentScale.x > 0.05f || currentScale.x < 0.0005f ||
                currentScale.y != currentScale.x ||
                currentScale.z != currentScale.x)
            {
                canvasRect.localScale = Vector3.one * 0.001f;
                Debug.Log("[MainDashboardController] Fixed Canvas scale to (0.001, 0.001, 0.001)");
            }
        }
    }

    private void ForceTextColors()
    {
        var allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in allTexts)
        {
            if (text != null && forceTextColor)
            {
                text.color = textColor;
                if (text.fontSize < fontSize)
                    text.fontSize = fontSize;
            }
        }
    }

    private void AutoBindIfNeeded()
    {
        if (didAutobind)
            return;

        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (var t in texts)
        {
            var n = t.gameObject.name.ToLowerInvariant();

            if (serverRoomTempText == null && (n.Contains("temp") || n.Contains("temperature")))
                serverRoomTempText = t;

            if (serverListText == null && (n.Contains("server") || n.Contains("list")))
                serverListText = t;
        }

        if (serverRoomTempText == null && texts.Length > 0)
            serverRoomTempText = texts[0];

        if (serverListText == null && texts.Length > 1)
            serverListText = texts[1];

        didAutobind = true;
    }

    private void FindCyrillicFont()
    {
        // ВАЖНО: не трогаем шрифты элементов, которые ты настроил руками.
        // Шрифт меняем только если ты ЯВНО указал его в инспекторе.
        cyrillicFontAsset = cyrillicFontOverride;
    }

    private static bool ContainsCyrillic(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (char c in text)
        {
            if ((c >= 0x0400 && c <= 0x04FF) || (c >= 0x0500 && c <= 0x052F))
                return true;
        }

        return false;
    }

    private void InitializeTemperatureGraph()
    {
        if (temperatureGraphContainer == null)
        {
            var rects = GetComponentsInChildren<RectTransform>(true);
            foreach (var r in rects)
            {
                var n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("graph") || n.Contains("trend"))
                {
                    temperatureGraphContainer = r;
                    break;
                }
            }
        }

        if (temperatureGraphContainer == null)
        {
            Debug.LogError("[MainDashboardController] TemperatureGraphContainer not found!");
            return;
        }

        // Используем сам контейнер как родительский объект, не создаем новый TemperatureGraph
        graphParentObject = temperatureGraphContainer.gameObject;

        // Ищем или создаем только линию графика
        Transform existingLine = graphParentObject.transform.Find("TemperatureLine");
        if (existingLine != null)
        {
            temperatureLineRenderer = existingLine.GetComponent<LineRenderer>();
            if (temperatureLineRenderer == null)
                temperatureLineRenderer = existingLine.gameObject.AddComponent<LineRenderer>();
        }
        else
        {
            GameObject graphObject = new GameObject("TemperatureLine");
            graphObject.transform.SetParent(graphParentObject.transform, false);
            temperatureLineRenderer = graphObject.AddComponent<LineRenderer>();
        }

        if (temperatureLineRenderer.material == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                temperatureLineRenderer.material = new Material(shader);
        }
        temperatureLineRenderer.startWidth = 0.02f;
        temperatureLineRenderer.endWidth = 0.02f;
        temperatureLineRenderer.useWorldSpace = false;
        temperatureLineRenderer.startColor = Color.cyan;
        temperatureLineRenderer.endColor = Color.cyan;

        // Находим существующие элементы, не создаем новые
        FindExistingGraphElements();

        UpdateAxesPositions();
    }

    private void FindExistingGraphElements()
    {
        // Находим существующие оси
        Transform axisX = graphParentObject.transform.Find("AxisX");
        if (axisX != null)
        {
            axisXLineRenderer = axisX.GetComponent<LineRenderer>();
            if (axisXLineRenderer == null)
                axisXLineRenderer = axisX.gameObject.AddComponent<LineRenderer>();
            EnsureLineStyle(axisXLineRenderer, axisColor, 0.015f);
            axisXLineRenderer.positionCount = 2;
        }

        Transform axisY = graphParentObject.transform.Find("AxisY");
        if (axisY != null)
        {
            axisYLineRenderer = axisY.GetComponent<LineRenderer>();
            if (axisYLineRenderer == null)
                axisYLineRenderer = axisY.gameObject.AddComponent<LineRenderer>();
            EnsureLineStyle(axisYLineRenderer, axisColor, 0.015f);
            axisYLineRenderer.positionCount = 2;
        }

        // Находим существующие текстовые элементы
        graphTitleText = FindTmpText("GraphTitle");
        axisXLabelText = FindTmpText("AxisXLabel");
        axisYLabelText = FindTmpText("AxisYLabel");

        // Находим существующие метки значений
        FindExistingAxisValueLabels();

        // Находим существующие линии сетки
        FindExistingGridLines();
    }

    private TextMeshProUGUI FindTmpText(string name)
    {
        Transform existing = graphParentObject.transform.Find(name);
        if (existing != null)
        {
            var text = existing.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                ApplyFont(text);
                return text;
            }
        }
        return null;
    }

    private void FindExistingAxisValueLabels()
    {
        axisXValueTexts.Clear();
        axisYValueTexts.Clear();

        // Ищем метки оси X
        for (int i = 0; i <= gridLinesX; i++)
        {
            Transform existing = graphParentObject.transform.Find($"AxisXValue_{i}");
            if (existing != null)
            {
                var text = existing.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    ApplyFont(text);
                    axisXValueTexts.Add(text);
                }
            }
        }

        // Ищем метки оси Y
        for (int i = 0; i <= gridLinesY; i++)
        {
            Transform existing = graphParentObject.transform.Find($"AxisYValue_{i}");
            if (existing != null)
            {
                var text = existing.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    ApplyFont(text);
                    axisYValueTexts.Add(text);
                }
            }
        }
    }

    private void FindExistingGridLines()
    {
        gridLineRenderers.Clear();

        // Ищем линии сетки X
        for (int i = 1; i < gridLinesX; i++)
        {
            Transform existing = graphParentObject.transform.Find($"GridLineX_{i}");
            if (existing != null)
            {
                var lr = existing.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    EnsureLineStyle(lr, gridColor, 0.005f);
                    lr.positionCount = 2;
                    gridLineRenderers.Add(lr);
                }
            }
        }

        // Ищем линии сетки Y
        for (int i = 1; i < gridLinesY; i++)
        {
            Transform existing = graphParentObject.transform.Find($"GridLineY_{i}");
            if (existing != null)
            {
                var lr = existing.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    EnsureLineStyle(lr, gridColor, 0.005f);
                    lr.positionCount = 2;
                    gridLineRenderers.Add(lr);
                }
            }
        }
    }

    private void UpdateAxesPositions()
    {
        if (axisXLineRenderer == null || axisYLineRenderer == null)
            return;

        float halfWidth = graphWidth * 0.5f;
        float halfHeight = graphHeight * 0.5f;

        axisXLineRenderer.SetPosition(0, new Vector3(-halfWidth, -halfHeight, 0f));
        axisXLineRenderer.SetPosition(1, new Vector3(halfWidth, -halfHeight, 0f));

        axisYLineRenderer.SetPosition(0, new Vector3(-halfWidth, -halfHeight, 0f));
        axisYLineRenderer.SetPosition(1, new Vector3(-halfWidth, halfHeight, 0f));
    }


    private void ApplyFont(TextMeshProUGUI t)
    {
        if (t == null)
            return;

        // По умолчанию НЕ перезаписываем шрифт у уже настроенных объектов.
        // Если задан override — считаем это явным желанием применить шрифт.
        if (cyrillicFontAsset != null && t.font != cyrillicFontAsset)
        {
            t.font = cyrillicFontAsset;
        }
    }

    private static void EnsureLineStyle(LineRenderer lr, Color color, float width)
    {
        if (lr == null)
            return;

        if (lr.material == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                lr.material = new Material(shader);
        }

        lr.useWorldSpace = false;
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = width;
        lr.endWidth = width;
    }

    private void RetryInitialization()
    {
        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnDataUpdated += UpdateDisplay;
            UpdateDisplay();
            Debug.Log("[MainDashboardController] Retry successful - subscribed and updated");
        }
        else
        {
            Debug.LogError("[MainDashboardController] MonitoringDataService.Instance is still null. Make sure ServiceInitializer is in the scene!");
        }
    }

    private void OnDestroy()
    {
        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnDataUpdated -= UpdateDisplay;
        }
    }

    private void UpdateDisplay()
    {
        FixCanvasSize();
        AutoBindIfNeeded();
        ForceTextColors();

        if (MonitoringDataService.Instance == null)
        {
            if (serverRoomTempText != null)
                serverRoomTempText.text = "<color=#FFFFFF><size=36>ОШИБКА: Сервисы не инициализированы</size></color>";
            return;
        }

        var incidentController = IncidentReplayController.Instance;
        IncidentReplayController.Snapshot incidentSnapshot = null;
        if (incidentController != null && incidentController.ShowIncident)
            incidentSnapshot = incidentController.GetIncidentSnapshot();

        if (incidentSnapshot != null)
        {
            UpdateServerRoomTemperature(incidentSnapshot.roomTemperature);
            UpdateServerList(incidentSnapshot.servers);
        }
        else
        {
            UpdateServerRoomTemperature();
            UpdateServerList();
        }

        UpdateTemperatureGraph();
    }

    private void UpdateServerRoomTemperature()
    {
        if (serverRoomTempText != null && MonitoringDataService.Instance != null)
        {
            float temp = MonitoringDataService.Instance.GetServerRoomTemperature();
            UpdateServerRoomTemperature(temp);
        }
    }

    private void UpdateServerRoomTemperature(float temp)
    {
        lastRoomTemperature = temp;
        if (serverRoomTempText != null)
        {
            serverRoomTempText.color = Color.white;
            serverRoomTempText.text = $"<size={fontSize}><b>Температура серверной: {temp:F1}°C</b></size>";
        }
    }

    private void UpdateServerList()
    {
        if (MonitoringDataService.Instance == null)
            return;

        MonitoringDataService.ServerData[] servers = MonitoringDataService.Instance.GetAllServerData();
        UpdateServerList(servers);
    }

    private void UpdateServerList(MonitoringDataService.ServerData[] servers)
    {
        if (serverListText == null)
            return;

        if (servers == null || servers.Length == 0)
        {
            serverListText.text = "<size=28>Нет данных по серверам</size>";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<size={fontSize}><b>СЕРВЕРЫ:</b></size>");
        sb.AppendLine("");

        for (int i = 0; i < servers.Length; i++)
        {
            var server = servers[i];
            string statusText = GetStatusText(server.status);
            string statusColor = GetStatusColor(server.status);

            sb.AppendLine($"<size={fontSize - 4}>Сервер {i + 1}: {server.temperature:F1}°C | CPU: {server.cpuLoad:F0}% | <color={statusColor}>{statusText}</color></size>");
        }

        serverListText.color = Color.white;
        serverListText.text = sb.ToString();
    }

    private void UpdateTemperatureGraph()
    {
        if (temperatureLineRenderer == null)
            return;

        temperatureHistory.Add(lastRoomTemperature);
        if (temperatureHistory.Count > maxTemperaturePoints)
            temperatureHistory.RemoveAt(0);

        if (temperatureHistory.Count == 0)
            return;

        temperatureLineRenderer.positionCount = temperatureHistory.Count;

        float stepX = graphWidth / Mathf.Max(1, maxTemperaturePoints - 1);

        float minTemp = float.MaxValue;
        float maxTemp = float.MinValue;
        for (int i = 0; i < temperatureHistory.Count; i++)
        {
            float t = temperatureHistory[i];
            if (t < minTemp) minTemp = t;
            if (t > maxTemp) maxTemp = t;
        }

        if (Mathf.Approximately(minTemp, maxTemp))
        {
            minTemp -= 5f;
            maxTemp += 5f;
        }
        else
        {
            float range = maxTemp - minTemp;
            minTemp -= range * 0.1f;
            maxTemp += range * 0.1f;
        }

        minTempDisplay = minTemp;
        maxTempDisplay = maxTemp;

        // Используем реальное количество точек для правильного масштабирования
        float actualStepX = temperatureHistory.Count > 1 ? graphWidth / (temperatureHistory.Count - 1) : 0f;

        for (int i = 0; i < temperatureHistory.Count; i++)
        {
            float x = -graphWidth * 0.5f + i * actualStepX;
            float normalized = (temperatureHistory[i] - minTemp) / (maxTemp - minTemp);
            float y = -graphHeight * 0.5f + normalized * graphHeight;
            temperatureLineRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }

        // Обновляем метки на обеих осях динамически
        UpdateAxisXLabels();
        UpdateAxisYLabels();
    }

    private void UpdateAxisXLabels()
    {
        if (axisXValueTexts == null || axisXValueTexts.Count == 0)
            return;

        // Вычисляем реальное время истории на основе количества точек
        int actualHistoryCount = temperatureHistory.Count;
        float maxTimeSeconds = actualHistoryCount > 0 ? actualHistoryCount * dataUpdateInterval : maxTemperaturePoints * dataUpdateInterval;

        for (int i = 0; i < axisXValueTexts.Count; i++)
        {
            if (axisXValueTexts[i] == null)
                continue;

            // Вычисляем время для этой метки (от максимального времени до 0)
            float normalizedPosition = (float)i / Mathf.Max(1, axisXValueTexts.Count - 1);
            float timeValue = maxTimeSeconds * (1f - normalizedPosition); // Инвертируем: справа 0, слева maxTimeSeconds

            // Форматируем время: если больше 60 секунд, показываем минуты
            string timeText;
            if (timeValue >= 60f)
            {
                int minutes = Mathf.FloorToInt(timeValue / 60f);
                int seconds = Mathf.FloorToInt(timeValue % 60f);
                timeText = $"{minutes}м {seconds}с";
            }
            else
            {
                timeText = $"{timeValue:F0}с";
            }

            axisXValueTexts[i].text = timeText;
        }
    }

    private void UpdateAxisYLabels()
    {
        if (axisYValueTexts == null || axisYValueTexts.Count == 0)
            return;

        for (int i = 0; i < axisYValueTexts.Count; i++)
        {
            if (axisYValueTexts[i] == null)
                continue;

            // i = 0 — нижняя метка, i = последний — верхняя метка
            float normalizedValue = (float)i / Mathf.Max(1, axisYValueTexts.Count - 1);
            float tempValue = minTempDisplay + (maxTempDisplay - minTempDisplay) * normalizedValue;
            axisYValueTexts[i].text = $"{tempValue:F1}°C";
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
}
