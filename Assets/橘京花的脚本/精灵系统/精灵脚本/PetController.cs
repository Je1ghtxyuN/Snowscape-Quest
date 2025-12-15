using UnityEngine;

public class PetController : MonoBehaviour
{
    [Header("跟随设置")]
    public Transform playerHead;
    public Vector3 targetOffset = new Vector3(0.8f, 0.2f, 0.5f);
    public float smoothTime = 0.5f;
    public float rotationSpeed = 5f;

    [Header("悬浮呼吸感")]
    public float floatAmplitude = 0.1f;
    public float floatFrequency = 1.5f;

    [Header("模型朝向修正 (关键)")]
    [Tooltip("如果是侧身模型填90/-90，屁股对人填180")]
    [Range(-180f, 180f)]
    public float modelRotationOffset = 0f;

    [Header("状态")]
    public bool isBusy = false;

    private Vector3 currentVelocity;
    private Vector3 floatOffset;

    void Start()
    {
        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;

        // ⭐ 游戏开始瞬间：强制瞬移到目标位置并强制对齐朝向
        // 这样一开始就不会出现精灵在奇怪位置或慢慢转头的情况
        if (playerHead != null)
        {
            Vector3 startPos = playerHead.TransformPoint(targetOffset);
            transform.position = startPos;
            HandleRotation(true); // true = 瞬间完成
        }
    }

    void LateUpdate()
    {
        if (playerHead == null || isBusy) return;

        HandleMovement();
        HandleRotation(false); // false = 平滑旋转
    }

    void HandleMovement()
    {
        // 1. 计算基础目标位置
        Vector3 targetPos = playerHead.TransformPoint(targetOffset);

        // 2. 叠加呼吸浮动
        floatOffset.y = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        Vector3 finalTarget = targetPos + floatOffset;

        // 3. 平滑移动
        transform.position = Vector3.SmoothDamp(transform.position, finalTarget, ref currentVelocity, smoothTime);
    }

    void HandleRotation(bool isInstant)
    {
        Vector3 targetLookDir;

        // ⭐ 逻辑整合：从 VoiceSystem 获取状态
        bool isTalking = false;
        if (PetVoiceSystem.Instance != null)
            isTalking = PetVoiceSystem.Instance.IsSpeaking;

        // 1. 确定我们要看哪里
        if (isTalking)
        {
            // 说话时：看着玩家
            targetLookDir = playerHead.position - transform.position;
        }
        else
        {
            // 不说话时：看着前方（和玩家同向）
            targetLookDir = playerHead.forward;
        }

        // 2. 锁定Y轴 (防止抬头低头)
        targetLookDir.y = 0;

        // 3. 计算旋转
        if (targetLookDir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(targetLookDir);

            // ⭐ 应用你的修正角度 (这里是解决问题的关键)
            // 先看向目标，再叠加自身的修正旋转
            Quaternion finalRot = lookRot * Quaternion.Euler(0, modelRotationOffset, 0);

            if (isInstant)
            {
                transform.rotation = finalRot;
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, finalRot, rotationSpeed * Time.deltaTime);
            }
        }
    }
}