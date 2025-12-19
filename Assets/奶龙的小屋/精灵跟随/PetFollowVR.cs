using UnityEngine;
using System.Collections;

public class PetFollowVR : MonoBehaviour
{
    public Transform playerTarget;
    public float followSpeed = 2.0f;
    public float stopDistance = 1.5f;
    public float rotationSpeed = 5.0f;
    public float playerMovementThreshold = 0.05f;
    public float smoothTime = 0.3f; // 新增：平滑延迟时间[6](@ref)

    private Animator petAnimator;
    private bool isTouchingPlayer = false;
    private Vector3 lastPlayerPosition;
    private bool isPlayerMoving = false;
    private Vector3 targetPosition; // 改为世界坐标目标位置
    private Vector3 currentVelocity = Vector3.zero; // 用于SmoothDamp[6](@ref)

    void Start()
    {
        petAnimator = GetComponent<Animator>();

        if (playerTarget == null)
            playerTarget = GameObject.FindGameObjectWithTag("Player").transform;

        // 不再设置为玩家子物体，保持独立
        // transform.SetParent(null);

        // 初始化位置为玩家后方
        Vector3 playerForward = playerTarget.forward;
        targetPosition = playerTarget.position - playerForward * stopDistance;
        transform.position = targetPosition;

        // 初始面向玩家
        transform.LookAt(playerTarget.position);

        lastPlayerPosition = playerTarget.position;
    }

    void LateUpdate() // 改为LateUpdate确保在玩家移动后执行[3](@ref)
    {
        if (isTouchingPlayer) return;

        CheckPlayerMovement();
        FollowPlayer();
    }

    void CheckPlayerMovement()
    {
        float movement = Vector3.Distance(playerTarget.position, lastPlayerPosition);
        isPlayerMoving = movement > playerMovementThreshold;
        lastPlayerPosition = playerTarget.position;
    }

    void FollowPlayer()
    {
        // 计算理想的目标位置（玩家后方保持距离）
        Vector3 playerForward = playerTarget.forward;
        Vector3 desiredPosition = playerTarget.position - playerForward * stopDistance;

        // 使用SmoothDamp实现平滑延迟跟随[6](@ref)
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition,
            ref currentVelocity, smoothTime, followSpeed);

        // 计算与玩家的实际距离
        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > stopDistance * 1.1f) // 增加缓冲范围
        {
            petAnimator.SetBool("IsMoving", true);
        }
        else
        {
            petAnimator.SetBool("IsMoving", false);
        }

        // 智能旋转
        RotatePetIntelligently();
    }

    void RotatePetIntelligently()
    {
        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        Vector3 targetDirection;

        if (isPlayerMoving)
        {
            // 玩家移动时：面向玩家移动方向，增加自然感
            Vector3 playerMovementDirection = (playerTarget.position - lastPlayerPosition).normalized;
            if (playerMovementDirection != Vector3.zero)
            {
                targetDirection = playerMovementDirection;
            }
            else
            {
                targetDirection = directionToPlayer;
            }
        }
        else
        {
            // 玩家停止时：面向玩家本身
            targetDirection = directionToPlayer;
        }

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(targetDirection.x, 0, targetDirection.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isTouchingPlayer = true;
            petAnimator.SetBool("IsMoving", false);
            petAnimator.SetTrigger("TouchPlayer");
            StartCoroutine(ResetTouchingStateAfterAnimation());
        }
    }

    IEnumerator ResetTouchingStateAfterAnimation()
    {
        yield return new WaitForSeconds(2.0f);
        ResetTouchingState();
    }

    void ResetTouchingState()
    {
        isTouchingPlayer = false;
    }
}