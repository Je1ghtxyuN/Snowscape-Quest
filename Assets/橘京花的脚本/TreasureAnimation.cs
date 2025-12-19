using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TreasureAnimation : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private GameObject successUIPrefab;

    private Camera vrCamera;
    private GameObject successUIInstance;
    private bool success = false;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        
        vrCamera = Camera.main;
    }

    public void Open()
    {
        if (success == false)
        {
            animator.SetBool("open", true);
            Debug.Log("宝箱打开");

            Invoke("ShowSuccessUI", 1f);//延迟展示成功UI
            success = true;
        }
        else return;
    }

    private void ShowSuccessUI()
    {
        successUIInstance = Instantiate(
            successUIPrefab,
            vrCamera.transform.position + vrCamera.transform.forward * 1.5f + (-vrCamera.transform.up) * 0.5f,
            vrCamera.transform.rotation
        );

        // 调整UI位置和大小
        //successUIInstance.transform.localScale = Vector3.one * 0.003f;

    }
}
