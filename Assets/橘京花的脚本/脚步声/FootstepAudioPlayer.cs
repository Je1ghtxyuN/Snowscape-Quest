using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepAudioPlayer : MonoBehaviour
{
    [SerializeField] private FootstepAudioData footstepAudioData;
    [SerializeField] private float raycastDistance = 0.02f;
    [SerializeField] private float footstepDistance = 2f; // 新增：移动距离阈值

    private AudioSource audioSource;
    private float lastFootstepTime = -1f;
    private string currentSurfaceTag = "Default";
    private Vector3 lastPosition;
    private float distanceMoved;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        CheckGroundSurface();

        // 移动距离检测
        distanceMoved += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (distanceMoved >= footstepDistance)
        {
            PlayFootstep();
            distanceMoved = 0f;
        }
    }

    private void CheckGroundSurface()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance))
        {
            currentSurfaceTag = hit.collider.tag;
            Debug.Log("检测到地面标签: " + currentSurfaceTag);
        }
        else
        {
            currentSurfaceTag = "Default";
        }
    }

    public void PlayFootstep()
    {
        if (footstepAudioData == null)
        {
            Debug.LogError("FootstepAudioData未分配！");
            return;
        }

        FootstepAudio footstepAudio = FindFootstepAudio(currentSurfaceTag);

        if (footstepAudio == null)
        {
            Debug.LogWarning($"找不到标签为 '{currentSurfaceTag}' 的脚步声数据");
            return;
        }

        if (footstepAudio.AudioClips == null || footstepAudio.AudioClips.Count == 0)
        {
            Debug.LogWarning($"标签 '{currentSurfaceTag}' 的AudioClips列表为空");
            return;
        }

        if (lastFootstepTime >= 0 && Time.time - lastFootstepTime < footstepAudio.Delay)
        {
            // Debug.Log($"脚步声延迟中..."); // 注释掉避免刷屏
            return;
        }

        AudioClip clip = footstepAudio.AudioClips[Random.Range(0, footstepAudio.AudioClips.Count)];

        // --- 修改开始：检测标签并调整音量 ---
        float volumeScale = 1.0f; // 默认音量为 1.0 (100%)

        // 使用 OrdinalIgnoreCase 忽略大小写，这样 "snow", "Snow", "SNOW" 都可以识别
        if (currentSurfaceTag.Equals("Snow", System.StringComparison.OrdinalIgnoreCase))
        {
            volumeScale = 0.6f; // 如果是雪地，音量设为 0.6
        }
        // 你可以在这里继续添加其他 else if ...

        // PlayOneShot 的第二个参数控制音量 (0.0 - 1.0)
        audioSource.PlayOneShot(clip, volumeScale);
        // --- 修改结束 ---

        lastFootstepTime = Time.time;
        Debug.Log($"成功播放脚步声: {clip.name} (标签: {currentSurfaceTag}, 音量: {volumeScale})");
    }

    private FootstepAudio FindFootstepAudio(string tag)
    {
        if (footstepAudioData.FootstepAudio == null)
        {
            Debug.LogError("FootstepAudio列表未初始化");
            return null;
        }

        foreach (var footstep in footstepAudioData.FootstepAudio)
        {
            if (footstep.Tag.Equals(tag, System.StringComparison.OrdinalIgnoreCase))
            {
                return footstep;
            }
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * raycastDistance);
    }

    // 动画事件调用的方法
    public void OnFootstepEvent()
    {
        PlayFootstep();
    }
}