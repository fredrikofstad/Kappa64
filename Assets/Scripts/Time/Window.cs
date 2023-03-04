using UnityEngine;

public class Window : MonoBehaviour
{
    [SerializeField] TimeManager time;
    [SerializeField] Material material;

    float blend;

    // can refactor into timemanager
    void Update()
    {
        SetBlend(4, 6, "_Blend", true);
        SetBlend(19, 21, "_Blend");
    }

    void SetBlend(int from, int to, string variable, bool reverse = false)
    {
        if (time.GetHours() > from && time.GetHours() < to)
        {
            blend = reverse ? 1 - ((time.GetHours() - from) / (to - from)) :
                                   (time.GetHours() - from) / (to - from);
            material.SetFloat(variable, blend);
        }
    }
}
