using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class PetVoiceSystem : MonoBehaviour
{
    public static PetVoiceSystem Instance { get; private set; }

    [System.Serializable]
    public struct VoiceClip
    {
        public string id;       // 语音ID (如 "Start", "EnemySpotted")
        public AudioClip clip;  // 音频文件
        [TextArea] public string subtitle; // 字幕 (可选)
    }

    [Header("语音库配置")]
    public List<VoiceClip> voiceLibrary = new List<VoiceClip>();

    private AudioSource audioSource;
    private bool isSpeaking = false;

    private float lastEnemySpottedTime = -999f;

    private bool hasPlayedFirstDrop = false;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f; // 3D音效，声音从小精灵发出
    }

    // 供外部调用：PetVoiceSystem.Instance.PlayVoice("EnemyFound");
    public void PlayVoice(string id, float delay = 0f)
    {
        StartCoroutine(PlayVoiceRoutine(id, delay));
    }

    public void TryPlayEnemySpottedVoice()
    {
        if (Time.time - lastEnemySpottedTime > 15f)
        {
            PlayVoice("FindEnemy");
            lastEnemySpottedTime = Time.time;
        }
    }

    public void TryPlayFirstDropVoice()
    {
        // 只有当标志位为 false (没播过) 时才播放
        if (!hasPlayedFirstDrop)
        {
            PlayVoice("Tutorial6");
            hasPlayedFirstDrop = true; // 锁死，下次不再播
        }
    }


    private IEnumerator PlayVoiceRoutine(string id, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        // 查找语音
        VoiceClip target = voiceLibrary.Find(x => x.id == id);

        // 如果正在说话，是否打断？这里选择不打断，或者你可以根据优先级打断
        if (target.clip != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(target.clip);
            Debug.Log($"[Paimon]: {target.subtitle}");
            // 这里可以对接你的字幕 UI 系统
        }
        else
        {
            Debug.LogWarning($"语音ID未找到或正在说话: {id}");
        }
    }
}