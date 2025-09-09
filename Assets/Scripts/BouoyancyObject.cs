using UnityEngine;


[RequireComponent (typeof(Rigidbody))]
public class BuoyancyObject : MonoBehaviour
{
    public Transform[] floaters;
    
    public float underWaterDrag = 3f;
    public float underWaterAngularDrag = 1f;
    public float floatingPower = 15f;
    public float waterHeight = 0f;

    public float airDrag = 0f;
    public float airAngularDrag = 0.05f;

    public bool underwater;

    private Rigidbody m_Rigidbody;
    int floatersUnderwater;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        floatersUnderwater = 0;
        for (int i = 0; i < floaters.Length; i++)
        {
            float difference = floaters[i].position.y - waterHeight;

            if (difference < 0)
            {
                m_Rigidbody.AddForceAtPosition(Vector3.up * floatingPower * Mathf.Abs(difference),
                    floaters[i].position, ForceMode.Force);
                floatersUnderwater += 1;
                if (!underwater)
                {
                    underwater = true;
                }
            }
        }
        
        if (underwater && floatersUnderwater == 0)
        {
            underwater = false;
            SwitchState(false);
        }
    }

    void SwitchState (bool isUnderwater)
    {
        if (isUnderwater)
        {
            m_Rigidbody.linearDamping = underWaterDrag;
            m_Rigidbody.angularDamping = underWaterAngularDrag;
        }
        else
        {
            m_Rigidbody.linearDamping = airDrag;
            m_Rigidbody.angularDamping = airAngularDrag;
        }
    }
}
