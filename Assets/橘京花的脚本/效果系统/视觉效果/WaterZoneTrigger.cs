using UnityEngine;

public class WaterZoneTrigger : MonoBehaviour
{
    [Header("触发设置")]
    public string playerTag = "Player";
    public string waterTag = "water";

    [Header("效果引用")]
    public VRLensEffectManager effectManager;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && effectManager != null)
        {
            effectManager.EnterWaterEffect();
            Debug.Log($"玩家进入水域: {gameObject.name}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && effectManager != null)
        {
            effectManager.ExitWaterEffect();
            Debug.Log($"玩家离开水域: {gameObject.name}");
        }
    }

    void OnValidate()
    {
        // 自动设置tag
        if (gameObject.CompareTag(waterTag) == false)
        {
            gameObject.tag = waterTag;
        }

        // 自动添加碰撞体
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            gameObject.AddComponent<BoxCollider>();
            GetComponent<Collider>().isTrigger = true;
        }
    }
}