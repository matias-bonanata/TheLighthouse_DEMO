using UnityEngine;
using UnityEngine.XR;

public class Clock : MonoBehaviour
{
    [SerializeField] private Transform smallHand;
    [SerializeField] private Transform bigHand;
    public LightingManager LightingManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject lightingScript = GameObject.Find("Time & Lighting Manager");
        LightingManager = lightingScript.GetComponent<LightingManager>();
    }

    // Update is called once per frame
    void Update()
    {
        // Ensure bigHand is assigned
        if (smallHand == null) return;
        if (bigHand == null) return;

        // Map hours (0-24) so X rotation does a full 360 over each 12-hour span
        float smallHandRotation = ((LightingManager.TimeOfDay / 2) / 12f) * -360f;
        float bigHandRotation = ((LightingManager.TimeOfDay * 12) / 12f) * -360f;

        // Only modify X axis, keep y/z as designed
        //Vector3 smallOriginalEuler = smallHand.localEulerAngles;
        //Vector3 bigOriginalEuler = bighand.localEulerAngles;

        smallHand.localEulerAngles = new Vector3(smallHandRotation, 90f, -90f);
        bigHand.localEulerAngles = new Vector3(bigHandRotation, 90f, -90f);
    }
}
