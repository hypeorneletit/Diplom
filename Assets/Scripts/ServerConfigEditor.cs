using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Редактор конфигурации сервера внутри интерфейс-комнаты.
/// Позволяет менять имя сервера и вручную задавать его статус.
/// Все изменения сразу уходят в MonitoringDataService и отображаются на мониторах.
/// </summary>
public class ServerConfigEditor : MonoBehaviour
{
    [Header("Server Index")]
    [SerializeField] private int serverIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Dropdown statusDropdown; // 0=Авто,1=Норма,2=Предупреждение,3=Критично
    [SerializeField] private Slider cpuLoadSlider;        // Ручная нагрузка CPU (0-100)

    private void Start()
    {
        if (MonitoringDataService.Instance == null)
            return;

        var data = MonitoringDataService.Instance.GetServerData(serverIndex);
        if (data == null)
            return;

        // Имя
        if (nameInput != null)
        {
            nameInput.text = string.IsNullOrEmpty(data.displayName)
                ? $"Сервер {serverIndex + 1}"
                : data.displayName;
        }

        // Статус
        if (statusDropdown != null)
        {
            if (!data.manualOverride)
            {
                statusDropdown.value = 0; // Авто
            }
            else
            {
                switch (data.manualStatus)
                {
                    case MonitoringDataService.ServerStatus.Normal:
                        statusDropdown.value = 1;
                        break;
                    case MonitoringDataService.ServerStatus.Warning:
                        statusDropdown.value = 2;
                        break;
                    case MonitoringDataService.ServerStatus.Critical:
                        statusDropdown.value = 3;
                        break;
                }
            }
        }

        // CPU слайдер
        if (cpuLoadSlider != null)
        {
            if (data.manualCpuOverride)
            {
                cpuLoadSlider.value = data.manualCpuLoad;
            }
            else
            {
                cpuLoadSlider.value = data.cpuLoad;
            }
        }
    }

    /// <summary>
    /// Вызывается кнопкой "ПРИМЕНИТЬ" в интерфейс-комнате.
    /// </summary>
    public void ApplyChanges()
    {
        if (MonitoringDataService.Instance == null)
            return;

        // Имя
        if (nameInput != null)
        {
            MonitoringDataService.Instance.SetServerDisplayName(serverIndex, nameInput.text);
        }

        // Статус (override влияет только на статус, а не на CPU)
        bool statusOverrideOn = false;
        MonitoringDataService.ServerStatus status = MonitoringDataService.ServerStatus.Normal;

        if (statusDropdown != null)
        {
            statusOverrideOn = statusDropdown.value != 0;

            switch (statusDropdown.value)
            {
                case 1:
                    status = MonitoringDataService.ServerStatus.Normal;
                    break;
                case 2:
                    status = MonitoringDataService.ServerStatus.Warning;
                    break;
                case 3:
                    status = MonitoringDataService.ServerStatus.Critical;
                    break;
            }

            MonitoringDataService.Instance.SetServerManualStatus(serverIndex, statusOverrideOn, status);
        }

        // CPU: слайдер всегда включает ручной override нагрузки,
        // независимо от режима статуса
        if (cpuLoadSlider != null)
        {
            MonitoringDataService.Instance.SetServerManualCpuLoad(
                serverIndex,
                true,
                cpuLoadSlider.value);
        }
    }
}

