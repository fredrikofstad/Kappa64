using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mouth : MonoBehaviour
{
    [SerializeField] float[] offset;

    [SerializeField] Renderer rend;
    Material material;

    void Start()
    {
        material = rend.material;
        offset = new float[] { 0.0f, 0.125f, 0.25f };
    }

    public void Eat()
    {
        StartCoroutine(EatAnimation());
    }

    public void Drink(float duration)
    {
        StartCoroutine(DrinkAnimation(duration));
    }

    IEnumerator EatAnimation()
    {
        //open
        material.SetTextureOffset("_MainTex", new Vector2(offset[1], 0));
        yield return new WaitForSeconds(0.3f);
        //close
        material.SetTextureOffset("_MainTex", new Vector2(offset[0], 0));
        yield return new WaitForSeconds(0.2f);
        //open
        material.SetTextureOffset("_MainTex", new Vector2(offset[1], 0));
        yield return new WaitForSeconds(0.3f);
        //normal
        material.SetTextureOffset("_MainTex", new Vector2(offset[0], 0));
    }

    IEnumerator DrinkAnimation(float duration)
    {
        //open
        material.SetTextureOffset("_MainTex", new Vector2(offset[1], 0));
        yield return new WaitForSeconds(duration/2);
        //close
        material.SetTextureOffset("_MainTex", new Vector2(offset[0], 0));
    }

}