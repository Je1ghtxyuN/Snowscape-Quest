using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Collections;

// 明确要求必须挂在XR Origin上
[RequireComponent(typeof(ContinuousMoveProviderBase))]
[RequireComponent(typeof(CharacterController))]
public class XRJumpReset : MonoBehaviour
{
    [Header("跳跃参数")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private InputActionReference jumpAction;

    private ContinuousMoveProviderBase moveProvider;
    private CharacterController characterController;
    private bool isJumping;

    void Awake()
    {
        // 强制校验挂载对象
        if (!gameObject.name.Contains("XR Origin"))
        {
            Debug.LogError("脚本必须挂载在XR Origin上！", this);
            return;
        }

        moveProvider = GetComponent<ContinuousMoveProviderBase>();
        characterController = GetComponent<CharacterController>();

        // 自动继承移动组件的地面层级设置
        groundLayers = (LayerMask)typeof(ContinuousMoveProviderBase)
            .GetField("m_GroundLayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(moveProvider);
    }

    void OnEnable()
    {
        jumpAction.action.Enable();
        jumpAction.action.performed += OnJump;
    }

    void OnDisable()
    {
        jumpAction.action.performed -= OnJump;
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (IsGrounded() && !isJumping)
        {
            // 通过反射调用内部跳跃方法（兼容XRIT所有版本）
            typeof(ContinuousMoveProviderBase)
                .GetMethod("TryQueueJump", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(moveProvider, new object[] { jumpForce });

            isJumping = true;
            StartCoroutine(ResetJumpState());
        }
    }

    private bool IsGrounded()
    {
        return characterController.isGrounded ||
               Physics.CheckSphere(transform.position + Vector3.down * 0.1f, 0.25f, groundLayers);
    }

    private IEnumerator ResetJumpState()
    {
        yield return new WaitForSeconds(0.5f);
        isJumping = false;
    }

    // 每帧强制更新地面状态
    void Update()
    {
        if (isJumping && !IsGrounded())
        {
            characterController.Move(Vector3.down * groundCheckDistance * 2);
        }
    }
}