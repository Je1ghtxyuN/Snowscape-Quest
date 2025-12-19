using UnityEngine;
using System.Collections;

public class CastleGate : MonoBehaviour
{
    [Header("旋转设置")]
    public float rotationAngle = 105f;
    public float rotationDuration = 2.0f;

    [Header("门扇模型")]
    public Transform leftDoor;
    public Transform rightDoor;

    private Vector3 leftDoorInitialRotation;
    private Vector3 rightDoorInitialRotation;
    private Vector3 leftDoorTargetRotation;
    private Vector3 rightDoorTargetRotation;
    private bool isRotating = false;

    [Header("碰撞体设置")]
    public Collider specificColliderToRemove;

    [Header("音效设置")]
    public AudioClip openSound;
    private AudioSource audioSource;

    [Header("触发器设置")]
    public Collider triggerCollider;
    private bool hasOpened = false;

    void Start()
    {
        leftDoorInitialRotation = leftDoor.localEulerAngles;
        rightDoorInitialRotation = rightDoor.localEulerAngles;
        leftDoorTargetRotation = leftDoorInitialRotation + new Vector3(0, 0, rotationAngle);
        rightDoorTargetRotation = rightDoorInitialRotation + new Vector3(0, 0, -rotationAngle);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 建议改为1f以启用3D音效
        }

        if (triggerCollider != null) triggerCollider.isTrigger = true;
    }

    // ⭐ 核心修改：检查是否所有回合已结束
    private bool CanOpenGate()
    {
        if (GameRoundManager.Instance == null) return false;
        return GameRoundManager.Instance.isGameComplete;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasOpened) return;

        // ⭐ 修改：判断条件改为 CanOpenGate()
        if (other.CompareTag("Player") && CanOpenGate())
        {
            StartCoroutine(OpenGateSequence());
            Debug.Log("大门开启！");
        }
        else if (other.CompareTag("Player") && !CanOpenGate())
        {
            Debug.Log("门是锁着的，还需要清空更多敌人。");
        }
    }

    private IEnumerator OpenGateSequence()
    {
        hasOpened = true;
        isRotating = true;

        if (openSound != null) audioSource.PlayOneShot(openSound);

        float elapsedTime = 0f;
        Vector3 leftDoorStartRot = leftDoor.localEulerAngles;
        Vector3 rightDoorStartRot = rightDoor.localEulerAngles;

        while (elapsedTime < rotationDuration)
        {
            float t = elapsedTime / rotationDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            leftDoor.localEulerAngles = Vector3.Lerp(leftDoorStartRot, leftDoorTargetRotation, t);
            rightDoor.localEulerAngles = Vector3.Lerp(rightDoorStartRot, rightDoorTargetRotation, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        leftDoor.localEulerAngles = leftDoorTargetRotation;
        rightDoor.localEulerAngles = rightDoorTargetRotation;
        isRotating = false;

        if (specificColliderToRemove != null) specificColliderToRemove.enabled = false;
        if (triggerCollider != null) triggerCollider.enabled = false;
    }
}