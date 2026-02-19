using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Контроллер инцидентов: запоминает последний критический инцидент
/// и позволяет большому экрану показывать "картину мира" на момент аварии.
/// </summary>
public class IncidentReplayController : MonoBehaviour
{
    public static IncidentReplayController Instance { get; private set; }

    [System.Serializable]
    public class Snapshot
    {
        public float time;
        public float roomTemperature;
        public MonitoringDataService.ServerData[] servers;
    }

    [Header("History Settings")]
    [SerializeField] private float maxHistorySeconds = 600f; // просто ограничение по времени для снимков

    private readonly List<Snapshot> history = new List<Snapshot>();

    private Snapshot lastIncidentSnapshot;
    private bool hasIncident = false;

    // Включён ли показ инцидента на большом экране
    public bool ShowIncident { get; private set; } = false;

    public bool HasIncident => hasIncident;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"IncidentReplayController already exists. Destroying duplicate on {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnDataUpdated += OnDataUpdated;
            MonitoringDataService.Instance.OnServerStatusChanged += OnServerStatusChanged;
        }
        else
        {
            Invoke(nameof(SubscribeToMonitoringService), 0.5f);
        }
    }

    private void SubscribeToMonitoringService()
    {
        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnDataUpdated += OnDataUpdated;
            MonitoringDataService.Instance.OnServerStatusChanged += OnServerStatusChanged;
        }
    }

    private void OnDestroy()
    {
        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnDataUpdated -= OnDataUpdated;
            MonitoringDataService.Instance.OnServerStatusChanged -= OnServerStatusChanged;
        }
    }

    private void OnDataUpdated()
    {
        // Периодически очищаем историю от слишком старых записей
        float now = Time.time;
        while (history.Count > 0 && now - history[0].time > maxHistorySeconds)
        {
            history.RemoveAt(0);
        }

        // Сохраняем текущий снимок в историю (для потенциального использования в будущем)
        var snapshot = CaptureCurrentSnapshot();
        if (snapshot != null)
        {
            history.Add(snapshot);
        }
    }

    private void OnServerStatusChanged(int serverIndex, MonitoringDataService.ServerStatus oldStatus, MonitoringDataService.ServerStatus newStatus)
    {
        // При переходе в критический статус запоминаем снимок
        if (newStatus == MonitoringDataService.ServerStatus.Critical)
        {
            var snapshot = CaptureCurrentSnapshot();
            if (snapshot != null)
            {
                lastIncidentSnapshot = snapshot;
                hasIncident = true;
                Debug.Log($"[IncidentReplayController] Captured incident snapshot at time {snapshot.time}");
            }
        }
    }

    private Snapshot CaptureCurrentSnapshot()
    {
        if (MonitoringDataService.Instance == null)
            return null;

        Snapshot snapshot = new Snapshot();
        snapshot.time = Time.time;
        snapshot.roomTemperature = MonitoringDataService.Instance.GetServerRoomTemperature();

        var liveServers = MonitoringDataService.Instance.GetAllServerData();
        if (liveServers == null)
            return null;

        snapshot.servers = new MonitoringDataService.ServerData[liveServers.Length];
        for (int i = 0; i < liveServers.Length; i++)
        {
            snapshot.servers[i] = new MonitoringDataService.ServerData();
            snapshot.servers[i].temperature = liveServers[i].temperature;
            snapshot.servers[i].cpuLoad = liveServers[i].cpuLoad;
            snapshot.servers[i].status = liveServers[i].status;
        }

        return snapshot;
    }

    public void ToggleShowIncident()
    {
        if (!hasIncident)
        {
            Debug.LogWarning("[IncidentReplayController] No incident snapshot captured yet");
            return;
        }

        ShowIncident = !ShowIncident;
        Debug.Log($"[IncidentReplayController] ShowIncident set to {ShowIncident}");
    }

    public Snapshot GetIncidentSnapshot()
    {
        return hasIncident ? lastIncidentSnapshot : null;
    }
}

