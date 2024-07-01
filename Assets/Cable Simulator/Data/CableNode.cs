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
    public float Distance;
    public float PushPullStrength;
    public Vector3 wantedPosition;

    public float NodeDistance;

    public CableNode(Vector3 startPosition, float nodeDistance)
    {
        CurrentPosition = startPosition;
        OldPosition = CurrentPosition;
        PredictedPosition = CurrentPosition;
        Acceleration = Vector3.zero;
        Velocity = Vector3.zero;
        NodeDistance = nodeDistance;
    }
}
