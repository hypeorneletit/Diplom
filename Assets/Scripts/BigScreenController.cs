using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Контроллер большого экрана.
/// По нажатию на экран переключает режим: текущие данные / снимок последнего инцидента.
/// </summary>
public class BigScreenController : MonoBehaviour
{
    [Header("XR Interaction")]
    [SerializeField] private XRSimpleInteractable interactable;

    private void Awake()
    {
        if (interactable == null)
        {
            interactable = GetComponent<XRSimpleInteractable>();
        }
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnScreenSelected);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnScreenSelected);
        }
    }

    private void OnScreenSelected(SelectEnterEventArgs args)
    {
        if (IncidentReplayController.Instance == null)
        {
            Debug.LogWarning("[BigScreenController] IncidentReplayController not found in scene");
            return;
        }

        if (!IncidentReplayController.Instance.HasIncident)
        {
            Debug.LogWarning("[BigScreenController] No incidents captured yet");
            return;
        }

        // Переключаем отображение: текущие данные / снимок инцидента
        IncidentReplayController.Instance.ToggleShowIncident();
    }
}

