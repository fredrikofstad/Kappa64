using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalFlock : MonoBehaviour
{
    [SerializeField] int fishNumber = 10;
    [SerializeField] GameObject[] koiFishPrefab;
    [SerializeField] GameObject[] koiFish;
    [SerializeField] Transform target = null;

    Bounds bounds;

    [SerializeField] Transform goal;


    void Start()
    {
        bounds = GetComponent<Collider>().bounds;
        koiFish = new GameObject[fishNumber];
        int fishIndex = 0;
        for(int i = 0; i < fishNumber; i++)
        {
            if (fishIndex > koiFishPrefab.Length - 1) fishIndex = 0;
            Vector3 pos = new Vector3(Random.Range(-bounds.extents.x, bounds.extents.x),
                                      Random.Range(-bounds.extents.y, bounds.extents.y),
                                      Random.Range(-bounds.extents.z, bounds.extents.z)
                                      );
            koiFish[i] = Instantiate(koiFishPrefab[fishIndex], transform);
            koiFish[i].transform.localPosition = pos;
            fishIndex++;
        }
    }

    void Update()
    {
        if(Random.Range(0, 100000) < 50)
        {
            goal.localPosition = new Vector3(Random.Range(-bounds.extents.x, bounds.extents.x),
                               Random.Range(-bounds.extents.y, bounds.extents.y),
                               Random.Range(-bounds.extents.z, bounds.extents.z)
                               );
        }
        if(target != null) target.position = GetMeanVector(koiFish);



    }

    private Vector3 GetMeanVector(GameObject[] fish)
    {
        if (fish.Length == 0)
        {
            return Vector3.zero;
        }

        Vector3 meanVector = Vector3.zero;

        foreach (GameObject pos in fish)
        {
            meanVector += pos.transform.position;
        }

        return (meanVector / fish.Length);
    }

    public GameObject[] GetFish() => koiFish;
    public Vector3 GetGoal() => goal.position;
    public Bounds GetBounds() => bounds;

}
