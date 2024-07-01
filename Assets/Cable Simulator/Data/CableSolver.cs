using System;
using System.Threading;
using UnityEngine;

public struct CustomSphereCollider
{
    public Vector3 Position;
    public float Radius;
}

public struct CustomBoxCollider
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Matrix4x4 LocalToWorldMatrix;
    public Matrix4x4 WorldToLocalMatrix;
    public Vector3 Scale;
    public Vector3 Size;
}

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
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_cableNodes[i].wantedPosition, 0.02f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_cableNodes[i].CurrentPosition, 0.025f + RopeThickness / 2f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(_cableNodes[i].CurrentPosition, _cableNodes[i].Velocity.normalized);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_cableNodes[NodeCount - 1].CurrentPosition, 0.025f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_cableNodes[NodeCount - 1].CurrentPosition, 0.025f + RopeThickness / 2f);
    }

    private Thread _thread;
    private bool _isRunning = default;

    private CustomSphereCollider[] _customSphereColliders = default;
    private CustomBoxCollider[] _customBoxColliders = default;

    private void Start()
    {
        _customSphereColliders = new CustomSphereCollider[_sphereColliders.Length];
        for (int i = 0; i < _sphereColliders.Length; i++)
        {
            _customSphereColliders[i] = new CustomSphereCollider()
            {
                Position = _sphereColliders[i].transform.position,
                Radius = _sphereColliders[i].radius
            };
        }

        _customBoxColliders = new CustomBoxCollider[_boxColliders.Length];
        for (int i = 0; i < _boxColliders.Length; ++i)
        {
            _customBoxColliders[i] = new CustomBoxCollider()
            {
                Position = _boxColliders[i].transform.position,
                Rotation = _boxColliders[i].transform.rotation,
                Scale = _boxColliders[i].transform.localScale,
                LocalToWorldMatrix = _boxColliders[i].transform.localToWorldMatrix,
                WorldToLocalMatrix = _boxColliders[i].transform.worldToLocalMatrix,
                Size = _boxColliders[i].size
            };
        }

        InitializeCable();

        _isRunning = true;
        _thread = new Thread(MyLoop);
        _thread.Start();
    }

    private void OnDestroy()
    {
        _isRunning = false;
    }

    private void MyLoop()
    {
        while (_isRunning)
        {
            if (_isFixedRan)
            {
                _isFixedRan = false;
                int amountOfPasses = 0;
                float deltaTime = 0.02f;
                while (deltaTime > 0)
                {
                    Refresh(Mathf.Min(subStepDt, deltaTime));
                    deltaTime -= subStepDt;
                    amountOfPasses += 1;
                }
                Debug.Log(amountOfPasses);
            }
        }
    }

    private Vector3 thisHandle, otherHandle;

    public float subStepDt = 0.01f;
    private bool _isFixedRan = default;

    private void FixedUpdate()
    {
        _isFixedRan = true;
    }
    private void Update()
    {
        thisHandle = transform.position;
        otherHandle = EndHanlePoint.position;

        return;
        int amountOfPasses = 0;
        float deltaTime = Time.deltaTime;
        while (deltaTime > 0)
        {
            Refresh(Mathf.Min(subStepDt, deltaTime));
            deltaTime -= subStepDt;
            amountOfPasses += 1;
        }
        Debug.Log(amountOfPasses);
    }

    private void Refresh(float deltaTime)
    {
        // Calculate gravity velocity.
        for (int i = 0; i < NodeCount; i++)
        {
            CableNode cableNode = _cableNodes[i];
            Vector3 force = Vector3.down * Gravity * cableNode.Mass;
            cableNode.Acceleration = (force / cableNode.Mass) * deltaTime * deltaTime;
            cableNode.Velocity = cableNode.CurrentPosition - cableNode.OldPosition;
        }

        // Gravity pass + main rope pass.
        for (int i = 0; i < NodeCount; i++)
        {
            CableNode cableNode = _cableNodes[i];
            Vector3 newPosition = cableNode.CurrentPosition + (cableNode.Velocity * GravityDampening) + (cableNode.Acceleration * cableNode.Mass);
            cableNode.OldPosition = cableNode.CurrentPosition;
            cableNode.CurrentPosition = newPosition;

            // Collision check.
            ResolveSphereCollisionForSingleNode(cableNode);
            ResolveBoxCollisionForSingleNode(cableNode);
        }

        // Resolve self collision.
        //ResolveCollisionBetweenCables(_cableNodes, _cableNodes);

        // Solver.
        for (int z = 0; z < SolverCount; z++)
        {
            SatisfyConstrains();
            //ResolveCollisionBetweenCables(_cableNodes, _cableNodes);
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
            _cableNodes[i] = new CableNode(nodeStartPosition, NodeDistance);
        }
    }

    [ContextMenu("Ide gas")]
    public void InsertNode()
    {
        CableNode cableNode = _cableNodes[30];
        CableNode newObject = new CableNode(cableNode.CurrentPosition, 0.1f);
        _cableNodes = InsertAtIndex<CableNode>(_cableNodes, newObject, 30);
        NodeCount++;
    }

    public static T[] InsertAtIndex<T>(T[] array, T newObject, int index)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array), "Array cannot be null.");
        }

        if (index < 0 || index > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }

        T[] newArray = new T[array.Length + 1];

        // Copy elements before the index
        for (int i = 0; i < index; i++)
        {
            newArray[i] = array[i];
        }

        // Insert new object at the specified index
        newArray[index] = newObject;

        // Copy elements after the index
        for (int i = index; i < array.Length; i++)
        {
            newArray[i + 1] = array[i];
        }

        return newArray;
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
                node1.CurrentPosition = thisHandle;
            }
            if (EndHandle && i == NodeCount - 2)
            {
                node2.CurrentPosition = otherHandle;
            }


            Vector3 direction = node1.CurrentPosition - node2.CurrentPosition; // Towards Node 1.
            float distance = Vector3.Distance(node1.CurrentPosition, node2.CurrentPosition);

            float pushPullStrength = default;
            if (distance > 0)
            {
                pushPullStrength = (node1.NodeDistance - distance) / distance;
            }

            node1.Distance = distance;
            node1.PushPullStrength = pushPullStrength;
            Vector3 neededTranslation = direction * Elasticity * pushPullStrength;
            node1.CurrentPosition += neededTranslation;
            node2.CurrentPosition -= neededTranslation;

            //// Special case.
            //if (i == 0)
            //{
            //    node1.CurrentPosition = thisHandle;
            //}
            //if (EndHandle && i == NodeCount - 2)
            //{
            //    node2.CurrentPosition = otherHandle;
            //}

            node1.wantedPosition = Vector3.zero;
            // Collision handling.
            ResolveSphereCollisionForSingleNode(node1);
            ResolveBoxCollisionForSingleNode(node1);
        }
    }

    private void ResolveSphereCollisionForSingleNode(CableNode cableNode)
    {
        foreach (CustomSphereCollider sphereCollider in _customSphereColliders)
        {
            float distance = Vector3.Distance(cableNode.CurrentPosition, sphereCollider.Position);
            float radius = sphereCollider.Radius;
            if (distance > radius + RopeThickness / 2)
            {
                // No collision;
                continue;
            }

            // Push point outside circle.
            Vector3 direction = (cableNode.CurrentPosition - sphereCollider.Position);
            Vector3 positionOnSphere = sphereCollider.Position + (direction.normalized * radius) + (direction.normalized * RopeThickness / 2);
            cableNode.wantedPosition = cableNode.CurrentPosition;
            cableNode.OldPosition = positionOnSphere;
            cableNode.CurrentPosition = positionOnSphere;
            cableNode.Acceleration = Vector3.zero;
            cableNode.Velocity = Vector3.zero;
        }
    }

    private void ResolveBoxCollisionForSingleNode(CableNode cableNode)
    {
        foreach (CustomBoxCollider boxCollider in _customBoxColliders)
        {
            Vector3 localPositionToBoxCollider = boxCollider.WorldToLocalMatrix.MultiplyPoint(cableNode.CurrentPosition);
            Vector3 halfColliderSize = boxCollider.Size * 0.5f;
            halfColliderSize += Vector3.one * RopeThickness / 2;

            Vector3 scalar = boxCollider.Scale;
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
            Vector3 globalPositionToBoxCollider = boxCollider.LocalToWorldMatrix.MultiplyPoint(localPositionToBoxCollider);
            cableNode.wantedPosition = cableNode.CurrentPosition;
            cableNode.OldPosition = globalPositionToBoxCollider;
            cableNode.CurrentPosition = globalPositionToBoxCollider;
        }
    }
}
