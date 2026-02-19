using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Контроллер монитора рабочего места - открывает окно при нажатии
/// </summary>
public class WorkstationMonitorController : MonoBehaviour
{
    [Header("Window Reference")]
    [SerializeField] private MonitorWindow monitorWindow;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer monitorScreenRenderer;
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private Material normalMaterial;

    private XRBaseInteractable interactable;
    private Material originalMaterial;

    private void Awake()
    {
        // Поиск XR Interactable компонента
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable == null)
        {
            interactable = GetComponentInChildren<XRBaseInteractable>();
        }

        // Поиск MonitorWindow если не указан
        if (monitorWindow == null)
        {
            monitorWindow = GetComponentInChildren<MonitorWindow>();
        }

        // Сохранение оригинального материала
        if (monitorScreenRenderer != null)
        {
            originalMaterial = monitorScreenRenderer.material;
            if (normalMaterial == null)
            {
                normalMaterial = originalMaterial;
            }
        }
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnMonitorActivated);
            interactable.hoverEntered.AddListener(OnMonitorHovered);
            interactable.hoverExited.AddListener(OnMonitorUnhovered);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnMonitorActivated);
            interactable.hoverEntered.RemoveListener(OnMonitorHovered);
            interactable.hoverExited.RemoveListener(OnMonitorUnhovered);
        }
    }

    private void OnMonitorActivated(SelectEnterEventArgs args)
    {
        if (monitorWindow != null)
        {
            // Повторное нажатие по монитору теперь закрывает окно
            if (monitorWindow.IsOpen)
            {
                monitorWindow.CloseWindow();
            }
            else
            {
                monitorWindow.OpenWindow();
            }
        }
        else
        {
            Debug.LogWarning($"MonitorWindow not found on {gameObject.name}");
        }
    }

    private void OnMonitorHovered(HoverEnterEventArgs args)
    {
        // Визуальная обратная связь при наведении
        if (monitorScreenRenderer != null && hoverMaterial != null)
        {
            monitorScreenRenderer.material = hoverMaterial;
        }
    }

    private void OnMonitorUnhovered(HoverExitEventArgs args)
    {
        // Возврат к нормальному материалу
        if (monitorScreenRenderer != null && normalMaterial != null)
        {
            monitorScreenRenderer.material = normalMaterial;
        }
    }
}