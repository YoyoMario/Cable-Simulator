using System;
using UnityEngine;

public class CollisionTunneling : MonoBehaviour
{
    public BoxCollider BoxCollider = default;
    public Vector3 Position = default;
    public Vector3 OldPosition = default;
    public float RopeThickness = 0.05f;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(Position, RopeThickness);

        Vector3 dir = transform.position - BoxCollider.transform.position;

        //Gizmos.DrawRay(BoxCollider.transform.position, dir.normalized * finalValue);
    }

    private void Update()
    {
        OldPosition = Position;
        Position = transform.position;
        ResolveBoxCollider();
    }

    private void ResolveBoxCollider()
    {
        Vector2 localPositionToBoxCollider = BoxCollider.transform.worldToLocalMatrix.MultiplyPoint(Position);
        Vector2 localScale = BoxCollider.transform.localScale;

        bool overlappingX = IsOverlapping(localScale.x / 2f, localPositionToBoxCollider.x);
        bool overlappingY = IsOverlapping(localScale.y / 2f, localPositionToBoxCollider.y);

        if (!overlappingX || !overlappingY)
        {
            // If one of them isn't overlapping, means we're not inside the collision.
            return;
        }

        float overlappingDistanceX = OverlappingDistance(localScale.x / 2f, localPositionToBoxCollider.x);
        float overlappingDistanceY = OverlappingDistance(localScale.y / 2f, localPositionToBoxCollider.y);

        float distance = default;
        if (overlappingDistanceX < overlappingDistanceY)
        {
            distance = DistanceFromCenterToEdgeThroughPoint(localPositionToBoxCollider.x, localPositionToBoxCollider.y, localScale.x);
        }
        else
        {
            distance = DistanceFromCenterToEdgeThroughPoint(localPositionToBoxCollider.y, localPositionToBoxCollider.x, localScale.y);
        }

        Vector3 directionTowardPoint = transform.position - BoxCollider.transform.position;
        directionTowardPoint = directionTowardPoint.normalized * distance;
        //if (overlappingDistanceX < overlappingDistanceY)
        //{
        //    localPositionToBoxCollider.x = AdjustCollision(localScale.x / 2f, localPositionToBoxCollider.x);
        //}
        //else
        //{
        //    localPositionToBoxCollider.y = AdjustCollision(localScale.y / 2f, localPositionToBoxCollider.y);
        //}

        Vector2 globalPositionToBoxCollider = BoxCollider.transform.localToWorldMatrix.MultiplyPoint(directionTowardPoint);
        Position = globalPositionToBoxCollider;
    }



    private float DistanceFromCenterToEdgeThroughPoint(float localPointX, float localPointY, float localScale)
    {
        Vector2 localPoint = new Vector2(localPointX, localPointY);
        double angle = Math.Atan(Math.Abs(localPoint.y / localPoint.x)) * Mathf.Rad2Deg;
        float leftoverDistanceFromPointToEdge = localScale / 2 - Mathf.Abs(localPoint.x);
        float hipotenisus = leftoverDistanceFromPointToEdge / Mathf.Cos((float)angle * Mathf.Deg2Rad);
        return Mathf.Sqrt(Mathf.Pow(localPoint.x, 2) + Mathf.Pow(localPoint.y, 2)) + hipotenisus;
    }

    private bool IsOverlapping(float halfDimension, float axisPosition)
    {
        return (halfDimension - Mathf.Abs(axisPosition)) > 0;
    }

    private float OverlappingDistance(float halfDimension, float axisPosition)
    {
        return (halfDimension - Mathf.Abs(axisPosition));
    }

    private float AdjustCollision(float halfDimension, float axisPosition)
    {
        float signedValue = Mathf.Sign(axisPosition);
        axisPosition = halfDimension * signedValue;
        return axisPosition;
    }
}
