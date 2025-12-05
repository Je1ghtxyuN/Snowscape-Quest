using UnityEngine;

public class VRBodyAnimator : MonoBehaviour
{
    [Header("组件")]
    public Animator animator;
    public Transform headset;

    [Header("移动参数")]
    public float speedSmoothTime = 0.1f;
    public float animationSpeedMultiplier = 1.0f;

    [Header("转身参数")]
    public float turnSpeed = 5f;
    public float turnThreshold = 30f;

    [Header("高度修正 (防下陷)")]
    [Tooltip("模型的地面高度偏移，通常设为0。如果模型浮空，调小；如果陷地，调大")]
    public float floorHeightOffset = 0f;

    private Vector3 previousPos;
    private float currentSpeed;
    private float speedVelocity;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        // 自动查找 VR 相机
        if (headset == null && Camera.main != null)
            headset = Camera.main.transform;

        if (headset != null) previousPos = headset.position;
    }

    void Update()
    {
        if (headset == null) return;

        // --- 1. 强制锁定位置 (解决下陷/漂移核心) ---
        // 让模型时刻跟随头显的 X 和 Z，但 Y 轴强制锁定在地面
        Vector3 targetPos = headset.position;
        // 这里的 y = floorHeightOffset 保证模型永远站在地上
        targetPos.y = transform.parent != null ? transform.parent.position.y + floorHeightOffset : floorHeightOffset;

        // 应用位置 (平滑跟随一点点，避免VR微抖动导致模型抽搐)
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 20f);


        // --- 2. 计算速度传给动画 ---
        Vector3 currentHeadPos = headset.position;
        Vector3 delta = currentHeadPos - previousPos;
        delta.y = 0; // 只计算水平移动速度

        float targetSpeed = delta.magnitude / Time.deltaTime;
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, speedSmoothTime);

        animator.SetFloat("Speed", currentSpeed * animationSpeedMultiplier);

        previousPos = currentHeadPos;


        // --- 3. 转身逻辑 ---
        Vector3 headForward = headset.forward;
        headForward.y = 0;
        headForward.Normalize();

        Vector3 bodyForward = transform.forward;
        float angle = Vector3.Angle(bodyForward, headForward);

        if (angle > turnThreshold || currentSpeed > 0.05f) // 移动时也转身
        {
            Quaternion targetRot = Quaternion.LookRotation(headForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
        }
    }
}