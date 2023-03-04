using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdFlap : MonoBehaviour
{
    Animator animator;
    private float lastY;

    void Start()
    {
        animator = GetComponent<Animator>();
        lastY = gameObject.transform.position.y;
    }

    void Update()
    {
        animator.SetBool("Flap", lastY < gameObject.transform.position.y);
        lastY = gameObject.transform.position.y;
    }
}
