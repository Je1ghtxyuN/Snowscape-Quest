using UnityEngine;

public class VoiceTriggerZone : MonoBehaviour
{
    [Header("语音设置")]
    [Tooltip("在 PetVoiceSystem 中配置的语音ID")]
    public string voiceID = "Tutorial_Move";

    [Tooltip("延迟几秒播放")]
    public float delay = 0.5f;

    [Header("触发设置")]
    public bool triggerOnce = true; // 是否只触发一次

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;

        if (other.CompareTag("Player"))
        {
            if (PetVoiceSystem.Instance != null)
            {
                PetVoiceSystem.Instance.PlayVoice(voiceID, delay);
                hasTriggered = true;

                // 如果只触发一次，可以直接销毁这个触发器省性能
                if (triggerOnce) Destroy(gameObject, delay + 1f);
            }
        }
    }
}