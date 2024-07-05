using DeltaReality.NucleusXR.CustomAttributes;
using UnityEngine;

public enum VirtualPointType
{
    DYNAMIC = 0,
    STATIC = 1
}

[RequireComponent(typeof(Rigidbody))]
public class CableVirtualPoint : MonoBehaviour
{
    [Header("Settings:")]
    public bool RegisterAtStart = default;
    public VirtualPointType VirtualPointType;
    public int NodeIndex = 59;
    [Space]
    [Header("Scene References:")]
    public CableSolver CableSolver;
    [Space]
    [Header("Runtime Info:")]
    public bool IsOutOfReach;
    public Vector3 TargetPosition;
    public Vector3 ActualPosition;
    public Vector3 Velocity;

    Rigidbody rb;
    private void Start()
    {
        if (RegisterAtStart)
        {
            Register();
        }
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = VirtualPointType == VirtualPointType.STATIC;
    }

    [Button]
    public void Register()
    {
        CableSolver.RegisterVirtualPoint(this);
    }

    [Button]
    public void Unregister()
    {
        CableSolver.UnregisterVirtualPoint(this);
    }

    private void FixedUpdate()
    {
        //if (this.VirtualPointType.Equals(VirtualPointType.STATIC))
        //{
        //    return;
        //}
        //rb.position = ActualPosition;
        //rb.linearVelocity -= Velocity;
    }
}
