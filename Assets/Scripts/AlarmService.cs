using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// Сервис аварийной сигнализации с красной подсветкой и миганием.
/// </summary>
public class AlarmService : MonoBehaviour
{
    public static AlarmService Instance { get; private set; }

    [Header("Alarm Settings")]
    [SerializeField] private float blinkInterval = 0.5f;
    [SerializeField] private Color alarmColor = Color.red;
    [SerializeField] private float alarmIntensity = 2f;

    private bool isAlarmActive = false;

    public event Action<bool> OnAlarmStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"AlarmService already exists. Destroying duplicate on {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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

        // Проверка начального состояния с задержкой
        Invoke(nameof(CheckAlarmState), 0.5f);
    }

    private void SubscribeToMonitoringService()
    {
        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.Instance.OnServerStatusChanged += OnServerStatusChanged;
            CheckAlarmState();
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
        CheckAlarmState();
    }

    private void CheckAlarmState()
    {
        if (MonitoringDataService.Instance == null)
            return;

        bool hasCritical = false;
        MonitoringDataService.ServerData[] servers = MonitoringDataService.Instance.GetAllServerData();

        foreach (var server in servers)
        {
            if (server.status == MonitoringDataService.ServerStatus.Critical)
            {
                hasCritical = true;
                break;
            }
        }

        if (hasCritical != isAlarmActive)
        {
            isAlarmActive = hasCritical;
            OnAlarmStateChanged?.Invoke(isAlarmActive);

            if (isAlarmActive)
            {
                StartCoroutine(BlinkCoroutine());
            }
            else
            {
                StopAllCoroutines();
                SetAlarmVisualState(false);
            }
        }
    }

    private IEnumerator BlinkCoroutine()
    {
        bool state = false;

        while (isAlarmActive)
        {
            state = !state;
            SetAlarmVisualState(state);
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    private void SetAlarmVisualState(bool isOn)
    {
        // Уведомление всех зарегистрированных визуальных элементов.
        // Реализация/подписчики находятся в контроллерах UI.
    }

    public bool IsAlarmActive()
    {
        return isAlarmActive;
    }

    public Color GetAlarmColor()
    {
        return alarmColor;
    }

    public float GetAlarmIntensity()
    {
        return alarmIntensity;
    }
}
