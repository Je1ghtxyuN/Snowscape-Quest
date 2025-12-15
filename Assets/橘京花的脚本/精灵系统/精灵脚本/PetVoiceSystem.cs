using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class PetVoiceSystem : MonoBehaviour
{
    public static PetVoiceSystem Instance { get; private set; }

    // 供外部查询：是否正在说话
    public bool IsSpeaking
    {
        get { return audioSource != null && audioSource.isPlaying; }
    }

    [System.Serializable]
    public struct VoiceClip
    {
        public string id;
        public AudioClip clip;
        [TextArea] public string subtitle;
    }

    private struct QueuedVoice
    {
        public string id;
        public float delay;

        public QueuedVoice(string id, float delay)
        {
            this.id = id;
            this.delay = delay;
        }
    }

    [Header("语音库配置")]
    public List<VoiceClip> voiceLibrary = new List<VoiceClip>();

    [Header("音量设置")]
    [Range(0f, 2f)]
    public float masterVolume = 1.0f;

    private AudioSource audioSource;
    private Queue<QueuedVoice> voiceQueue = new Queue<QueuedVoice>();
    private bool isProcessingQueue = false;
    private float lastEnemySpottedTime = -999f;
    private bool hasPlayedFirstDrop = false;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();

        audioSource.spatialBlend = 1.0f;
        audioSource.minDistance = 5.0f;
        audioSource.maxDistance = 100.0f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    // --- 播放逻辑保持不变 ---
    public void PlayVoice(string id, float delay = 0f)
    {
        voiceQueue.Enqueue(new QueuedVoice(id, delay));
        if (!isProcessingQueue) StartCoroutine(ProcessVoiceQueue());
    }

    public void TryPlayEnemySpottedVoice()
    {
        if (Time.time - lastEnemySpottedTime > 15f)
        {
            PlayVoice("Enemy_Spotted");
            lastEnemySpottedTime = Time.time;
        }
    }

    public void TryPlayFirstDropVoice()
    {
        if (!hasPlayedFirstDrop)
        {
            PlayVoice("First_Drop");
            hasPlayedFirstDrop = true;
        }
    }

    private IEnumerator ProcessVoiceQueue()
    {
        isProcessingQueue = true;
        while (voiceQueue.Count > 0)
        {
            QueuedVoice currentRequest = voiceQueue.Dequeue();
            if (currentRequest.delay > 0) yield return new WaitForSeconds(currentRequest.delay);

            VoiceClip target = voiceLibrary.Find(x => x.id == currentRequest.id);
            if (target.clip != null)
            {
                audioSource.PlayOneShot(target.clip, masterVolume);
                yield return new WaitForSeconds(target.clip.length + 0.2f);
            }
            else yield return null;
        }
        isProcessingQueue = false;
    }
}