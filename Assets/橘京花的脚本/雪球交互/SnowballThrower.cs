using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;

[RequireComponent(typeof(XRDirectInteractor))]
public class SnowballThrower : MonoBehaviour
{
    [Header("必填参数")]
    public GameObject snowballPrefab;
    public InputActionReference gripAction;

    [Header("生成位置")]
    [SerializeField] private float spawnOffset = 0.1f;

    [Header("抛掷参数")]
    [SerializeField] private float throwForceMultiplier = 1.2f;
    [SerializeField] private float gravityScale = 0.5f;
    [SerializeField] private int velocitySmoothingFrames = 10;

    private XRDirectInteractor interactor;
    private List<GameObject> activeSnowballs = new List<GameObject>();
    private Queue<Vector3> velocityHistory = new Queue<Vector3>();
    private Vector3 previousPosition;
    private Vector3 smoothedVelocity;

    private void Awake()
    {
        interactor = GetComponent<XRDirectInteractor>();

        // 确保 gripAction 不为 null
        if (gripAction != null && gripAction.action != null)
        {
            gripAction.action.Enable();
            gripAction.action.performed += OnGripPressed;
            gripAction.action.canceled += OnGripReleased;
        }
        else
        {
            Debug.LogError("Grip Action 未设置或无效!");
        }

        previousPosition = transform.position;

        // 初始化速度历史队列
        for (int i = 0; i < velocitySmoothingFrames; i++)
        {
            velocityHistory.Enqueue(Vector3.zero);
        }
    }

    private void Update()
    {
        // 计算控制器速度
        Vector3 currentVelocity = (transform.position - previousPosition) / Time.deltaTime;
        previousPosition = transform.position;

        // 更新速度历史队列
        velocityHistory.Enqueue(currentVelocity);
        if (velocityHistory.Count > velocitySmoothingFrames)
        {
            velocityHistory.Dequeue();
        }

        // 计算平滑速度（忽略最后几帧的不稳定轨迹）
        smoothedVelocity = CalculateSmoothedVelocity();

        // 清理已销毁的雪球引用
        for (int i = activeSnowballs.Count - 1; i >= 0; i--)
        {
            if (activeSnowballs[i] == null)
            {
                activeSnowballs.RemoveAt(i);
            }
        }
    }

    // 计算平滑速度的方法
    private Vector3 CalculateSmoothedVelocity()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        // 计算前80%帧的平均速度，忽略最后20%的不稳定轨迹
        int framesToSkip = Mathf.FloorToInt(velocitySmoothingFrames * 0.1f);
        int framesToUse = velocitySmoothingFrames - framesToSkip;

        foreach (Vector3 velocity in velocityHistory)
        {
            if (count < framesToUse)
            {
                sum += velocity;
                count++;
            }
        }

        return count > 0 ? sum / count : Vector3.zero;
    }

    private void OnDestroy()
    {
        if (gripAction != null && gripAction.action != null)
        {
            gripAction.action.performed -= OnGripPressed;
            gripAction.action.canceled -= OnGripReleased;
        }
    }

    private void OnGripPressed(InputAction.CallbackContext context)
    {
        if (snowballPrefab != null)
        {
            // 实例化雪球
            Vector3 spawnPos = transform.position + transform.forward * spawnOffset;
            GameObject snowball = Instantiate(snowballPrefab, spawnPos, Quaternion.identity);
            activeSnowballs.Add(snowball);

            // 获取或添加抓取组件
            XRGrabInteractable grabInteractable = snowball.GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = snowball.AddComponent<XRGrabInteractable>();
            }

            // 配置抓取属性
            grabInteractable.throwOnDetach = false;
            grabInteractable.selectExited.AddListener((args) => OnSnowballReleased(args, snowball));

            // 确保刚体设置正确
            Rigidbody rb = snowball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = false;

                // 禁用雪球与控制器之间的碰撞
                Collider snowballCollider = snowball.GetComponent<Collider>();
                if (snowballCollider != null)
                {
                    foreach (var controllerCollider in GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(snowballCollider, controllerCollider, true);
                    }
                }
            }

            // 开始抓取交互
            if (interactor != null)
            {
                interactor.StartManualInteraction(grabInteractable as IXRSelectInteractable);
            }
        }
    }

    private void OnGripReleased(InputAction.CallbackContext context)
    {
        // 释放所有当前被抓取的雪球
        if (interactor != null && interactor.hasSelection)
        {
            interactor.EndManualInteraction();
        }
    }

    private void OnSnowballReleased(SelectExitEventArgs args, GameObject snowball)
    {
        // 确保雪球对象仍然存在
        if (snowball == null) return;

        // 启用重力并应用平滑后的速度
        Rigidbody rb = snowball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 使用平滑后的速度，忽略最后的不稳定抖动
            Vector3 throwVelocity = smoothedVelocity * throwForceMultiplier;
            rb.velocity = throwVelocity;

            // 添加一些旋转效果，使抛物线更自然
            rb.angularVelocity = Vector3.Cross(transform.forward, throwVelocity).normalized *
                                throwVelocity.magnitude * 0.1f;

            // 添加自定义重力组件
            SnowballGravity snowballGravity = snowball.AddComponent<SnowballGravity>();
            snowballGravity.gravityScale = gravityScale;

            // 重新启用雪球与其他物体的碰撞
            Collider snowballCollider = snowball.GetComponent<Collider>();
            if (snowballCollider != null)
            {
                foreach (var controllerCollider in GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(snowballCollider, controllerCollider, false);
                }
            }
        }

        // 从活动列表中移除
        activeSnowballs.Remove(snowball);
    }
}

// 自定义重力组件
public class SnowballGravity : MonoBehaviour
{
    public float gravityScale = 0.5f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 禁用默认重力，我们将自己处理
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        // 应用自定义重力
        if (rb != null)
        {
            rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
        }
    }
}