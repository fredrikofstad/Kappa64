using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blink : MonoBehaviour
{

    [SerializeField] float duration = 3.0f;
    [SerializeField, Range(0,5)] float randomRange = 2.0f;
    [SerializeField] float[] offset;

    [SerializeField] Renderer rend;
    Material material;

    float randomNumber;

    void Start()
    {
        material = rend.material;
        offset = new float[] { 0.0f, 0.125f, 0.25f };
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        while(true)
        {
            RandomizeNumber();
            yield return new WaitForSeconds(duration + randomNumber);
            //half blink
            material.SetTextureOffset("_MainTex", new Vector2(offset[1], 0));
            yield return new WaitForSeconds(0.1f);
            //blink
            material.SetTextureOffset("_MainTex", new Vector2(offset[2], 0));
            yield return new WaitForSeconds(0.1f);
            //half blink
            material.SetTextureOffset("_MainTex", new Vector2(offset[1], 0));
            yield return new WaitForSeconds(0.1f);
            //normal
            material.SetTextureOffset("_MainTex", new Vector2(offset[0], 0));
        }
    }

    void RandomizeNumber()
    {
        randomNumber = Random.Range(0.0f, randomRange);
    }
}