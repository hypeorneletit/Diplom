using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер виртуальных камер видеонаблюдения серверной
/// </summary>
public class CameraFeedController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CameraFeed[] cameraFeeds = new CameraFeed[4];
    [SerializeField] private float imageChangeInterval = 3f;
    [SerializeField] private Texture2D[] normalTextures;
    [SerializeField] private Texture2D[] disturbedTextures;

    [Header("Effect Settings")]
    [SerializeField] private Material noiseMaterial;
    [SerializeField] private float criticalNoiseIntensity = 0.5f;
    [SerializeField] private Color criticalTint = new Color(0.5f, 0f, 0f, 1f);

    [System.Serializable]
    public class CameraFeed
    {
        public Renderer screenRenderer;
        public int associatedServerIndex; // Индекс связанного сервера (0-3)
        [HideInInspector]
        public Material screenMaterial;
        [HideInInspector]
        public bool isCritical = false;
    }

    private void Start()
    {
        InitializeCameraFeeds();
        StartCoroutine(UpdateCameraFeedsCoroutine());

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

    private void InitializeCameraFeeds()
    {
        for (int i = 0; i < cameraFeeds.Length; i++)
        {
            if (cameraFeeds[i].screenRenderer != null)
            {
                // Создание копии материала для каждой камеры
                cameraFeeds[i].screenMaterial = new Material(cameraFeeds[i].screenRenderer.material);
                cameraFeeds[i].screenRenderer.material = cameraFeeds[i].screenMaterial;
                cameraFeeds[i].associatedServerIndex = i; // Связь с сервером по индексу
            }
        }
    }

    private IEnumerator UpdateCameraFeedsCoroutine()
    {
        while (true)
        {
            UpdateAllCameraFeeds();
            yield return new WaitForSeconds(imageChangeInterval);
        }
    }

    private void UpdateAllCameraFeeds()
    {
        for (int i = 0; i < cameraFeeds.Length; i++)
        {
            UpdateCameraFeed(i);
        }
    }

    private void UpdateCameraFeed(int cameraIndex)
    {
        if (cameraIndex < 0 || cameraIndex >= cameraFeeds.Length)
            return;

        CameraFeed feed = cameraFeeds[cameraIndex];
        if (feed.screenMaterial == null)
            return;

        // Получение статуса связанного сервера
        bool isCritical = false;
        if (MonitoringDataService.Instance != null)
        {
            MonitoringDataService.ServerData server = MonitoringDataService.Instance.GetServerData(feed.associatedServerIndex);
            if (server != null)
            {
                isCritical = server.status == MonitoringDataService.ServerStatus.Critical;
                feed.isCritical = isCritical;
            }
        }

        // Выбор текстуры
        Texture2D[] texturesToUse = isCritical && disturbedTextures != null && disturbedTextures.Length > 0 
            ? disturbedTextures 
            : normalTextures;

        if (texturesToUse != null && texturesToUse.Length > 0)
        {
            // Случайная текстура из доступных
            int randomIndex = Random.Range(0, texturesToUse.Length);
            feed.screenMaterial.mainTexture = texturesToUse[randomIndex];
        }

        // Применение эффектов для критического состояния
        if (isCritical)
        {
            ApplyCriticalEffects(feed);
        }
        else
        {
            RemoveCriticalEffects(feed);
        }
    }

    private void ApplyCriticalEffects(CameraFeed feed)
    {
        if (feed.screenMaterial == null)
            return;

        // Затемнение
        feed.screenMaterial.color = criticalTint;

        // Шум (если доступен материал с шумом)
        if (noiseMaterial != null)
        {
            // Можно смешивать материалы или применять шум через шейдер
            // Для простоты используем цветовое затемнение
        }

        // Дополнительный эффект через шейдер (если поддерживается)
        if (feed.screenMaterial.HasProperty("_NoiseStrength"))
        {
            feed.screenMaterial.SetFloat("_NoiseStrength", criticalNoiseIntensity);
        }
    }

    private void RemoveCriticalEffects(CameraFeed feed)
    {
        if (feed.screenMaterial == null)
            return;

        // Восстановление нормального цвета
        feed.screenMaterial.color = Color.white;

        // Удаление шума
        if (feed.screenMaterial.HasProperty("_NoiseStrength"))
        {
            feed.screenMaterial.SetFloat("_NoiseStrength", 0f);
        }
    }

    private void OnServerStatusChanged(int serverIndex, MonitoringDataService.ServerStatus oldStatus, MonitoringDataService.ServerStatus newStatus)
    {
        // Обновление камеры, связанной с изменившимся сервером
        for (int i = 0; i < cameraFeeds.Length; i++)
        {
            if (cameraFeeds[i].associatedServerIndex == serverIndex)
            {
                UpdateCameraFeed(i);
                break;
            }
        }
    }
}