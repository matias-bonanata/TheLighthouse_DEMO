using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SpyglassCamera : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera mainVcam;
    [SerializeField] private CinemachineCamera spyglassVcam;

    [Header("UI")]
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private GameObject spyglassUIContainer;

    [Header("Spyglass Settings")]
    [SerializeField] private float normalFOV = 73f;
    [SerializeField] private float zoomedFOV = 18f;
    [SerializeField] private float fovLerpSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private int activePriority = 10;
    [SerializeField] private int inactivePriority = 0;

    [Header("Rotation Limits")]
    [SerializeField] private float minXRotation = -90f;
    [SerializeField] private float maxXRotation = 90f;
    [SerializeField] private float minYRotation = -60f;
    [SerializeField] private float maxYRotation = 60f;

    [Header("Post Processing")]
    [SerializeField] private Volume postProcessVolume;

    // Store original values
    private bool originalLensDistortionOverride;
    private float originalLensDistortionValue;
    private bool originalChromaticAberrationOverride;
    private float originalChromaticAberrationValue;
    private bool originalVignetteIntensityOverride, originalVignetteSmoothnessOverride, originalVignetteRoundedOverride;
    private float originalVignetteIntensityValue, originalVignetteSmoothnessValue;
    private bool originalVignetteRoundedValue;

    private bool IsSpyglassActive;
    private float xRotation = 0f;
    private float yRotation = 0f;
    private float targetFOV;

    private void Start()
    {
        if (zoomInButton) zoomInButton.onClick.AddListener(ZoomIn);
        if (zoomOutButton) zoomOutButton.onClick.AddListener(ZoomOut);

        spyglassUIContainer.SetActive(false);
        targetFOV = normalFOV;

        mainVcam.Priority = activePriority;
        spyglassVcam.Priority = inactivePriority;

        CacheOriginalPostProcessingValues();
    }

    private void CacheOriginalPostProcessingValues()
    {
        if (postProcessVolume?.profile == null) return;

        // FIXED: Explicit generic type syntax
        if (postProcessVolume.profile.TryGet<LensDistortion>(out var lensDistortion))
        {
            originalLensDistortionOverride = lensDistortion.intensity.overrideState;
            originalLensDistortionValue = lensDistortion.intensity.value;
        }

        if (postProcessVolume.profile.TryGet<ChromaticAberration>(out var chromaticAberration))
        {
            originalChromaticAberrationOverride = chromaticAberration.intensity.overrideState;
            originalChromaticAberrationValue = chromaticAberration.intensity.value;
        }

        if (postProcessVolume.profile.TryGet<Vignette>(out var vignette))
        {
            originalVignetteIntensityOverride = vignette.intensity.overrideState;
            originalVignetteIntensityValue = vignette.intensity.value;
            originalVignetteSmoothnessOverride = vignette.smoothness.overrideState;
            originalVignetteSmoothnessValue = vignette.smoothness.value;
            originalVignetteRoundedOverride = vignette.rounded.overrideState;
            originalVignetteRoundedValue = vignette.rounded.value;
        }
    }

    public void ToggleSpyglass()
    {
        IsSpyglassActive = !IsSpyglassActive;

        if (IsSpyglassActive)
        {
            ApplySpyglassPostProcessing();
            spyglassVcam.Priority = activePriority;
            mainVcam.Priority = inactivePriority;
            spyglassUIContainer.SetActive(true);
            targetFOV = normalFOV;
        }
        else
        {
            RestoreOriginalPostProcessing();
            mainVcam.Priority = activePriority;
            spyglassVcam.Priority = inactivePriority;
            spyglassUIContainer.SetActive(false);
            xRotation = 0f;
            yRotation = 0f;
        }
    }

    private void ApplySpyglassPostProcessing()
    {
        if (postProcessVolume?.profile == null) return;

        if (postProcessVolume.profile.TryGet<LensDistortion>(out var lens))
        {
            lens.intensity.overrideState = true;
            lens.intensity.value = 0.66f;
        }

        if (postProcessVolume.profile.TryGet<ChromaticAberration>(out var chroma))
        {
            chroma.intensity.overrideState = true;
            chroma.intensity.value = 0.7f;
        }

        if (postProcessVolume.profile.TryGet<Vignette>(out var vignette))
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.62f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.316f;
            vignette.rounded.overrideState = true;
            vignette.rounded.value = true;
        }
    }

    private void RestoreOriginalPostProcessing()
    {
        if (postProcessVolume?.profile == null) return;

        if (postProcessVolume.profile.TryGet<LensDistortion>(out var lens))
        {
            lens.intensity.overrideState = originalLensDistortionOverride;
            lens.intensity.value = originalLensDistortionValue;
        }

        if (postProcessVolume.profile.TryGet<ChromaticAberration>(out var chroma))
        {
            chroma.intensity.overrideState = originalChromaticAberrationOverride;
            chroma.intensity.value = originalChromaticAberrationValue;
        }

        if (postProcessVolume.profile.TryGet<Vignette>(out var vignette))
        {
            vignette.intensity.overrideState = originalVignetteIntensityOverride;
            vignette.intensity.value = originalVignetteIntensityValue;
            vignette.smoothness.overrideState = originalVignetteSmoothnessOverride;
            vignette.smoothness.value = originalVignetteSmoothnessValue;
            vignette.rounded.overrideState = originalVignetteRoundedOverride;
            vignette.rounded.value = originalVignetteRoundedValue;
        }
    }

    private void Update()
    {
        HandleFOV();
        HandleSpyglassInput();
    }

    private void HandleSpyglassInput()
    {
        if (!IsSpyglassActive) return;

        if (Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, minXRotation, maxXRotation);

            yRotation += mouseX;
            yRotation = Mathf.Clamp(yRotation, minYRotation, maxYRotation);

            transform.localEulerAngles = new Vector3(xRotation, yRotation, 0);
        }
    }

    private void ZoomIn()
    {
        if (!IsSpyglassActive) return;
        targetFOV = zoomedFOV;
    }

    private void ZoomOut()
    {
        if (!IsSpyglassActive) return;
        targetFOV = normalFOV;
    }

    private void HandleFOV()
    {
        if (!IsSpyglassActive) return;
        float currentFOV = spyglassVcam.Lens.FieldOfView;
        float newFOV = Mathf.Lerp(currentFOV, targetFOV, fovLerpSpeed * Time.deltaTime);
        spyglassVcam.Lens.FieldOfView = newFOV;
    }
}
