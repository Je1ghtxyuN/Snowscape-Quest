using UnityEngine;
using UnityEngine.InputSystem;

public class VREffectTester : MonoBehaviour
{
    private VRLensEffectManager effectManager;
    private Keyboard keyboard; // 新的输入系统引用

    void Start()
    {
        effectManager = FindObjectOfType<VRLensEffectManager>();
        keyboard = Keyboard.current; // 获取键盘引用

        if (effectManager == null)
        {
            Debug.LogError("未找到VRLensEffectManager！请检查挂载。");
        }
        else
        {
            Debug.Log("VR效果管理器找到成功！");
        }
    }

    void Update()
    {
        if (keyboard == null) return;

        // 使用新的Input System检测按键
        if (keyboard.digit1Key.wasPressedThisFrame && effectManager != null)
        {
            effectManager.EnterWaterEffect();
            Debug.Log("测试：进入水域效果");
        }

        if (keyboard.digit2Key.wasPressedThisFrame && effectManager != null)
        {
            effectManager.ExitWaterEffect();
            Debug.Log("测试：离开水域效果");
        }

        if (keyboard.digit3Key.wasPressedThisFrame && effectManager != null)
        {
            // 切换效果测试
            if (effectManager.isInWater)
                effectManager.ExitWaterEffect();
            else
                effectManager.EnterWaterEffect();
        }
    }
}