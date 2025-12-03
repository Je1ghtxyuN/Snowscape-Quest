using UnityEngine;

public class PetController : MonoBehaviour
{
    [Header("跟随设置")]
    public Transform playerHead; // 必须赋值：玩家的头部摄像机
    public Vector3 targetOffset = new Vector3(0.8f, 0.2f, 0.5f); // 目标位置：玩家右侧上方
    public float smoothTime = 0.5f;
    public float rotationSpeed = 5f;

    [Header("悬浮呼吸感")]
    public float floatAmplitude = 0.1f; // 上下浮动幅度
    public float floatFrequency = 1.5f; // 浮动频率

    [Header("状态")]
    public bool isBusy = false; // 如果在做动作（如攻击），暂停跟随逻辑

    private Vector3 currentVelocity;
    private Vector3 floatOffset;

    void Start()
    {
        if (playerHead == null)
        {
            // 尝试自动查找 VR 摄像机
            Camera mainCam = Camera.main;
            if (mainCam != null) playerHead = mainCam.transform;
        }
    }

    void LateUpdate()
    {
        if (playerHead == null || isBusy) return;

        // 1. 计算目标位置 (始终在玩家头部的相对位置)
        // 使用 TransformPoint 将局部坐标转为世界坐标
        Vector3 targetPos = playerHead.TransformPoint(targetOffset);

        // 2. 添加悬浮呼吸感 (正弦波)
        floatOffset.y = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        Vector3 finalTarget = targetPos + floatOffset;

        // 3. 平滑阻尼移动 (比 Lerp 更自然，带有惯性)
        transform.position = Vector3.SmoothDamp(transform.position, finalTarget, ref currentVelocity, smoothTime);

        // 4. 面向：始终温柔地看向玩家的脸，但保持 Y 轴直立
        Vector3 lookDir = playerHead.position - transform.position;
        lookDir.y = 0; // 锁定 Y 轴，防止歪头
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }
}