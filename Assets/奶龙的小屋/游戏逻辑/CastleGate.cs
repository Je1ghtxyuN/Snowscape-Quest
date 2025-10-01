using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class CastleGate : MonoBehaviour
{
    [Header("旋转设置")]
    public float rotationAngle = 105f; // 旋转角度
    public float rotationDuration = 2.0f; // 旋转持续时间

    [Header("门扇模型引用")]
    public Transform leftDoor; // 左侧门扇
    public Transform rightDoor; // 右侧门扇

    [Header("开门条件")]
    public int requiredScore = 10; // 开启大门所需的分数

    private Vector3 leftDoorInitialRotation; // 左侧门扇初始旋转
    private Vector3 rightDoorInitialRotation; // 右侧门扇初始旋转
    private Vector3 leftDoorTargetRotation; // 左侧门扇目标旋转
    private Vector3 rightDoorTargetRotation; // 右侧门扇目标旋转

    private bool isRotating = false; // 是否正在旋转

    [Header("碰撞体设置")]
    public Collider specificColliderToRemove; // 大门碰撞体

    [Header("音效设置")]
    public AudioClip openSound; // 开门音效
    private AudioSource audioSource;

    [Header("触发器设置")]
    public Collider triggerCollider; // 触发器碰撞体
    private bool hasOpened = false; // 防止重复触发

    // 全局分数系统（单例模式）
    private static ScoreSystem scoreSystemInstance;
    public static ScoreSystem ScoreSystem
    {
        get
        {
            if (scoreSystemInstance == null)
            {
                scoreSystemInstance = new ScoreSystem();
            }
            return scoreSystemInstance;
        }
    }

    void Start()
    {
        // 保存初始旋转
        leftDoorInitialRotation = leftDoor.localEulerAngles;
        rightDoorInitialRotation = rightDoor.localEulerAngles;

        // 计算目标旋转
        leftDoorTargetRotation = leftDoorInitialRotation + new Vector3(0, 0, rotationAngle);
        rightDoorTargetRotation = rightDoorInitialRotation + new Vector3(0, 0, -rotationAngle);

        // 自动获取音频源组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 3D音效需要改成1
        }

        // 确保触发器已启用
        if (triggerCollider != null) triggerCollider.isTrigger = true;

        // 调试信息
        UnityEngine.Debug.Log($"左侧门扇初始旋转: {leftDoorInitialRotation}, 目标旋转: {leftDoorTargetRotation}");
        UnityEngine.Debug.Log($"右侧门扇初始旋转: {rightDoorInitialRotation}, 目标旋转: {rightDoorTargetRotation}");
    }

    // 检测分数是否达标
    private bool ScoreReached()
    {
        return ScoreSystem.CurrentScore >= requiredScore;
    }

    // 触发器进入检测
    private void OnTriggerEnter(Collider other)
    {
        if (hasOpened) return;

        // 仅玩家触发且分数达标
        if (other.CompareTag("Player") && ScoreReached())
        {
            StartCoroutine(OpenGateSequence());
            UnityEngine.Debug.Log("触发开门");
        }
    }

    // 开门协程（平滑旋转+音效）
    private IEnumerator OpenGateSequence()
    {
        hasOpened = true;
        isRotating = true;

        // 播放开门音效
        if (openSound != null)
        {
            audioSource.PlayOneShot(openSound);
            UnityEngine.Debug.Log("音效播放");
        }

        // 平滑旋转门扇
        float elapsedTime = 0f;
        Vector3 leftDoorStartRot = leftDoor.localEulerAngles;
        Vector3 rightDoorStartRot = rightDoor.localEulerAngles;

        UnityEngine.Debug.Log($"开始旋转: 左门从 {leftDoorStartRot} 到 {leftDoorTargetRotation}");
        UnityEngine.Debug.Log($"开始旋转: 右门从 {rightDoorStartRot} 到 {rightDoorTargetRotation}");

        while (elapsedTime < rotationDuration)
        {
            // 计算插值比例
            float t = elapsedTime / rotationDuration;
            // 使用平滑的插值函数
            t = Mathf.SmoothStep(0f, 1f, t);

            // 更新门扇的旋转角度
            leftDoor.localEulerAngles = Vector3.Lerp(leftDoorStartRot, leftDoorTargetRotation, t);
            rightDoor.localEulerAngles = Vector3.Lerp(rightDoorStartRot, rightDoorTargetRotation, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 确保最终旋转角度准确
        leftDoor.localEulerAngles = leftDoorTargetRotation;
        rightDoor.localEulerAngles = rightDoorTargetRotation;
        isRotating = false;

        UnityEngine.Debug.Log($"旋转完成，左门最终旋转: {leftDoor.localEulerAngles}");
        UnityEngine.Debug.Log($"旋转完成，右门最终旋转: {rightDoor.localEulerAngles}");

        // 去除大门碰撞体
        if (specificColliderToRemove != null)
        {
            specificColliderToRemove.enabled = false;
        }
        else
        {
            UnityEngine.Debug.LogWarning("未找到碰撞体引用");
        }

        // 禁用触发器防止重复触发
        if (triggerCollider != null)
            triggerCollider.enabled = false;
    }
}

// 全局分数系统
public class ScoreSystem
{
    private int currentScore = 0;

    public int CurrentScore => currentScore;

    // 增加分数（普通敌人调用）
    public void AddRegularEnemyScore()
    {
        currentScore += 1;
        UnityEngine.Debug.Log($"普通敌人被击败！当前分数: {currentScore}");
    }

    // 增加分数（精英敌人调用）
    public void AddEliteEnemyScore()
    {
        currentScore += 2;
        UnityEngine.Debug.Log($"精英敌人被击败！当前分数: {currentScore}");
    }

    // 重置分数
    public void ResetScore()
    {
        currentScore = 0;
    }
}