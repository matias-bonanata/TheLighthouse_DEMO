using UnityEngine;

public class GetPlayerPosition : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    public void Update()
    {
        Vector3 position = transform.position;
        Shader.SetGlobalVector("_Player", transform.position); //FOR GRASS SHADER

    }
}
