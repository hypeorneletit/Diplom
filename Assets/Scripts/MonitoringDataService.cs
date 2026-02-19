using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// Сервис генерации данных мониторинга для 4 серверов
/// Использует Perlin Noise для плавных колебаний
/// </summary>
public class MonitoringDataService : MonoBehaviour
{
    public static MonitoringDataService Instance { get; private set; }

    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 2f;

    [Header("Temperature Ranges")]
    [SerializeField] private float serverTempMin = 35f;
    [SerializeField] private float serverTempMax = 90f;
    [SerializeField] private float roomTempMin = 20f;
    [SerializeField] private float roomTempMax = 30f;

    [Header("CPU Load Ranges")]
    [SerializeField] private float cpuLoadMin = 20f;
    [SerializeField] private float cpuLoadMax = 100f;

    private float timeOffset = 0f;
    private ServerData[] servers = new ServerData[4];
    private float serverRoomTemperature;
    private float mainRoomTemperature;

    public event Action OnDataUpdated;
    public event Action<int, ServerStatus, ServerStatus> OnServerStatusChanged;

    [Serializable]
    public class ServerData
    {
        public float temperature;
        public float cpuLoad;
        public ServerStatus status;

        public ServerData()
        {
            temperature = 0f;
            cpuLoad = 0f;
            status = ServerStatus.Normal;
        }
    }

    public enum ServerStatus
    {
        Normal,
        Warning,
        Critical
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"MonitoringDataService already exists. Destroying duplicate on {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Инициализация серверов
        for (int i = 0; i < servers.Length; i++)
        {
            servers[i] = new ServerData();
        }

        timeOffset = UnityEngine.Random.Range(0f, 1000f);
        
        // Первоначальное обновление данных
        UpdateAllData();
    }

    private void Start()
    {
        StartCoroutine(UpdateDataCoroutine());
    }

    private IEnumerator UpdateDataCoroutine()
    {
        while (true)
        {
            UpdateAllData();
            OnDataUpdated?.Invoke();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void UpdateAllData()
    {
        float currentTime = Time.time + timeOffset;

        // Обновление данных серверов
        for (int i = 0; i < servers.Length; i++)
        {
            ServerStatus oldStatus = servers[i].status;

            // Используем Perlin Noise для плавных колебаний
            float noiseX = currentTime * 0.1f;
            float noiseY = i * 10f;
            float noiseValue = Mathf.PerlinNoise(noiseX, noiseY);

            // Температура сервера
            servers[i].temperature = Mathf.Lerp(serverTempMin, serverTempMax, noiseValue);

            // Нагрузка CPU (слегка другой паттерн)
            float cpuNoiseX = currentTime * 0.15f;
            float cpuNoiseY = i * 15f;
            float cpuNoiseValue = Mathf.PerlinNoise(cpuNoiseX, cpuNoiseY);
            servers[i].cpuLoad = Mathf.Lerp(cpuLoadMin, cpuLoadMax, cpuNoiseValue);

            // Определение статуса
            servers[i].status = DetermineStatus(servers[i].temperature);

            // Событие при изменении статуса
            if (oldStatus != servers[i].status)
            {
                OnServerStatusChanged?.Invoke(i, oldStatus, servers[i].status);
            }
        }

        // Температура серверной (зависит от среднего значения серверов)
        float avgServerTemp = 0f;
        for (int i = 0; i < servers.Length; i++)
        {
            avgServerTemp += servers[i].temperature;
        }
        avgServerTemp /= servers.Length;
        serverRoomTemperature = Mathf.Lerp(roomTempMin, roomTempMax + 10f, (avgServerTemp - serverTempMin) / (serverTempMax - serverTempMin));
        serverRoomTemperature = Mathf.Clamp(serverRoomTemperature, roomTempMin, roomTempMax + 15f);

        // Температура главной комнаты (более стабильная)
        float mainRoomNoise = Mathf.PerlinNoise(currentTime * 0.05f, 100f);
        mainRoomTemperature = Mathf.Lerp(roomTempMin, roomTempMax, mainRoomNoise);
    }

    private ServerStatus DetermineStatus(float temperature)
    {
        if (temperature >= 85f)
            return ServerStatus.Critical;
        else if (temperature >= 70f)
            return ServerStatus.Warning;
        else
            return ServerStatus.Normal;
    }

    // Публичные методы для получения данных
    public ServerData GetServerData(int index)
    {
        if (index >= 0 && index < servers.Length)
            return servers[index];
        return null;
    }

    public ServerData[] GetAllServerData()
    {
        return servers;
    }

    public float GetServerRoomTemperature()
    {
        return serverRoomTemperature;
    }

    public float GetMainRoomTemperature()
    {
        return mainRoomTemperature;
    }
}