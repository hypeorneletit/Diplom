using UnityEngine;

/// <summary>
/// Автоматическая инициализация всех сервисов системы
/// Добавьте этот компонент на GameObject в сцене
/// </summary>
public class ServiceInitializer : MonoBehaviour
{
    [Header("Auto Create Services")]
    [SerializeField] private bool autoCreateServices = true;
    [SerializeField] private string servicesParentName = "Services";

    private void Awake()
    {
        if (autoCreateServices)
        {
            InitializeServices();
        }
    }

    private void InitializeServices()
    {
        // Создание родительского объекта для сервисов
        GameObject servicesParent = GameObject.Find(servicesParentName);
        if (servicesParent == null)
        {
            servicesParent = new GameObject(servicesParentName);
        }

        // Инициализация MonitoringDataService
        if (MonitoringDataService.Instance == null)
        {
            GameObject monitoringService = new GameObject("MonitoringDataService");
            monitoringService.transform.SetParent(servicesParent.transform);
            monitoringService.AddComponent<MonitoringDataService>();
        }

        // Инициализация EventLogService
        if (EventLogService.Instance == null)
        {
            GameObject eventLogService = new GameObject("EventLogService");
            eventLogService.transform.SetParent(servicesParent.transform);
            eventLogService.AddComponent<EventLogService>();
        }

        // Инициализация AlarmService
        if (AlarmService.Instance == null)
        {
            GameObject alarmService = new GameObject("AlarmService");
            alarmService.transform.SetParent(servicesParent.transform);
            alarmService.AddComponent<AlarmService>();
        }
    }
}