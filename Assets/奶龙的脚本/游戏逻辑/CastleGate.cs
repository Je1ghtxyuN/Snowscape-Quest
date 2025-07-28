using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class CastleGate : MonoBehaviour
{
    [Header("动画控制")]
    public Animator gateAnimator; // 绑定大门Animator组件
    public string openTrigger = "Open"; // 动画触发器参数名

    [Header("音效设置")]
    public AudioClip openSound; // 开门音效
    private AudioSource audioSource;

    [Header("触发器设置")]
    public Collider triggerCollider; // 触发器碰撞体
    private bool hasOpened = false; // 防止重复触发

    void Start()
    {
        // 自动获取音频源组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 3D音效需要改成1
        }

        // 确保触发器已启用
        if (triggerCollider != null) triggerCollider.isTrigger = true;
    }

    // 检测敌人是否存在
    private bool NoEnemiesInScene()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        return enemies.Length == 0;
    }

    // 触发器进入检测
    private void OnTriggerEnter(Collider other)
    {
        if (hasOpened) return;

        // 仅玩家触发且场景无敌人
        if (other.CompareTag("Player") && NoEnemiesInScene())
        {
            StartCoroutine(OpenGateSequence());
            UnityEngine.Debug.Log("触发开门");
        }
    }

    // 开门协程（动画+音效）
    private IEnumerator OpenGateSequence()
    {
        hasOpened = true;

        // 播放开门动画
        if (gateAnimator != null)
        {
            gateAnimator.SetTrigger(openTrigger);
            UnityEngine.Debug.Log("动画播放");
            yield return null; // 确保动画触发
        }

        // 播放开门音效
        if (openSound != null)
        {
            audioSource.PlayOneShot(openSound);
            UnityEngine.Debug.Log("音效播放");
            yield return new WaitForSeconds(openSound.length);
        }

        // 可选：禁用触发器防止重复触发
        triggerCollider.enabled = false;
    }
}