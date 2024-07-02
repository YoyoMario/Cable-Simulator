using System;
using UnityEngine;

public class CableSolver : MonoBehaviour
{
    [Header("Settings:")]
    public int SolverCount = 10;
    public int NodeCount = 40;
    public float CableThickness = 0.05f;
    [Range(0, 0.2f)] public float NodeDistance = 0.1f;
    public float Gravity = 9.81f;
    [Range(0.9f, 1.0f)] public float GravityDampening = 1;
    [Range(0.01f, .9f)] public float Elasticity = 1;
    [Space]
    public bool EndHandle = default;
    public Transform EndHanlePoint = default;
    [Space]
    [SerializeField] private CollisionHandler _collisionHandler = default;
    [Space]
    [Header("Runtime Info:")]
    [SerializeField] private CableNode[] _cableNodes = default;
    [SerializeField] private CableCollisionInfo _cableCollisionInfo = default;

    private Vector3 firstHandle = default;
    private Vector3 secondHandle = default;

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
            Gizmos.color = (_cableNodes[i].IsSpecial) ? Color.cyan : Color.green;
            Gizmos.DrawWireSphere(_cableNodes[i].CurrentPosition, 0.025f + CableThickness / 2f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(_cableNodes[i].CurrentPosition, _cableNodes[i].Velocity.normalized);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_cableNodes[NodeCount - 1].CurrentPosition, 0.025f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_cableNodes[NodeCount - 1].CurrentPosition, 0.025f + CableThickness / 2f);
    }

    private void Awake()
    {
        _collisionHandler = new CollisionHandler(_collisionHandler.SphereColliders, _collisionHandler.BoxColliders);
        InitializeCable();
    }

    private void Start()
    {
        ThreadManager.Updated += OnUpdated;
    }

    private void OnDestroy()
    {
        ThreadManager.Updated -= OnUpdated;
    }

    private void Update()
    {
        firstHandle = transform.position;
        secondHandle = EndHanlePoint.position;
    }

    private void OnUpdated(double deltaTime)
    {
        Refresh((float)deltaTime);
    }

    private void InitializeCable()
    {
        _cableNodes = new CableNode[NodeCount];
        for (int i = 0; i < NodeCount; i++)
        {
            Vector3 nodeStartPosition = transform.position + (Vector3.down * NodeDistance * i);
            _cableNodes[i] = new CableNode(nodeStartPosition, NodeDistance, Elasticity);
        }
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
        }

        // Solver.
        for (int z = 0; z < SolverCount; z++)
        {
            SatisfyConstrains();
        }

        for (int i = 0; i < NodeCount; i++)
        {
            // Collision handling.
            bool isInCollision = _collisionHandler.ResolveSphereCollisionForNode(_cableNodes[i], CableThickness);
            if (isInCollision)
            {
                _cableCollisionInfo.AddCollisionIndex(i, NodeCount, _cableNodes);
            }
            else
            {
                _cableCollisionInfo.RemoveCollisionIndex(i, _cableNodes);
            }
            _collisionHandler.ResolveBoxCollisionForNode(_cableNodes[i], CableThickness);
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
                node1.CurrentPosition = firstHandle;
            }
            if (EndHandle && i == NodeCount - 2)
            {
                node2.CurrentPosition = secondHandle;
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
            Vector3 neededTranslation = direction * node1.Elasticity * pushPullStrength;
            node1.CurrentPosition += neededTranslation;
            node2.CurrentPosition -= neededTranslation;

            node1.wantedPosition = Vector3.zero;
        }
    }



    //[ContextMenu("Ide gas")]
    //public void InsertNode()
    //{
    //    for (int i = 0; i < 10; i++)
    //    {
    //        CableNode cableNode = _cableNodes[20];
    //        CableNode newObject = new CableNode(cableNode.CurrentPosition, 0.05f, true);
    //        _cableNodes = InsertAtIndex<CableNode>(_cableNodes, newObject, 30);
    //        NodeCount++;
    //    }

    //}

    //public static T[] InsertAtIndex<T>(T[] array, T newObject, int index)
    //{
    //    if (array == null)
    //    {
    //        throw new ArgumentNullException(nameof(array), "Array cannot be null.");
    //    }

    //    if (index < 0 || index > array.Length)
    //    {
    //        throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
    //    }

    //    T[] newArray = new T[array.Length + 1];

    //    // Copy elements before the index
    //    for (int i = 0; i < index; i++)
    //    {
    //        newArray[i] = array[i];
    //    }

    //    // Insert new object at the specified index
    //    newArray[index] = newObject;

    //    // Copy elements after the index
    //    for (int i = index; i < array.Length; i++)
    //    {
    //        newArray[i + 1] = array[i];
    //    }

    //    return newArray;
    //}
}
