using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRDirectInteractor))]
public class SnowballThrower : MonoBehaviour
{
    [Header("必填参数")]
    public GameObject snowballPrefab;
    public InputActionReference gripAction; // 绑定到Grip输入

    [Header("投掷参数")]
    [SerializeField] private float spawnOffset = 0.1f;
    [SerializeField][Range(1f, 3f)] private float throwForceMultiplier = 1.5f;

    private XRDirectInteractor interactor;
    private GameObject currentSnowball;
    private Vector3 throwDirection;

    private void Awake()
    {
        interactor = GetComponent<XRDirectInteractor>();

        // 配置输入Action
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
        if (currentSnowball == null)
        {
            // 生成雪球（带偏移防止穿模）
            Vector3 spawnPos = transform.position + transform.forward * spawnOffset;
            currentSnowball = Instantiate(snowballPrefab, spawnPos, Quaternion.identity);

            // 配置物理参数
            var grabInteractable = currentSnowball.GetComponent<XRGrabInteractable>();
            grabInteractable.throwVelocityScale = throwForceMultiplier;
            grabInteractable.throwSmoothingDuration = 0.1f;

            // 立即抓取
            interactor.StartManualInteraction(grabInteractable as IXRSelectInteractable);
        }
    }

    private void OnGripReleased(InputAction.CallbackContext context)
    {
        if (currentSnowball != null)
        {
            // 获取手柄运动方向（世界坐标系）
            throwDirection = interactor.transform.forward;

            // 强制修正方向（可选）
            Rigidbody rb = currentSnowball.GetComponent<Rigidbody>();
            rb.velocity = throwDirection * rb.velocity.magnitude;

            currentSnowball = null;
        }
    }
}