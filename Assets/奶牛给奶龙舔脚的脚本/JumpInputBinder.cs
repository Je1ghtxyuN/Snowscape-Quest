using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class JumpInputBinder : MonoBehaviour
{
    [SerializeField] private XRController leftHandController;
    [SerializeField] private XRController rightHandController;
    [SerializeField] private InputActionReference jumpActionReference;

    private void Start()
    {
        // 确保跳跃动作已启用
        jumpActionReference.action.Enable();

       
        jumpActionReference.action.AddBinding("<XRController>{RightHand}/primaryButton");
    }
}