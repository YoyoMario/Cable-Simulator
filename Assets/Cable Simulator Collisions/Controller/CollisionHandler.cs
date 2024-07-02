using System;
using UnityEngine;

[Serializable]
public class CollisionHandler
{
    public SphereCollider[] SphereColliders;
    public BoxCollider[] BoxColliders;

    private SphereColliderData[] _sphereColliderDatas;
    private BoxColliderData[] _boxColliderDatas;

    public CollisionHandler(SphereCollider[] sphereColliders, BoxCollider[] boxColliders)
    {
        SphereColliders = sphereColliders;
        BoxColliders = boxColliders;

        CreateSphereCollidersSnapshot();
        CreateBoxCollidersSnapshot();
    }

    public bool ResolveSphereCollisionForNode(CableNode cableNode, float cableThickness)
    {
        bool inCollision = default;
        foreach (SphereColliderData sphereColliderData in _sphereColliderDatas)
        {
            float distance = Vector3.Distance(cableNode.CurrentPosition, sphereColliderData.Position);
            float radius = sphereColliderData.Radius;
            if (distance > radius + cableThickness / 2)
            {
                // No collision;
                continue;
            }

            // Push point outside circle.
            Vector3 direction = (cableNode.CurrentPosition - sphereColliderData.Position);
            Vector3 positionOnSphere = sphereColliderData.Position + (direction.normalized * radius) + (direction.normalized * cableThickness / 2);
            cableNode.wantedPosition = cableNode.CurrentPosition;
            cableNode.OldPosition = positionOnSphere;
            cableNode.CurrentPosition = positionOnSphere;
            inCollision = true;
        }
        return inCollision;
    }
    public void ResolveBoxCollisionForNode(CableNode cableNode, float cableThickness)
    {
        foreach (BoxColliderData boxColliderData in _boxColliderDatas)
        {
            Vector3 localPositionToBoxCollider = boxColliderData.WorldToLocalMatrix.MultiplyPoint(cableNode.CurrentPosition);
            Vector3 halfColliderSize = boxColliderData.Size * 0.5f;
            halfColliderSize += Vector3.one * cableThickness / 2;

            Vector3 scalar = boxColliderData.Scale;
            float dx = localPositionToBoxCollider.x;
            float px = halfColliderSize.x - Mathf.Abs(dx);
            if (px <= 0)
            {
                continue;
            }

            float dy = localPositionToBoxCollider.y;
            float py = halfColliderSize.y - Mathf.Abs(dy);
            if (py <= 0)
            {
                continue;
            }

            float dz = localPositionToBoxCollider.z;
            float pz = halfColliderSize.z - Mathf.Abs(dz);
            if (pz <= 0)
            {
                continue;
            }

            // Push node out along closest edge.
            // Need to multiply distance by scale or we'll mess up on scaled box corners.
            if (px * scalar.x < py * scalar.y && px * scalar.x < pz * scalar.z)
            {
                float sx = Mathf.Sign(dx);
                localPositionToBoxCollider.x = halfColliderSize.x * sx;
            }
            else if (py * scalar.y < px * scalar.x && py * scalar.y < pz * scalar.z)
            {
                float sy = Mathf.Sign(dy);
                localPositionToBoxCollider.y = halfColliderSize.y * sy;
            }
            else
            {
                float sz = Mathf.Sign(dz);
                localPositionToBoxCollider.z = halfColliderSize.z * sz;
            }
            Vector3 globalPositionToBoxCollider = boxColliderData.LocalToWorldMatrix.MultiplyPoint(localPositionToBoxCollider);
            cableNode.wantedPosition = cableNode.CurrentPosition;
            cableNode.OldPosition = globalPositionToBoxCollider;
            cableNode.CurrentPosition = globalPositionToBoxCollider;
        }
    }

    //private void ResolveCollisionBetweenCables(CableNode[] cable1, CableNode[] cable2)
    //{
    //    for (int i = 0; i < cable1.Length; i++)
    //    {
    //        for (int j = i + 1; j < cable2.Length; j++)
    //        {
    //            Vector3 delta = cable2[j].CurrentPosition - cable1[i].CurrentPosition;
    //            float distance = Vector3.Distance(cable2[j].CurrentPosition, cable1[i].CurrentPosition);
    //            float minDistance = MinRadius * 2;

    //            if (distance < minDistance)
    //            {
    //                Vector3 direction = delta / distance;
    //                float correction = (minDistance - distance) / 2;
    //                cable1[i].CurrentPosition -= direction * correction;
    //                cable2[j].CurrentPosition += direction * correction;
    //            }
    //        }
    //    }
    //}

    private void CreateSphereCollidersSnapshot()
    {
        _sphereColliderDatas = new SphereColliderData[SphereColliders.Length];
        for (int i = 0; i < SphereColliders.Length; i++)
        {
            _sphereColliderDatas[i] = new SphereColliderData()
            {
                Position = SphereColliders[i].transform.position,
                Radius = SphereColliders[i].radius
            };
        }
    }
    private void CreateBoxCollidersSnapshot()
    {
        _boxColliderDatas = new BoxColliderData[BoxColliders.Length];
        for (int i = 0; i < BoxColliders.Length; ++i)
        {
            _boxColliderDatas[i] = new BoxColliderData()
            {
                Position = BoxColliders[i].transform.position,
                Rotation = BoxColliders[i].transform.rotation,
                Scale = BoxColliders[i].transform.localScale,
                LocalToWorldMatrix = BoxColliders[i].transform.localToWorldMatrix,
                WorldToLocalMatrix = BoxColliders[i].transform.worldToLocalMatrix,
                Size = BoxColliders[i].size
            };
        }
    }
}
