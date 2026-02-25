using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Контроллер сервера - управляет взаимодействием с сервером через нажатия
/// Короткое нажатие открывает окно, длительное нажатие закрывает окно (как у камер наблюдения)
/// </summary>
public class ServerController : MonoBehaviour
{
    [Header("Window Reference")]
    [SerializeField] private ServerControllerWindow serverWindow;

    [Header("Server Settings")]
    [SerializeField] private int serverIndex = 0; // Индекс сервера (0-3)
    [SerializeField] private string serverDisplayName = "Сервер";

    [Header("Visual Feedback")]
    [SerializeField] private Renderer[] serverRenderers; // Массив рендереров для визуальной обратной связи
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private Material normalMaterial;

    [Header("Input Settings")]
    [SerializeField] private float longPressThreshold = 1.0f; // Время длительного нажатия в секундах

    private XRBaseInteractable interactable;
    private Material[] originalMaterials;
    private float selectStartTime = -1f;

    private void Awake()
    {
        // Поиск XR Interactable компонента
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable == null)
        {
            interactable = GetComponentInChildren<XRBaseInteractable>();
        }

        // Если нет XR Interactable, создаем его
        if (interactable == null)
        {
            GameObject interactionObject = new GameObject("ServerInteraction");
            interactionObject.transform.SetParent(transform);
            interactionObject.transform.localPosition = Vector3.zero;
            interactionObject.transform.localRotation = Quaternion.identity;
            interactionObject.transform.localScale = Vector3.one;

            // Добавляем коллайдер
            BoxCollider collider = interactionObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.5f, 1.5f, 1.5f);
            collider.isTrigger = false;

            // Добавляем XR Simple Interactable
            interactable = interactionObject.AddComponent<XRSimpleInteractable>();
        }

        // Поиск ServerControllerWindow если не указан
        if (serverWindow == null)
        {
            serverWindow = GetComponentInChildren<ServerControllerWindow>();
        }

        // Сохранение оригинальных материалов
        if (serverRenderers != null && serverRenderers.Length > 0)
        {
            originalMaterials = new Material[serverRenderers.Length];
            for (int i = 0; i < serverRenderers.Length; i++)
            {
                if (serverRenderers[i] != null)
                {
                    originalMaterials[i] = serverRenderers[i].material;
                    if (normalMaterial == null)
                    {
                        normalMaterial = originalMaterials[i];
                    }
                }
            }
        }
        else
        {
            // Автоматический поиск рендереров
            serverRenderers = GetComponentsInChildren<Renderer>();
            if (serverRenderers != null && serverRenderers.Length > 0)
            {
                originalMaterials = new Material[serverRenderers.Length];
                for (int i = 0; i < serverRenderers.Length; i++)
                {
                    if (serverRenderers[i] != null)
                    {
                        originalMaterials[i] = serverRenderers[i].material;
                        if (normalMaterial == null)
                        {
                            normalMaterial = originalMaterials[i];
                        }
                    }
                }
            }
        }
    }

    private void Start()
    {
        SetupInteractable();
        UpdateServerWindowSettings();
    }

    private void SetupInteractable()
    {
        if (interactable == null)
            return;

        // Подписка на события
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);
        interactable.hoverEntered.AddListener(OnServerHovered);
        interactable.hoverExited.AddListener(OnServerUnhovered);
    }

    private void UpdateServerWindowSettings()
    {
        if (serverWindow != null)
        {
            // Передаем настройки сервера в окно через рефлексию
            var serverIndexField = serverWindow.GetType().GetField("serverIndex", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (serverIndexField != null)
            {
                serverIndexField.SetValue(serverWindow, serverIndex);
            }

            var serverNameField = serverWindow.GetType().GetField("serverDisplayName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (serverNameField != null)
            {
                serverNameField.SetValue(serverWindow, serverDisplayName);
            }
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Запоминаем время начала нажатия
        selectStartTime = Time.time;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (serverWindow != null)
        {
            float pressDuration = (selectStartTime > 0f) ? Time.time - selectStartTime : 0f;
            selectStartTime = -1f;

            if (!serverWindow.IsOpen)
            {
                // Окно ещё не открыто — любое нажатие открывает его
                serverWindow.OpenWindow();
            }
            else
            {
                // Окно уже открыто:
                // короткое нажатие — ничего не делаем (можно добавить действие),
                // долгое нажатие (>= longPressThreshold) — закрыть окно
                if (pressDuration >= longPressThreshold)
                {
                    serverWindow.CloseWindow();
                }
            }
        }
        else
        {
            Debug.LogWarning($"[ServerController] ServerControllerWindow not found on {gameObject.name}");
        }
    }

    private void OnServerHovered(HoverEnterEventArgs args)
    {
        // Визуальная обратная связь при наведении
        UpdateVisualFeedback(true);
    }

    private void OnServerUnhovered(HoverExitEventArgs args)
    {
        // Возврат к нормальному материалу
        UpdateVisualFeedback(false);
    }

    private void UpdateVisualFeedback(bool isHovered)
    {
        if (serverRenderers == null || serverRenderers.Length == 0)
            return;

        Material materialToUse = isHovered && hoverMaterial != null ? hoverMaterial : 
            (normalMaterial != null ? normalMaterial : null);

        if (materialToUse == null)
            return;

        for (int i = 0; i < serverRenderers.Length; i++)
        {
            if (serverRenderers[i] != null)
            {
                serverRenderers[i].material = materialToUse;
            }
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
            interactable.hoverEntered.RemoveListener(OnServerHovered);
            interactable.hoverExited.RemoveListener(OnServerUnhovered);
        }
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveAllListeners();
            interactable.selectExited.RemoveAllListeners();
            interactable.hoverEntered.RemoveAllListeners();
            interactable.hoverExited.RemoveAllListeners();
        }
    }
}
