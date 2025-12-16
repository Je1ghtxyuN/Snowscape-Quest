using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class PlayerVoiceSystem : MonoBehaviour
{
    public static PlayerVoiceSystem Instance { get; private set; }

    [System.Serializable]
    public struct VoiceClip
    {
        public string id;       // ID例如: "Leave_Water", "Get_Armor"
        public AudioClip clip;
        [TextArea] public string subtitle; // 备注字幕
    }

    [Header("玩家心理暗示语音库")]
    public List<VoiceClip> voiceLibrary = new List<VoiceClip>();

    [Header("设置")]
    [Range(0f, 3f)] // ⭐ 改动：范围扩大到3，方便放大音量
    public float volume = 1.5f; // ⭐ 改动：默认给大一点

    private AudioSource audioSource;

    // ⭐ 新增：记录已经播放过的语音ID，防止重复播放
    private HashSet<string> playedVoiceIDs = new HashSet<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();

        // 玩家内心的声音是 2D 的
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
    }

    public void PlayVoice(string id)
    {
        PlayClipInternal(id);
    }

    // ⭐ 新增：只播放一次的方法
    public void PlayVoiceOnce(string id)
    {
        if (playedVoiceIDs.Contains(id))
        {
            // 如果已经播过，直接忽略
            return;
        }

        if (PlayClipInternal(id))
        {
            playedVoiceIDs.Add(id); // 记录下来
            Debug.Log($"🧠 [PlayerVoice] ID: {id} 已标记为不再重复播放");
        }
    }

    // 内部播放逻辑
    private bool PlayClipInternal(string id)
    {
        VoiceClip clipData = voiceLibrary.Find(x => x.id == id);

        if (clipData.clip != null)
        {
            audioSource.PlayOneShot(clipData.clip, volume);
            Debug.Log($"🧠 [PlayerVoice] 播放语音: {clipData.subtitle}");
            return true;
        }
        else
        {
            Debug.LogWarning($"⚠️ [PlayerVoice] 找不到 ID 为 {id} 的语音！");
            return false;
        }
    }

    [ContextMenu("Test Play First")]
    public void TestPlay()
    {
        if (voiceLibrary.Count > 0) PlayVoice(voiceLibrary[0].id);
    }
}