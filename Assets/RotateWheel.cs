using UnityEngine;

public class RotateWheel : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public float angularVel;
    // Update is called once per frame
    void FixedUpdate()
    {
        GetComponent<Rigidbody>().angularVelocity = Time.fixedDeltaTime * angularVel * Vector3.forward;
    }
}
