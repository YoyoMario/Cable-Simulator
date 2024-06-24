using System.Threading;
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
    [SerializeField] private SphereCollider[] _sphereColliders = default;
    [SerializeField] private BoxCollider[] _boxColliders = default;

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
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_cableNodes[i].CurrentPosition, 0.025f + RopeThickness / 2f);
        }
        Gizmos.DrawSphere(_cableNodes[NodeCount - 1].CurrentPosition, 0.025f);
        Gizmos.DrawSphere(_cableNodes[NodeCount - 1].CurrentPosition, 0.025f + RopeThickness / 2f);
    }

    private void Start()
    {
        InitializeCable();
    }

    private void FixedUpdate()
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

        // Resolve self collision.
        ResolveCollisionBetweenCables(_cableNodes, _cableNodes);

        // Solver.
        for (int z = 0; z < SolverCount; z++)
        {
            SatisfyConstrains();
            ResolveCollisionBetweenCables(_cableNodes, _cableNodes);
        }
    }
      public float MinRadius = 0.2f;

    private void ResolveCollisionBetweenCables(CableNode[] cable1, CableNode[] cable2)
    {
        for (int i = 0; i < cable1.Length; i++)
        {
            for (int j = i + 1; j < cable2.Length; j++)
            {
                Vector3 delta = cable2[j].CurrentPosition - cable1[i].CurrentPosition;
                float distance = Vector3.Distance(cable2[j].CurrentPosition, cable1[i].CurrentPosition);
                float minDistance = MinRadius * 2;

                if (distance < minDistance)
                {
                    Vector3 direction = delta / distance;
                    float correction = (minDistance - distance) / 2;
                    cable1[i].CurrentPosition -= direction * correction;
                    cable2[j].CurrentPosition += direction * correction;
                }
            }
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

    private void SatisfyConstrains()
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
        foreach (SphereCollider sphereCollider in _sphereColliders)
        {
            float distance = Vector3.Distance(cableNode.CurrentPosition, sphereCollider.transform.position);
            float radius = sphereCollider.radius;
            if (distance > radius + RopeThickness / 2)
            {
                // No collision;
                continue;
            }

            // Push point outside circle.
            Vector3 direction = (cableNode.CurrentPosition - sphereCollider.transform.position);
            Vector3 positionOnSphere = sphereCollider.transform.position + (direction.normalized * radius) + (direction.normalized * RopeThickness / 2);
            cableNode.CurrentPosition = positionOnSphere;
            cableNode.Acceleration = Vector3.zero;
            cableNode.Velocity = Vector3.zero;
        }
    }

    private void ResolveBoxCollisionForSingleNode(CableNode cableNode)
    {
        foreach (BoxCollider boxCollider in _boxColliders)
        {
            Vector3 localPositionToBoxCollider = boxCollider.transform.worldToLocalMatrix.MultiplyPoint(cableNode.CurrentPosition);
            Vector3 halfColliderSize = boxCollider.size * 0.5f;
            halfColliderSize += Vector3.one * RopeThickness / 2;

            Vector3 scalar = boxCollider.transform.localScale;
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
            Vector3 globalPositionToBoxCollider = boxCollider.transform.localToWorldMatrix.MultiplyPoint(localPositionToBoxCollider);
            cableNode.CurrentPosition = globalPositionToBoxCollider;
        }
    }
}
