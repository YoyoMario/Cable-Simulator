using UnityEngine;

public class Test : MonoBehaviour
{
    public Vector3 CurrentPosition = default;
    public Vector3 Velocity = default;
    private void OnEnable()
    {
        CurrentPosition = transform.position;
        ThreadManager.Updated += OnUpdated;
    }

    private void OnDisable()
    {
        ThreadManager.Updated -= OnUpdated;
    }

    public float UpdateTime;
    public float ThreadTime;
    public float FixedTime;
    private void Update()
    {
        //transform.position = CurrentPosition;
        UpdateTime += Time.deltaTime;
    }

    private void FixedUpdate()
    {
        FixedTime += Time.fixedDeltaTime;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(CurrentPosition, 0.5f);
    }

    public float Gravity = 9.81f;

    private void OnUpdated(double deltaTime)
    {
        ThreadTime += (float)deltaTime;
        Vector3 gravity = Vector3.down * Gravity;
        Velocity += gravity * (float)deltaTime;
        CurrentPosition += Velocity * (float)deltaTime;
    }
}
