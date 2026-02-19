using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// XR-управление монитором камер наблюдения.
/// По клику по монитору переключает камеру (скриншот) в SecurityCameraFeed.
/// </summary>
public class SecurityCameraMonitorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SecurityCameraFeed cameraFeed;
    [SerializeField] private SecurityCameraWindow cameraWindow;
    [SerializeField] private XRBaseInteractable interactable;

    [Header("Optional Visual Feedback")]
    [SerializeField] private Renderer monitorScreenRenderer;
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private Material normalMaterial;

    private Material originalMaterial;
    
    [Header("Input")]
    [SerializeField] private float longPressThreshold = 1.0f;

    private float selectStartTime = -1f;

    private void Awake()
    {
        if (cameraFeed == null)
            cameraFeed = GetComponentInChildren<SecurityCameraFeed>();

        if (cameraWindow == null)
            cameraWindow = GetComponentInChildren<SecurityCameraWindow>();

        if (interactable == null)
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (interactable == null)
                interactable = GetComponentInChildren<XRBaseInteractable>();
        }

        if (monitorScreenRenderer != null)
        {
            originalMaterial = monitorScreenRenderer.material;
            if (normalMaterial == null)
                normalMaterial = originalMaterial;
        }
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
            interactable.hoverEntered.AddListener(OnHovered);
            interactable.hoverExited.AddListener(OnUnhovered);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
            interactable.hoverEntered.RemoveListener(OnHovered);
            interactable.hoverExited.RemoveListener(OnUnhovered);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        selectStartTime = Time.time;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (cameraWindow != null)
        {
            float pressDuration = (selectStartTime > 0f) ? Time.time - selectStartTime : 0f;
            selectStartTime = -1f;

            if (!cameraWindow.IsOpen)
            {
                // Окно ещё не открыто — любое нажатие открывает его.
                cameraWindow.OpenWindow();
            }
            else
            {
                // Окно уже открыто:
                // короткое нажатие — следующая камера,
                // долгое нажатие (>= longPressThreshold) — закрыть окно.
                if (pressDuration >= longPressThreshold)
                {
                    cameraWindow.CloseWindow();
                }
                else
                {
                    cameraWindow.ShowNextCamera();
                }
            }
        }
        else if (cameraFeed != null)
        {
            // Фоллбэк: просто листаем камеры на экране без окна.
            cameraFeed.Next();
        }
        else
        {
            Debug.LogWarning($"[SecurityCameraMonitorController] SecurityCameraWindow / SecurityCameraFeed not found on {name}");
        }
    }

    private void OnHovered(HoverEnterEventArgs args)
    {
        if (monitorScreenRenderer != null && hoverMaterial != null)
            monitorScreenRenderer.material = hoverMaterial;
    }

    private void OnUnhovered(HoverExitEventArgs args)
    {
        if (monitorScreenRenderer != null && normalMaterial != null)
            monitorScreenRenderer.material = normalMaterial;
    }
}

