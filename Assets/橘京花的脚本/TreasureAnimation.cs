using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureAnimation : MonoBehaviour
{
    private Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Open()
    {
        animator.SetBool("open",true);
        Debug.Log("±¦Ïä´ò¿ª");
    }
}
