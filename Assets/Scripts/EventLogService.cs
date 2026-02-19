using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Сервис лога событий с фиксированным размером очереди (100 записей)
/// </summary>
public class EventLogService : MonoBehaviour
{
    public static EventLogService Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int maxLogEntries = 100;

    private Queue<EventEntry> logQueue;
    private List<EventEntry> logList;

    public event Action<EventEntry> OnEventAdded;

    [Serializable]
    public class EventEntry
    {
        public string timestamp;
        public EventType eventType;
        public string message;

        public EventEntry(EventType type, string msg)
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            eventType = type;
            message = msg;
        }
    }

    public enum EventType
    {
        ServerStatusChange,
        TemperatureAlert,
        SystemStart,
        DataUpdate
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"EventLogService already exists. Destroying duplicate on {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        logQueue = new Queue<EventEntry>(maxLogEntries);
        logList = new List<EventEntry>(maxLogEntries);
    }

    private void Start()
    {
        // Подписка на события MonitoringDataService
        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnServerStatusChanged += OnServerStatusChanged;
        }
        else
        {
            // Повторная попытка подписки через небольшую задержку
            Invoke(nameof(SubscribeToMonitoringService), 0.5f);
        }

        // Системное событие запуска
        AddEvent(EventType.SystemStart, "Система мониторинга запущена");
    }

    private void SubscribeToMonitoringService()
    {
        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnServerStatusChanged += OnServerStatusChanged;
        }
    }

    private void OnDestroy()
    {
        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnServerStatusChanged -= OnServerStatusChanged;
        }
    }

    private void OnServerStatusChanged(int serverIndex, MonitoringDataService.ServerStatus oldStatus, MonitoringDataService.ServerStatus newStatus)
    {
        string statusText = GetStatusText(newStatus);
        string oldStatusText = GetStatusText(oldStatus);
        AddEvent(EventType.ServerStatusChange, $"Сервер {serverIndex + 1}: {oldStatusText} → {statusText}");
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

    public void AddEvent(EventType eventType, string message)
    {
        EventEntry entry = new EventEntry(eventType, message);

        // Добавление в очередь и список
        logQueue.Enqueue(entry);
        logList.Add(entry);

        // Ограничение размера
        if (logQueue.Count > maxLogEntries)
        {
            EventEntry removed = logQueue.Dequeue();
            logList.Remove(removed);
        }

        OnEventAdded?.Invoke(entry);
    }

    public List<EventEntry> GetRecentEvents(int count)
    {
        int takeCount = Mathf.Min(count, logList.Count);
        return logList.Skip(Mathf.Max(0, logList.Count - takeCount)).Take(takeCount).ToList();
    }

    public List<EventEntry> GetAllEvents()
    {
        return new List<EventEntry>(logList);
    }

    public int GetEventCount()
    {
        return logList.Count;
    }
}