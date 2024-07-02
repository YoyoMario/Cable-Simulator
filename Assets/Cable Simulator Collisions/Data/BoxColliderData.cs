using UnityEngine;

public struct BoxColliderData
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Matrix4x4 LocalToWorldMatrix;
    public Matrix4x4 WorldToLocalMatrix;
    public Vector3 Scale;
    public Vector3 Size;
}
