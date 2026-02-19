using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Контроллер двери - ИСПРАВЛЕНО: вращение вокруг pivot с правильным позиционированием
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private bool openInward = true;
    [SerializeField] private float pivotOffsetRight = 0f; // Смещение pivot вправо для правильного позиционирования
    [SerializeField] private float openedExtraRight = 0f; // Доп. смещение вправо ТОЛЬКО в открытом состоянии
    
    [Header("Pivot Reference")]
    [SerializeField] private Transform pivotTransform;

    [Header("XR Interaction")]
    [SerializeField] private XRBaseInteractable interactable;

    private Transform doorPivot;
    private Vector3 pivotWorldPosition;
    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen = false;
    private Coroutine currentAnimation;
    private Rigidbody doorRigidbody;

    private void Awake()
    {
        // КРИТИЧНО: Делаем Rigidbody kinematic
        doorRigidbody = GetComponent<Rigidbody>();
        if (doorRigidbody != null)
        {
            doorRigidbody.isKinematic = true;
            doorRigidbody.useGravity = false;
            Debug.Log($"[DoorController] Set Rigidbody to kinematic on {gameObject.name}");
        }

        // Определение pivot
        if (pivotTransform != null)
        {
            doorPivot = pivotTransform;
            Debug.Log($"[DoorController] Using assigned pivot: {pivotTransform.name}");
        }
        else
        {
            doorPivot = FindPivot("Pivot_Closed");
            if (doorPivot == null)
            {
                doorPivot = transform;
                Debug.LogWarning($"[DoorController] Pivot not found, using door transform itself on {gameObject.name}");
            }
            else
            {
                Debug.Log($"[DoorController] Found pivot: {doorPivot.name}");
            }
        }

        // Сохраняем мировую позицию pivot и начальную позицию двери
        pivotWorldPosition = doorPivot.position;
        closedPosition = transform.position;
        closedRotation = transform.rotation;
        
        // Вычисляем открытое вращение
        float direction = openInward ? -1f : 1f;
        openRotation = closedRotation * Quaternion.Euler(0, direction * openAngle, 0);

        Debug.Log($"[DoorController] Door closed rotation: {closedRotation.eulerAngles}, open rotation: {openRotation.eulerAngles}");
        Debug.Log($"[DoorController] Pivot position: {pivotWorldPosition}, Door position: {closedPosition}");

        // Поиск Interactable
        if (interactable == null)
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (interactable == null)
            {
                interactable = GetComponentInParent<XRBaseInteractable>();
            }
            if (interactable == null)
            {
                interactable = GetComponentInChildren<XRBaseInteractable>(true);
            }
        }

        if (interactable != null)
        {
            Debug.Log($"[DoorController] Found interactable: {interactable.name} on {gameObject.name}");
        }
        else
        {
            Debug.LogError($"[DoorController] XR Interactable not found on {gameObject.name}!");
        }
    }

    private Transform FindPivot(string name)
    {
        Transform found = transform.Find(name);
        if (found == null)
        {
            foreach (Transform child in transform)
            {
                found = child.Find(name);
                if (found != null) break;
                
                foreach (Transform grandchild in child)
                {
                    found = grandchild.Find(name);
                    if (found != null) break;
                }
                if (found != null) break;
            }
        }
        return found;
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnDoorActivated);
            Debug.Log($"[DoorController] Subscribed to selectEntered event on {gameObject.name}");
        }
        else
        {
            Debug.LogError($"[DoorController] Cannot subscribe - XR Interactable is null on {gameObject.name}!");
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnDoorActivated);
        }
    }

    private void OnDoorActivated(SelectEnterEventArgs args)
    {
        Debug.Log($"[DoorController] Door activated! Current state: {(isOpen ? "Open" : "Closed")}");
        ToggleDoor();
    }

    public void ToggleDoor()
    {
        Debug.Log($"[DoorController] ToggleDoor called. Current state: {(isOpen ? "Open" : "Closed")}");
        
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        isOpen = !isOpen;
        currentAnimation = StartCoroutine(AnimateDoor(isOpen));
    }

    private IEnumerator AnimateDoor(bool open)
    {
        Debug.Log($"[DoorController] Starting door animation: {(open ? "Opening" : "Closing")}");
        
        // Обновляем позицию pivot на случай если она изменилась
        pivotWorldPosition = doorPivot.position;
        
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = open ? openRotation : closedRotation;
        Vector3 startPos = transform.position;
        Vector3 targetPos = open ? CalculateOpenPosition() : closedPosition;

        Debug.Log($"[DoorController] Animation: startRot={startRot.eulerAngles}, targetRot={targetRot.eulerAngles}");
        Debug.Log($"[DoorController] Animation: startPos={startPos}, targetPos={targetPos}");

        float duration = 1f / Mathf.Max(0.1f, openSpeed);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            
            // Вращаем дверь вокруг pivot point
            Quaternion currentRot = Quaternion.Slerp(startRot, targetRot, t);
            // Вычисляем offset от pivot
            Vector3 offset = startPos - pivotWorldPosition;
            
            // Применяем вращение к offset
            Vector3 rotatedOffset = currentRot * Quaternion.Inverse(startRot) * offset;
            
            // Устанавливаем новую позицию и вращение
            // Доп. смещение вправо при открытии (чтобы дверь не была по центру проема)
            Vector3 rightExtra = (open ? transform.right * openedExtraRight : Vector3.zero);
            transform.position = pivotWorldPosition + rotatedOffset + rightExtra;
            transform.rotation = currentRot;
            
            yield return null;
        }

        // Финальная позиция и вращение
        Vector3 finalOffset = targetPos - pivotWorldPosition;
        transform.position = targetPos;
        transform.rotation = targetRot;
        
        Debug.Log($"[DoorController] Door animation complete: {(open ? "Opened" : "Closed")}, final rotation: {transform.rotation.eulerAngles}, final position: {transform.position}");
        currentAnimation = null;
    }

    private Vector3 CalculateOpenPosition()
    {
        // Вычисляем позицию двери в открытом состоянии
        // Дверь должна быть с краю проема, поэтому используем pivot как точку вращения
        Vector3 offset = closedPosition - pivotWorldPosition;
        Vector3 rotatedOffset = openRotation * Quaternion.Inverse(closedRotation) * offset;
        
        // Базовое смещение вправо (чтобы дверь была ближе к правому краю)
        Vector3 rightOffset = transform.right * pivotOffsetRight;

        // Доп. смещение вправо только в открытом состоянии (тонкая подстройка)
        Vector3 rightOpenOffset = transform.right * openedExtraRight;
        
        return pivotWorldPosition + rotatedOffset + rightOffset + rightOpenOffset;
    }
}