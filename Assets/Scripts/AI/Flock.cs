using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flock : MonoBehaviour
{
    [SerializeField] float minSpeed = 0.5f;
    [SerializeField] float maxSpeed = 1.0f;
    private float speed;
    [SerializeField] float rotationSpeed= 4.0f;

    GlobalFlock parent;
    [SerializeField] float neighborDistance = 3.0f;
    [SerializeField] float avoidDistance = 1.0f;


    private void Start()
    {
        parent = GetComponentInParent<GlobalFlock>();
        speed = Random.Range(minSpeed, maxSpeed);
    }
    private void Update()
    {

        
        if (!parent.GetBounds().Contains(transform.position))
        {
            Vector3 direction = parent.transform.position - transform.position;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                  Quaternion.LookRotation(direction),
                                                  rotationSpeed * Time.deltaTime);

            speed = Random.Range(minSpeed, maxSpeed);
        }
        else if (Random.Range(0, 5) < 1) ApplyRules();

        transform.Translate(0, 0, Time.deltaTime * speed);
    }

    // rules for flocking algorithm. based on bird algo from lion king
    private void ApplyRules()
    {
        GameObject[] flock = parent.GetFish();
        Vector3 vCenter = Vector3.zero;
        Vector3 vAvoid = Vector3.zero;
        float gSpeed = 0.1f;

        Vector3 goal = parent.GetGoal();
        float distance;
        int groupSize = 0;

        foreach(GameObject fish in flock)
        {
            if (fish == this.gameObject) continue;
            distance = Vector3.Distance(fish.transform.position, this.transform.position);
            if (distance <= neighborDistance)
            {
                vCenter += fish.transform.position;
                groupSize++;

                // more efficient than hbox detection
                if(distance < avoidDistance) vAvoid += this.transform.position - fish.transform.position;

                Flock otherFish = fish.GetComponent<Flock>();
                gSpeed += otherFish.GetSpeed();
            }
        }

        if (groupSize > 0)
        {
            vCenter = vCenter / groupSize + (goal - this.transform.position);
            speed = gSpeed / groupSize;
            Vector3 moveDirection = (vCenter + vAvoid) - transform.position;
            if (moveDirection == Vector3.zero) return;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                  Quaternion.LookRotation(moveDirection),
                                                  rotationSpeed * Time.deltaTime);
        }
    }

    public float GetSpeed() => speed;
}
