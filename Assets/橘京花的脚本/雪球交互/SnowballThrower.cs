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
    public InputActionReference gripAction;

    [Header("投掷参数")]
    [SerializeField] private float spawnOffset = 0.1f;
    [SerializeField][Range(1f, 3f)] private float throwForceMultiplier = 1.5f;
    [SerializeField] private float minThrowSpeed = 2f; // 新增：最小投掷速度阈值

    private XRDirectInteractor interactor;
    private GameObject currentSnowball;
    private Rigidbody currentSnowballRb; // 缓存Rigidbody引用

    private void Awake()
    {
        interactor = GetComponent<XRDirectInteractor>();
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
            Vector3 spawnPos = transform.position + transform.forward * spawnOffset;
            currentSnowball = Instantiate(snowballPrefab, spawnPos, Quaternion.identity);
            currentSnowballRb = currentSnowball.GetComponent<Rigidbody>();

            // 确保刚体初始状态正确
            currentSnowballRb.isKinematic = false;
            currentSnowballRb.useGravity = true;

            var grabInteractable = currentSnowball.GetComponent<XRGrabInteractable>();
            grabInteractable.throwVelocityScale = throwForceMultiplier;
            grabInteractable.throwSmoothingDuration = 0.1f;

            // 监听释放事件（保险措施）
            grabInteractable.selectExited.AddListener(OnSnowballReleased);

            interactor.StartManualInteraction(grabInteractable as IXRSelectInteractable);
        }
    }

    private void OnGripReleased(InputAction.CallbackContext context)
    {
        if (currentSnowball != null)
        {
            ReleaseSnowball();
        }
    }

    // 新增：安全释放雪球方法
    private void ReleaseSnowball()
    {
        if (currentSnowballRb != null)
        {
            // 确保物理状态正确
            currentSnowballRb.isKinematic = false;

            // 获取XR交互工具计算的抛出速度
            Vector3 throwVelocity = currentSnowballRb.velocity;

            // 如果速度太小，赋予最小初速度
            if (throwVelocity.magnitude < minThrowSpeed)
            {
                throwVelocity = interactor.transform.forward * minThrowSpeed;
            }

            currentSnowballRb.velocity = throwVelocity;
        }

        currentSnowball = null;
        currentSnowballRb = null;
    }

    // 新增：防止XR交互工具异常释放
    private void OnSnowballReleased(SelectExitEventArgs args)
    {
        if (args.interactableObject.transform.gameObject == currentSnowball)
        {
            ReleaseSnowball();
        }
    }
}