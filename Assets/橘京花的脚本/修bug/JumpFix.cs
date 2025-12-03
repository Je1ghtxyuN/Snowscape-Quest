using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;

[RequireComponent(typeof(JumpProvider))]
public class JumpFix : MonoBehaviour
{
    [Header("输入设置")]
    [Tooltip("请务必绑定 RightHand/Optional/PrimaryButton (A键)")]
    public InputActionProperty jumpInputSource;

    [Header("跳跃限制")]
    public float jumpCooldown = 0.5f;

    [Header("调试信息 (运行游戏时查看)")]
    [SerializeField] private bool _isPressed;
    [SerializeField] private bool _isGrounded;
    [SerializeField] private bool _isCooldownReady;

    private JumpProvider m_JumpProvider;
    private CharacterController m_CharacterController;
    private bool m_LastButtonState;
    private float m_LastJumpTime;

    private void Awake()
    {
        m_JumpProvider = GetComponent<JumpProvider>();
        // 尝试获取 CharacterController 用于双重地面检测
        m_CharacterController = GetComponentInParent<CharacterController>();
    }

    private void Start()
    {
        if (m_JumpProvider)
        {
            // 彻底接管控制权
            m_JumpProvider.unlimitedInAirJumps = false;
            m_JumpProvider.inAirJumpCount = 0; // 禁止自带的空跳逻辑
        }
    }

    private void Update()
    {
        // 1. 读取按键
        bool currentPress = IsJumpInputPressed();

        // 2. 更新调试状态 (请在 Inspector 观察这三个勾选框)
        _isPressed = currentPress;
        _isGrounded = CheckIsGrounded();
        _isCooldownReady = (Time.time - m_LastJumpTime > jumpCooldown);

        // 3. 触发逻辑 (按下瞬间 + 冷却好 + 在地面)
        if (currentPress && !m_LastButtonState)
        {
            if (_isCooldownReady)
            {
                if (_isGrounded)
                {
                    PerformForceJump();
                }
                else
                {
                    Debug.LogWarning("❌ 跳跃失败：角色未着地 (isGrounded = false)");
                }
            }
            else
            {
                // Debug.Log("跳跃冷却中...");
            }
        }

        m_LastButtonState = currentPress;
    }

    // 强制跳跃逻辑
    private void PerformForceJump()
    {
        if (m_JumpProvider != null)
        {
            // 临时开启“无限跳”权限，骗过 JumpProvider 的检查
            // 因为我们已经在上面自己检查过 isGrounded 了，这里不仅要跳，而且必须跳
            bool oldState = m_JumpProvider.unlimitedInAirJumps;
            m_JumpProvider.unlimitedInAirJumps = true;

            m_JumpProvider.Jump();

            // 还原状态
            m_JumpProvider.unlimitedInAirJumps = oldState;

            m_LastJumpTime = Time.time;
            Debug.Log("✅ 跳跃成功！");
        }
    }

    private bool IsJumpInputPressed()
    {
        if (jumpInputSource.action == null) return false;
        return jumpInputSource.action.ReadValue<float>() > 0.5f;
    }

    // 双重地面检测：优先信 JumpProvider，信不过就问 CharacterController
    private bool CheckIsGrounded()
    {
        // 1. 尝试通过 JumpProvider 内部逻辑判断 (通常依赖 GravityProvider)
        // 我们利用 CanJump 的部分逻辑：如果它允许跳，说明它认为在地板上
        // 但我们要排除 unlimit 的干扰，所以不能直接调 CanJump

        // 2. 直接问 CharacterController (最准)
        if (m_CharacterController != null)
        {
            return m_CharacterController.isGrounded;
        }

        return false;
    }

    private void OnEnable() => jumpInputSource.action?.Enable();
    private void OnDisable() => jumpInputSource.action?.Disable();
}