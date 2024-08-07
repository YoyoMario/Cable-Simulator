using System;
using UnityEngine;

[Serializable]
public class CableNode
{
    public Vector3 OldPosition;
    public Vector3 CurrentPosition;
    public Vector3 PredictedPosition;
    public Vector3 Acceleration;
    public Vector3 Velocity;
    public float Mass = 0.5f; // in kg
    public Vector3 PositionBeforeAdjustment;

    [Header("Runtime Info:")]
    public float Distance;
    public float PushPullStrength;

    public CableNode(Vector3 startPosition, float mass)
    {
        CurrentPosition = startPosition;
        OldPosition = CurrentPosition;
        PredictedPosition = CurrentPosition;
        Acceleration = Vector3.zero;
        Velocity = Vector3.zero;
        Mass = mass;
    }
}
