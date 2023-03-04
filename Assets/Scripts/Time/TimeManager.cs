using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] MeshRenderer skyDome;
    [SerializeField, Range(0, 1)] float blend;

    [SerializeField, Range(0, 100)] int factor = 2;

    [SerializeField] Color dayHue = Color.white;
    [SerializeField] Color nightHue;
    [SerializeField] Color dawnHue;
    Color colorTint;



    Material skyMaterial;

    public float minutes;
    public float hours;

    bool timeIsPaused = false;

    void Start()
    {
        minutes = 500;
        skyMaterial = skyDome.material;
        Shader.SetGlobalColor("_Sun", dayHue);
    }

    void Update()
    {
        UpdateTime();
        UpdateMaterials();
    }

    void UpdateMaterials()
    {
        SetBlend(4, 6, "_Blend", true);
        SetBlend(4, 6, "_Blend2", true);
        SetHue(4, 6, nightHue, dayHue);
        SetBlend(17, 19, "_Blend2");
        SetHue(17, 19, dayHue, dawnHue);
        SetBlend(19, 21, "_Blend");
        SetHue(19, 21, dawnHue, nightHue);
    }

    // Procedure that sets the blend ratio in sky material.
    // @Param - time in hours, string to change, optional reverse bool
    void SetBlend(int from, int to, string variable, bool reverse = false)
    {
        if (GetHours() > from && GetHours() < to)
        {
            blend = reverse ? 1 - ((hours - from) / (to - from)) :
                                   (hours - from) / (to - from);
            skyMaterial.SetFloat(variable, blend);
        }
    }

    void SetHue(int from, int to, Color fromColor, Color toColor)
    {
        if (GetHours() > from && GetHours() < to)
        {
            colorTint = Color.Lerp(fromColor, toColor, (hours - from) / (to - from));

            Shader.SetGlobalColor("_Sun", colorTint);
        }
    }


    public float GetHours() => minutes / 60;
    public float GetMinutes() => minutes;

    void UpdateTime()
    {
        if (timeIsPaused) return;

        minutes += Time.deltaTime * factor;
        hours = minutes / 60;

        if (minutes > 1440) minutes = 1;
    }
}
