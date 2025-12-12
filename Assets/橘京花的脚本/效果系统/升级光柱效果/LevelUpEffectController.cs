using UnityEngine;
using System.Collections;

public class LevelUpEffectController : MonoBehaviour
{
    [Header("自动引用")]
    public Transform outerMesh;
    public Transform innerMesh;
    public ParticleSystem iceParticles;
    public AudioSource audioSource;

    [Header("配置")]
    public float riseTime = 1.0f;
    public float stayTime = 2.0f;
    public float maxHeight = 6.0f;
    public AudioClip sfx;



    void Start()
    {
        StartCoroutine(AnimateProcess());
    }

    IEnumerator AnimateProcess()
    {
        // 1. 初始化
        float currentHeight = 0.1f;
        UpdateHeight(currentHeight);

        if (iceParticles) iceParticles.Play();
        if (audioSource && sfx) audioSource.PlayOneShot(sfx);

        // 2. 升起
        float timer = 0f;
        while (timer < riseTime)
        {
            timer += Time.deltaTime;
            float t = timer / riseTime;
            // 弹性缓动
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            currentHeight = Mathf.Lerp(0.1f, maxHeight, smoothT);
            UpdateHeight(currentHeight);
            
            yield return null;
        }

        // 3. 停留
        yield return new WaitForSeconds(stayTime);

        // 4. 停止粒子
        if (iceParticles) iceParticles.Stop();

        // 5. 消失 (缩小)
        timer = 0f;
        float shrinkTime = 0.5f;
        while (timer < shrinkTime)
        {
            timer += Time.deltaTime;
            float t = timer / shrinkTime;
            
            currentHeight = Mathf.Lerp(maxHeight, 0.1f, t);
            UpdateHeight(currentHeight);

            yield return null;
        }

        Destroy(gameObject, 1.0f);
    }

    void UpdateHeight(float h)
    {
        // 保持中心在地板上
        float posY = h / 2f;

        // 外壳：Scale XZ = 4
        if (outerMesh)
        {
            outerMesh.localScale = new Vector3(4f, h, 4f);
            outerMesh.localPosition = new Vector3(0, posY, 0);
        }

        // 内胆：Scale XZ = 3.9 (正数！因为网格已经翻转了)
        if (innerMesh)
        {
            innerMesh.localScale = new Vector3(3.9f, h, 3.9f);
            innerMesh.localPosition = new Vector3(0, posY, 0);
        }
    }
}