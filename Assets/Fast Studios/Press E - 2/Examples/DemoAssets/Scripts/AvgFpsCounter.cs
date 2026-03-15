using UnityEngine;
using TMPro;

namespace FastStudios.Demo
{
    public class AverageFPSCounter : MonoBehaviour
    {
        public TextMeshProUGUI fpsText;

        private float avgDeltaTime = 0f;
        private const float smoothing = .0001f;

        void Update()
        {
            float currentDeltaTime = Time.deltaTime / Time.timeScale;

            avgDeltaTime += (currentDeltaTime - avgDeltaTime) * smoothing;

            float displayFPS = 1F / avgDeltaTime;

            fpsText.text = string.Format("AVG: {0:0} FPS", displayFPS);
        }
    }
}
