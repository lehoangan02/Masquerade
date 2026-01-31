using UnityEngine;

public class FollowMainCamera : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Follow the main camera's position
        transform.position = Camera.main.transform.position;
    }
}
