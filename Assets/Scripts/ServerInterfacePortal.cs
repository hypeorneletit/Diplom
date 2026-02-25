using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ServerInterfacePortal : MonoBehaviour
{
    [Header("XR")]
    [SerializeField] private Transform xrOrigin;      // XR Origin / XR Rig
    [SerializeField] private CharacterController xrController; // если используешь CharacterController

    [Header("Teleport Points")]
    [SerializeField] private Transform enterPoint;    // точка в комнате
    [SerializeField] private Transform exitPoint;     // точка возле сервера

    [Header("Interface Room Root (optional)")]
    [SerializeField] private GameObject interfaceRoomRoot; // Server3A_InterfaceRoom (можно включать/выключать)

    private bool isInside = false;
    private Vector3 savedPosition;
    private Quaternion savedRotation;

    public void EnterInterface()
    {
        if (isInside || xrOrigin == null || enterPoint == null)
            return;

        // Сохраняем позицию
        savedPosition = xrOrigin.position;
        savedRotation = xrOrigin.rotation;

        // Если есть CharacterController, временно отключаем, чтобы не было конфликтов
        if (xrController != null)
            xrController.enabled = false;

        // Переносим XR Origin в интерфейс‑комнату
        xrOrigin.position = enterPoint.position;
        xrOrigin.rotation = enterPoint.rotation;

        if (xrController != null)
            xrController.enabled = true;

        if (interfaceRoomRoot != null)
            interfaceRoomRoot.SetActive(true);

        isInside = true;
    }

    public void ExitInterface()
    {
        if (!isInside || xrOrigin == null || exitPoint == null)
            return;

        if (xrController != null)
            xrController.enabled = false;

        xrOrigin.position = exitPoint.position;
        xrOrigin.rotation = exitPoint.rotation;

        if (xrController != null)
            xrController.enabled = true;

        if (interfaceRoomRoot != null)
            interfaceRoomRoot.SetActive(false);

        isInside = false;
    }
}