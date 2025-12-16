using UnityEngine;

public class WaterfallVoiceTrigger : MonoBehaviour
{
    [Tooltip("确保这个物体有 Collider 并且 IsTrigger 勾选了")]
    private void OnTriggerEnter(Collider other)
    {
        // 调试日志：看看是谁撞了我
        // Debug.Log($"💦 瀑布触发器检测到碰撞: {other.name} (Tag: {other.tag})");

        // 检测是否是玩家进入
        if (other.CompareTag("Player"))
        {
            if (PlayerVoiceSystem.Instance != null)
            {
                // ⭐ 改动：使用 PlayVoiceOnce，确保只播一次
                PlayerVoiceSystem.Instance.PlayVoiceOnce("Enter_Waterfall");
            }
        }
    }
}