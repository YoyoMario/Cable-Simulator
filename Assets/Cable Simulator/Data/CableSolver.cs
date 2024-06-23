using UnityEngine;

public class CableSolver : MonoBehaviour
{
    [Header("Settings:")]
    public int SolverCount = 10;
    public int NodeCount = 40;
    public float RopeThickness = 0.05f;
    [Range(0, 0.2f)] public float NodeDistance = 0.1f;
    public float Gravity = 9.81f;
    [Range(0.01f, .9f)] public float Elasticity = 1;
    [Range(0.9f, 1.0f)] public float GravityDampening = 1;

    public bool EndHandle = default;
    public Transform EndHanlePoint = default;

    [Header("Runtime Info:")]
    [SerializeField] private CableNode[] _cableNodes = default;
    [SerializeField] private SphereCollider _sphereCollider = default;
    [SerializeField] private BoxCollider _boxCollider = default;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        for (int i = 0; i < NodeCount - 1; i++)
        {
            if (i % 2 == 0)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.white;
            }
            Gizmos.DrawLine(_cableNodes[i].CurrentPosition, _cableNodes[i + 1].CurrentPosition);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_cableNodes[i].CurrentPosition, 0.025f);
        }
        Gizmos.DrawSphere(_cableNodes[NodeCount - 1].CurrentPosition, 0.025f);
    }

    private void Start()
    {
        InitializeCable();
    }

    private void Update()
    {
        // Calculate gravity velocity.
        for (int i = 0; i < NodeCount; i++)
        {
            CableNode cableNode = _cableNodes[i];
            Vector3 force = Vector3.down * Gravity * cableNode.Mass;
            cableNode.Acceleration = force / cableNode.Mass;
            cableNode.Velocity = cableNode.CurrentPosition - cableNode.OldPosition;
        }

        // Gravity pass + main rope pass.
        for (int i = 0; i < NodeCount; i++)
        {
            CableNode cableNode = _cableNodes[i];
            Vector3 newPosition = cableNode.CurrentPosition + cableNode.Velocity * GravityDampening + (cableNode.Acceleration * Time.deltaTime * Time.deltaTime * cableNode.Mass);
            cableNode.OldPosition = cableNode.CurrentPosition;
            cableNode.CurrentPosition = newPosition;

            // Collision check.
            ResolveSphereCollisionForSingleNode(cableNode);
            ResolveBoxCollisionForSingleNode(cableNode);
        }

        //// Check for collisions.
        //for (int i = 0; i < NodeCount; i++)
        //{
        //    CableNode cableNode = _cableNodes[i];
        //    float distance = Vector3.Distance(cableNode.CurrentPosition, _sphereCollider.transform.position);
        //    float radius = _sphereCollider.radius;
        //    if (distance > radius)
        //    {
        //        // No collision;
        //        continue;
        //    }

        //    // Push point outside circle.
        //    Vector3 direction = (cableNode.CurrentPosition - _sphereCollider.transform.position);
        //    Vector3 positionOnSphere = _sphereCollider.transform.position + (direction.normalized * radius);
        //    cableNode.CurrentPosition = positionOnSphere;
        //    cableNode.Acceleration = Vector3.zero;
        //    cableNode.Velocity = Vector3.zero;
        //}

        // Solver.
        for (int z = 0; z < SolverCount; z++)
        {
            ResolveNodePositions();
        }
    }

    private void InitializeCable()
    {
        _cableNodes = new CableNode[NodeCount];
        for (int i = 0; i < NodeCount; i++)
        {
            Vector3 nodeStartPosition = transform.position + (Vector3.down * NodeDistance * i);
            _cableNodes[i] = new CableNode(nodeStartPosition);
        }
    }

    private void ResolveNodePositions()
    {
        for (int i = 0; i < NodeCount - 1; i++)
        {
            CableNode node1 = _cableNodes[i];
            CableNode node2 = _cableNodes[i + 1];

            // Special case.
            if (i == 0)
            {
                node1.CurrentPosition = transform.position;
            }
            if (EndHandle && i == NodeCount - 2)
            {
                node2.CurrentPosition = EndHanlePoint.position;
            }


            Vector3 direction = node1.CurrentPosition - node2.CurrentPosition; // Towards Node 1.
            float distance = Vector3.Distance(node1.CurrentPosition, node2.CurrentPosition);

            float pushPullStrength = default;
            if (distance > 0)
            {
                pushPullStrength = (NodeDistance - distance) / distance;
            }

            Vector3 neededTranslation = direction * Elasticity * pushPullStrength;
            node1.CurrentPosition += neededTranslation;
            node2.CurrentPosition -= neededTranslation;

            if (i == 0)
            {
                node1.CurrentPosition = transform.position;
            }
            if (EndHandle && i == NodeCount - 2)
            {
                node2.CurrentPosition = EndHanlePoint.position;
            }

            // Collision handling.
            ResolveSphereCollisionForSingleNode(node1);
            ResolveBoxCollisionForSingleNode(node1);
        }
    }

    private void ResolveSphereCollisionForSingleNode(CableNode cableNode)
    {
        float distance = Vector3.Distance(cableNode.CurrentPosition, _sphereCollider.transform.position);
        float radius = _sphereCollider.radius;
        if (distance > radius + RopeThickness / 2)
        {
            // No collision;
            return;
        }

        // Push point outside circle.
        Vector3 direction = (cableNode.CurrentPosition - _sphereCollider.transform.position);
        Vector3 positionOnSphere = _sphereCollider.transform.position + (direction.normalized * radius) + (direction.normalized * RopeThickness / 2);
        cableNode.CurrentPosition = positionOnSphere;
        cableNode.Acceleration = Vector3.zero;
        cableNode.Velocity = Vector3.zero;
    }

    private void ResolveBoxCollisionForSingleNode(CableNode cableNode)
    {
        Vector3 localPositionToBoxCollider = _boxCollider.transform.worldToLocalMatrix.MultiplyPoint(cableNode.CurrentPosition);
        Vector3 halfColliderSize = _boxCollider.size * 0.5f;
        halfColliderSize += Vector3.one * RopeThickness / 2;

        Vector3 scalar = _boxCollider.transform.localScale;
        float dx = localPositionToBoxCollider.x;
        float px = halfColliderSize.x - Mathf.Abs(dx);
        if (px <= 0)
        {
            return;
        }

        float dy = localPositionToBoxCollider.y;
        float py = halfColliderSize.y - Mathf.Abs(dy);
        if (py <= 0)
        {
            return;
        }

        float dz = localPositionToBoxCollider.z;
        float pz = halfColliderSize.z - Mathf.Abs(dz);
        if (pz <= 0)
        {
            return;
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

        //// Test x.
        //float distanceX = localPositionToBoxCollider.x;
        //float pointX = halfColliderSize.x - Mathf.Abs(distanceX);
        //if (pointX >= 0)
        //{
        //    float sx = Mathf.Sign(distanceX);
        //    localPositionToBoxCollider.x = halfColliderSize.x * sx;
        //}

        //// Test y.
        //float distanceY = localPositionToBoxCollider.y;
        //float pointY = halfColliderSize.y - Mathf.Abs(distanceY);
        //if (pointY >= 0)
        //{
        //    float sy = Mathf.Sign(distanceY);
        //    localPositionToBoxCollider.y = halfColliderSize.y * sy;
        //}

        //// Test z.
        //float distanceZ = localPositionToBoxCollider.z;
        //float pointZ = halfColliderSize.z - Mathf.Abs(distanceZ);
        //if (pointZ >= distanceZ)
        //{
        //    localPositionToBoxCollider.z = pointZ;
        //}

        Vector3 globalPositionToBoxCollider = _boxCollider.transform.localToWorldMatrix.MultiplyPoint(localPositionToBoxCollider);
        cableNode.CurrentPosition = globalPositionToBoxCollider;
    }
}
