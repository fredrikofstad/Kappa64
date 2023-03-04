using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 public class Shadow : MonoBehaviour
 {
    [SerializeField] float raycastLength = 10;
    [SerializeField] float upDecalFromFloor = 0.05f;
    [SerializeField] float maxShadowSize = 2.5f;
    [SerializeField] float distanceFromKappa;
    [SerializeField] Transform target;

    private void Update ()
    {
        Ray ray = new Ray (transform.position + ( Vector3.up * raycastLength ), Vector3.down * raycastLength);
        if ( Physics.Raycast (ray, out var hit))
        {
            transform.position = hit.point + (Vector3.up * upDecalFromFloor);
            transform.LookAt (transform.position + hit.normal );
            
            Debug.DrawLine (hit.point, hit.point + (hit.normal * 5));
            distanceFromKappa = target.position.y - transform.position.y;
            ScaleShadow(distanceFromKappa);
        }
    }

    void ScaleShadow(float distanceFromGround)
    {
        distanceFromGround = Mathf.Clamp(distanceFromGround, 0.0001f, 3);

        // Normalize distance
        float normalizedHeight = distanceFromGround / 3;

        float scaleFactor = Mathf.Clamp(maxShadowSize / normalizedHeight, 0.01f, maxShadowSize);
        transform.localScale = Vector3.one * scaleFactor;
    }

}
