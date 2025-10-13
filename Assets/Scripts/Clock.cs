using UnityEngine;
using UnityEngine.XR;
using TMPro;

public class Clock : MonoBehaviour
{
    //Hand Move Logic
    [Header("Transform Hands with Time")]
    [SerializeField] private Transform smallHand;
    [SerializeField] private Transform bigHand;
    public LightingManager LightingManager;

    //Sleep Clock
    private float oldDistanceInFront;
    private float oldTimeSpeed;
    private float newTime;
    private float addingTime = 0f;
    private float smallHandRotation;
    private float bigHandRotation;
    private bool sleepSelectScreen = false;

    [Header("Sleep Logic")]
    [SerializeField] private float xRotationAdd = -20f; //Rotation to Add when sleep (visual only)
    [SerializeField] private TextMeshProUGUI addingTimeText; //Finding Text
    [SerializeField] private GameObject UIContainer; //Finding UI Container
    public GoToCamera goToCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject lightingScript = GameObject.Find("Time & Lighting Manager"); //force find and attach
        LightingManager = lightingScript.GetComponent<LightingManager>();
        
        oldDistanceInFront = goToCamera.distanceInFront; //save initial distance in front
        oldTimeSpeed = LightingManager.daySpeed;

        if (goToCamera != null) goToCamera = GetComponent<GoToCamera>(); //Get camera

    }

    // Update is called once per frame
    void Update()
    {
        // Ensure bigHand is assigned
        if (smallHand == null) return;
        if (bigHand == null) return;

        //rotation based on floats
        smallHand.localEulerAngles = new Vector3(smallHandRotation, 90f, -90f);
        bigHand.localEulerAngles = new Vector3(bigHandRotation, 90f, -90f);

        //
        //TOGGLE SLEEP
        if (Input.GetKeyDown(KeyCode.T))
        {
            sleepSelectScreen = !sleepSelectScreen;
        }

        //If NOT SLEEP
        if (!sleepSelectScreen)
        {
            //Unfreeze Time Passing
            LightingManager.daySpeed = oldTimeSpeed;
            newTime = 0f;
            addingTime = 0f; //RESET

            // Clock rotation to 24hrs
            smallHandRotation = ((LightingManager.TimeOfDay / 2) / 12f) * -360f;
            bigHandRotation = smallHandRotation * 12; //moves 12 times slower than small  hand

            UIContainer.SetActive(false); //Hide UI
            goToCamera.distanceInFront = oldDistanceInFront; //Go Back to Old distance front
        }

        //IF SLEEP
        if (sleepSelectScreen)
        {
            UIContainer.SetActive(true);
            goToCamera.distanceInFront = 1.45f;
            LightingManager.daySpeed = 0f;
            newTime = LightingManager.TimeOfDay + addingTime;
            //Debug.Log("New Time:" + newTime + "Adding Time:" + addingTime);

            if (Input.GetKeyDown(KeyCode.D) && addingTime < 12f)
            {
                //Visual clock moving
                smallHandRotation -= xRotationAdd;
                bigHandRotation -= xRotationAdd * 12f;

                //ADD TIME
                addingTime += 1f;
                UpdateAddingTimeText();
            }
            if (Input.GetKeyDown(KeyCode.A) && addingTime > 0f)
            {
                //Visual clock moving
                smallHandRotation += xRotationAdd;
                bigHandRotation += xRotationAdd * 12f;

                //SUBSTRACT TIME
                addingTime -= 1f;
                UpdateAddingTimeText();
            }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                    LightingManager.TimeOfDay = newTime;
                    newTime = 0f;
                    addingTime = 0f; //RESET
                    UpdateAddingTimeText();

                    sleepSelectScreen = false; //Go BACK
            }
        }
    }

    private void UpdateAddingTimeText()
    {
        if(addingTimeText != null) addingTimeText.text = addingTime.ToString("F0") + " Hours"; // Format to 1 decimal place
    }
}
