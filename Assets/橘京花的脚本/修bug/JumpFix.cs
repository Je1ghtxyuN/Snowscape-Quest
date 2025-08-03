using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;

[RequireComponent(typeof(JumpProvider))]
public class JumpFix : MonoBehaviour
{
    [SerializeField] private InputHelpers.Button jumpButton = InputHelpers.Button.PrimaryButton;
    [SerializeField] private float jumpForceMultiplier = 1.5f; // 跳跃力增强系数
    [SerializeField] private float groundCheckDistance = 0.1f; // 地面检测距离
    [SerializeField] private LayerMask groundLayer = ~0; // 地面层掩码

    private JumpProvider m_JumpProvider;
    private InputDevice m_RightInputDevice;
    private bool m_LastButtonState;
    private float m_LastJumpTime;
    private const float JUMP_COOLDOWN = 0.3f;
    private CharacterController m_CharacterController;

    private void Awake()
    {
        m_JumpProvider = GetComponent<JumpProvider>();
        m_CharacterController = GetComponent<CharacterController>();

        // 强制启用必要组件
        if (m_CharacterController == null)
        {
            m_CharacterController = gameObject.AddComponent<CharacterController>();
        }
    }

    private void Start()
    {
        InitializeInputDevice();
        ModifyJumpParameters();
    }

    private void InitializeInputDevice()
    {
        m_RightInputDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!m_RightInputDevice.isValid)
        {
            Debug.LogWarning("Right hand device not found, will retry...");
        }
    }

    private void ModifyJumpParameters()
    {
        // 通过反射修改JumpProvider的关键参数
        var heightField = typeof(JumpProvider).GetField("m_JumpHeight",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var unlimitedField = typeof(JumpProvider).GetField("m_UnlimitedInAirJumps",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (heightField != null)
        {
            // 修正类型转换问题
            float currentHeight = (float)heightField.GetValue(m_JumpProvider);
            heightField.SetValue(m_JumpProvider, currentHeight * jumpForceMultiplier);
        }

        if (unlimitedField != null)
            unlimitedField.SetValue(m_JumpProvider, true);
    }

    private void Update()
    {
        if (m_JumpProvider == null)
            return;

        if (!m_RightInputDevice.isValid)
        {
            InitializeInputDevice();
            return;
        }

        if (TryGetButtonInput(out bool isPressed))
        {
            if (isPressed && !m_LastButtonState && Time.time - m_LastJumpTime > JUMP_COOLDOWN && IsGrounded())
            {
                PerformJump();
                m_LastJumpTime = Time.time;
            }
            m_LastButtonState = isPressed;
        }
    }

    private bool IsGrounded()
    {
        if (m_CharacterController == null) return false;

        // 使用CharacterController的内置地面检测
        if (m_CharacterController.isGrounded) return true;

        // 额外的射线检测作为备用
        RaycastHit hit;
        Vector3 rayStart = transform.position + m_CharacterController.center;
        float rayLength = m_CharacterController.height / 2 - m_CharacterController.radius + groundCheckDistance;

        if (Physics.SphereCast(rayStart, m_CharacterController.radius, Vector3.down, out hit, rayLength, groundLayer))
        {
            return true;
        }

        return false;
    }

    private bool TryGetButtonInput(out bool isPressed)
    {
        return m_RightInputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out isPressed) ||
               m_RightInputDevice.IsPressed(jumpButton, out isPressed, 0.1f);
    }

    private void PerformJump()
    {
        Debug.Log("Jump initiated");

        // 方法1：直接调用JumpProvider
        //if (IsGrounded())
        //{
        //    m_JumpProvider.Jump();
        //}
        // 方法2：强制应用跳跃力（备用方案）
        //if (IsGrounded())
        //{
        //    StartCoroutine(ApplyForceAfterDelay(0.1f));
        //}
    }

    private System.Collections.IEnumerator ApplyForceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (m_CharacterController != null)
        {
            // 计算跳跃速度 (v = sqrt(2gh))
            float jumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * (float)typeof(JumpProvider)
                .GetField("m_JumpHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(m_JumpProvider));

            m_CharacterController.Move(Vector3.up * jumpVelocity * Time.deltaTime);
            Debug.Log($"Manual jump applied with force: {jumpVelocity}");
        }
    }
}