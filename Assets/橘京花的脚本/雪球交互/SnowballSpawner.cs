using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRDirectInteractor))]
public class SnowballSpawner : MonoBehaviour
{
    [Header("必填项")]
    public GameObject snowballPrefab;
    public InputActionProperty gripAction; // 新版输入绑定方式

    [Header("可选配置")]
    [SerializeField] private float spawnOffset = 0.1f;
    [SerializeField] private float throwForceMultiplier = 1.5f;

    private XRDirectInteractor interactor;
    private GameObject currentSnowball;

    private void Awake()
    {
        interactor = GetComponent<XRDirectInteractor>();

        // 配置输入Action（兼容新旧输入系统）
        gripAction.action.Enable();
        gripAction.action.performed += OnGripPressed;
        gripAction.action.canceled += OnGripReleased;
    }

    private void OnDestroy()
    {
        gripAction.action.performed -= OnGripPressed;
        gripAction.action.canceled -= OnGripReleased;
    }

    private void OnGripPressed(InputAction.CallbackContext context)
    {
        if (currentSnowball == null && snowballPrefab != null)
        {
            // 生成位置偏移（避免穿模）
            Vector3 spawnPos = transform.position + transform.forward * spawnOffset;

            currentSnowball = Instantiate(snowballPrefab, spawnPos, Quaternion.identity);
            var grabInteractable = currentSnowball.GetComponent<XRGrabInteractable>();

            // 配置投掷力度
            if (grabInteractable != null)
            {
                grabInteractable.throwVelocityScale = throwForceMultiplier;
                grabInteractable.throwSmoothingDuration = 0.1f;
            }

            // 立即抓取
            interactor.StartManualInteraction((IXRSelectInteractable)grabInteractable);
        }
    }

    private void OnGripReleased(InputAction.CallbackContext context)
    {
        if (currentSnowball != null)
        {
            Rigidbody rb = currentSnowball.GetComponent<Rigidbody>();
            rb.isKinematic = false; 
            rb.velocity = transform.forward * throwForceMultiplier;
            currentSnowball = null;
        }
    }
}