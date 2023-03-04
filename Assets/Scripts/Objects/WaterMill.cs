using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterMill : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 180f;
    [SerializeField] Transform mill;


    // Update is called once per frame
    void Update()
    {
        mill.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
