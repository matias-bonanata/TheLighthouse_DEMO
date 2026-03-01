using DistantLands.Cozy;
using UnityEngine;

public class CozyGrassWindLink : MonoBehaviour
{
    [Header("Wind Remapping")]
    [SerializeField] public float windSpeedMultiplier = 1f;
    [SerializeField] public float windScaleMultiplier = 1f;

    CozyWeather weather;

    void Awake()
    {
        weather = CozyWeather.instance;
    }

    void Update()
    {
        if (weather == null) return;

        // Push Cozy values to ALL grass globally
        Shader.SetGlobalFloat("_WindSpeed", weather.windModule.windAmount * windSpeedMultiplier);
        Shader.SetGlobalFloat("_WindScale", weather.windModule.windSpeed * windScaleMultiplier);
    }
}
