using UnityEngine;
using UnityEngine.InputSystem;

public class VRAnimationSpeedControl : MonoBehaviour
{
    [Header("组件引用")]
    public Animator animator;
    [Tooltip("请绑定左手摇杆 (LeftHand Locomotion/Move)")]
    public InputActionProperty moveInputSource;

    [Header("速度控制 (关键设置)")]
    [Tooltip("动画播放倍速：1=正常，2=两倍速。觉得慢就调大这个值！")]
    public float playbackSpeedMultiplier = 1.5f;

    [Header("参数名称 (需与Animator一致)")]
    // 根据你的截图 image_d00923.png，你的参数名是这些：
    public string paramHorizontal = "VRIK_Horizontal";
    public string paramVertical = "VRIK_Vertical";
    public string paramSpeed = "VRIK_Speed";

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. 读取摇杆输入
        Vector2 input = Vector2.zero;
        if (moveInputSource.action != null)
            input = moveInputSource.action.ReadValue<Vector2>();

        // 2. 传给动画机 (保证腿会动)
        animator.SetFloat(paramHorizontal, input.x);
        animator.SetFloat(paramVertical, input.y);
        animator.SetFloat(paramSpeed, input.magnitude);

        // 3. ⭐ 核心：控制播放速度
        // 如果有输入(在移动)，就应用倍速；如果停下，恢复1倍速(避免呼吸动画鬼畜)
        if (input.magnitude > 0.05f)
        {
            // 根据推摇杆的力度，从 1倍速 过渡到 设置的倍速
            animator.speed = Mathf.Lerp(1.0f, playbackSpeedMultiplier, input.magnitude);
        }
        else
        {
            animator.speed = 1.0f;
        }
    }

    // 激活输入系统
    void OnEnable() => moveInputSource.action?.Enable();
    void OnDisable() => moveInputSource.action?.Disable();
}