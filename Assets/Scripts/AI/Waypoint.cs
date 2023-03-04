using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    // set waypoint type? door etc

    [SerializeField] Waypoint[] neighbors;
    [SerializeField] Color currentColor = Color.white;
    public Waypoint[] Neighbors => neighbors;
    public void ChangeGizmoColor(Color newColor) => currentColor = newColor;
    
    private void OnDrawGizmos()
    {
        Gizmos.color = currentColor;
        Gizmos.DrawWireSphere(transform.position, 1.0f);
        Gizmos.DrawIcon(transform.position, name);

        Gizmos.color = Color.green;
        foreach (var neighbor in neighbors)
        {
            Gizmos.DrawLine(transform.position, neighbor.transform.position);
        }
    }

    

}
