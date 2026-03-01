using UnityEngine;
using System;
using DistantLands.Cozy;

namespace MyGame.Wind
{
    [ExecuteAlways]
    public class WindManager : MonoBehaviour
    {
        [Header("Cozy Integration")]
        public bool useCozyWind = true;

        [Header("Manual Fallback")]
        public Vector2 windDirection = new Vector2(1, 0);
        public float windSize = 1.0f;
        public float windStrength = 1.0f;
        public float windSpeed = 1.0f;

        public static event Action<Vector2> OnWindDirectionChanged;

        private Vector2 lastWindDirection;
        private float lastWindSize, lastWindStrength, lastWindSpeed;

        void Update()
        {
            Vector2 dir; float size, strength, speed;

            if (useCozyWind && CozyWeather.instance?.windModule != null)
            {
                var module = CozyWeather.instance.windModule;
                dir = module.WindDirection;
                strength = module.windAmount;  // Maps to wind strength
                speed = module.windSpeed;      // Maps to wind speed/change rate
                size = 1.0f;                   // Derive from Cozy if exposed, or fixed/manual
            }
            else
            {
                dir = windDirection;
                size = windSize;
                strength = windStrength;
                speed = windSpeed;
            }

            // Apply only if changed (your existing logic)
            if (dir != lastWindDirection || size != lastWindSize ||
                strength != lastWindStrength || speed != lastWindSpeed)
            {
                Shader.SetGlobalVector("_WindDirection", new Vector4(dir.x, dir.y, 0, 0));
                Shader.SetGlobalFloat("_WindSize", size);
                Shader.SetGlobalFloat("_WindStrength", strength);
                Shader.SetGlobalFloat("_WindSpeed", speed);

                if (dir != lastWindDirection)
                    OnWindDirectionChanged?.Invoke(dir);

                lastWindDirection = dir;
                lastWindSize = size;
                lastWindStrength = strength;
                lastWindSpeed = speed;
            }
        }

        void OnValidate()
        {
            // Reapply immediately for editor tweaks (fallback only)
            if (!useCozyWind)
            {
                Shader.SetGlobalVector("_WindDirection", new Vector4(windDirection.x, windDirection.y, 0, 0));
                Shader.SetGlobalFloat("_WindSize", windSize);
                Shader.SetGlobalFloat("_WindStrength", windStrength);
                Shader.SetGlobalFloat("_WindSpeed", windSpeed);
                OnWindDirectionChanged?.Invoke(windDirection);
            }
        }
    }
}
