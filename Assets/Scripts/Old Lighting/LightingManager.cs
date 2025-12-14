using UnityEngine;
using System.Collections;
using UnityEngine.SocialPlatforms;
// UnityEngine.Experimental.GlobalIllumination;

[ExecuteAlways] //execute methods in class whilst in unity editor
public class LightingManager : MonoBehaviour
{
    //References
    [Header("References")]
    [SerializeField] private Light DirectionalLight;
    [SerializeField] private Light MoonDirectionalLight;

    //time
    [Header("Time")]
    [SerializeField] public float daySpeed = 1f;
    [SerializeField] float startingPositionModifier = 0f;
    [SerializeField, Range(0,24)] public float TimeOfDay; //I made this public
    [SerializeField] private float dayBreak = 11.25f;
    //[SerializeField] private float nightBreak = 0.8f;

    //fog
    [Header("Fog Color")]
    [SerializeField] public Color dayFogColor = new Color(1f, 1f, 1f); // White, #FFFFFF
    //[SerializeField] public Color nightFogColor = new Color(0.321f, 0.467f, 0.718f); // #5277B7 
    //[SerializeField] private float fogBlendAmount = 0f;
    //[SerializeField] private float fogTransitionAmount = 0.5f;

    //moon intensity
    //[Header("Moon Intensity")]
    //[SerializeField] private float maxIntensity = 0.6f;
    //[SerializeField] private float timeForMoonToShine = 4f;
    //[SerializeField] private float targetIntensity = 0f;
    //[SerializeField] private float smoothTime = 3f;

    [Header("Light Change Parameters")]
    [SerializeField] private float ambientLightNight = 0.6f;
    [SerializeField] private float sunLightNight = 0.3f;
    [SerializeField] private float fogNight = 0.025f; 
    [SerializeField] private float ambientLightDay = 2.8f;
    [SerializeField] private float sunLightDay = 2f;
    [SerializeField] private float fogDay = 0.005f;

    //sunset light change
    [Header("Sun Colors")]
    [SerializeField] private float sunChangeDuration = 10f; // Duration to complete the full transition
    [SerializeField] private Color[] colors = { new Color(1f, 0.305f, 0f), new Color(1f, 0.8f, 0f), Color.white };

    private void Start()
    {

    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            TimeOfDay += Time.deltaTime * daySpeed;
            TimeOfDay %= 24; //clamp between 0-24
            UpdateLighting(TimeOfDay / 24f);

            if (TimeOfDay > 0.6f && TimeOfDay < 12)
            {
                RenderSettings.ambientIntensity = ambientLightNight;
                DirectionalLight.intensity = sunLightNight;
                RenderSettings.fogDensity = fogNight;
            }
            else
            {
                RenderSettings.ambientIntensity = ambientLightDay;
                DirectionalLight.intensity = sunLightDay;
                RenderSettings.fogDensity = fogDay;
            }
            //change sunlight to sunrise
            if (TimeOfDay > dayBreak && TimeOfDay < (dayBreak+1.2f))
            {
                StartCoroutine(BeginSunriseColor(sunChangeDuration));
            }

            //when night ensure sunlight starts red
            if (TimeOfDay == 0.8f) DirectionalLight.color = colors[2];
        }
    }

    private IEnumerator BeginSunriseColor(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration; // goes from 0 to 1 over 'duration'
            if (t < 0.5f)
            {
                // lerp from colors[0] to colors[1]
                DirectionalLight.color = Color.Lerp(colors[0], colors[1], t * 2);
            }
            else
            {
                // lerp from colors[1] to colors[2]
                DirectionalLight.color = Color.Lerp(colors[1], colors[2], (t - 0.5f) * 2);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Hold color at colors[2] after finish
        DirectionalLight.color = colors[2];
    }


    private void UpdateLighting(float timePercent)
    {
        if(DirectionalLight != null)
        {
            DirectionalLight.transform.localRotation = Quaternion.Euler(
                new Vector3((timePercent * -360f) - startingPositionModifier, 170f, 0));
        }
    }

    // Skybox logic for four time blocks
    //private void UpdateSkybox()
    //{
    //    int index;
    //    if (TimeOfDay < nightEnd) index = 0; // Night
    //    else if (TimeOfDay < sunriseEnd) index = 1; // Sunrise
    //    else if (TimeOfDay < dayEnd) index = 2; // Day
    //    else if (TimeOfDay < sunsetEnd) index = 3; // Sunset
    //    else index = 4; // Night again

    //    if (Skyboxes != null && Skyboxes.Length == 5 && Skyboxes[index] != null)
    //        RenderSettings.skybox = Skyboxes[index];
    //}


    private void OnValidate()
    {
        if (DirectionalLight != null)
            return;
    }


}
