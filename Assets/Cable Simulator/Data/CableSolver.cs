using System;
using System.Collections.Generic;
using UnityEngine;

public class CableSolver : MonoBehaviour
{
    [Header("Settings:")]
    public int SolverCount = 10;
    public int NodeCount = 40;
    public float CableThickness = 0.05f;
    [Range(0, 0.2f)] public float NodeDistance = 0.1f;
    public float ColliderNodeDistance = 0.01f;
    public float Gravity = 9.81f;
    [Range(0.9f, 1.0f)] public float GravityDampening = 1;
    [Range(0.01f, 1f)] public float Elasticity = 1;
    [Space]
    public bool EndHandle = default;
    public Transform EndHanlePoint = default;
    [Space]
    [SerializeField] private CollisionHandler _collisionHandler = default;
    [Space]
    [Header("Runtime Info:")]
    [SerializeField] private CableNode[] _cableNodes = default;
    [SerializeField] private List<CableColliders> _cableColliders = new List<CableColliders>();
    [SerializeField] private CableCollisionInfo _cableCollisionInfo = default;
    [SerializeField] private float _maxDistanceBetweenNodes = default;

    public CableNode[] CableNodes { get { return _cableNodes; } }
    public List<CableVirtualPoint> VirtualPoints = new List<CableVirtualPoint>();

    private Vector3 firstHandle = default;
    //private Vector3 secondHandle = default;

    public float totalRopeMass = 1;
    public float magic = 100;
    public float totalRopeDistance = default;

    private void OnValidate()
    {
        _maxDistanceBetweenNodes = NodeDistance + ((1 - Elasticity) * NodeDistance);
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
        foreach (CableVirtualPoint cableVirtualPoint in VirtualPoints)
        {
            cableVirtualPoint.TargetPosition = cableVirtualPoint.transform.position;
        }
    }

    private void InitializeCable()
    {
        _cableNodes = new CableNode[NodeCount];
        float massPerParticle = totalRopeMass / NodeCount;

        for (int i = 0; i < NodeCount; i++)
        {
            Vector3 nodeStartPosition = transform.position + (Vector3.down * NodeDistance * i);
            _cableNodes[i] = new CableNode(nodeStartPosition, massPerParticle);
        }
    }

    public void RegisterVirtualPoint(CableVirtualPoint cableVirtualPoint)
    {
        if (VirtualPoints.Contains(cableVirtualPoint))
        {
            return;
        }

        VirtualPoints.Add(cableVirtualPoint);
    }

    public void UnregisterVirtualPoint(CableVirtualPoint cableVirtualPoint)
    {
        if (!VirtualPoints.Contains(cableVirtualPoint))
        {
            return;
        }

        VirtualPoints.Remove(cableVirtualPoint);
    }


    public Vector3 calculatedForce;
    public float forceStrength;
    public float MaxStrength;
    private void OnUpdated(double deltaTime)
    {
        // Calculate gravity velocity.
        for (int i = 0; i < NodeCount; i++)
        {
            CableNode cableNode = _cableNodes[i];
            cableNode.Acceleration = Vector3.down * Gravity;
            cableNode.Velocity = cableNode.CurrentPosition - cableNode.OldPosition;
        }

        // Gravity pass + main rope pass.
        for (int i = 0; i < NodeCount; i++)
        {
            CableNode cableNode = _cableNodes[i];
            Vector3 newPosition = cableNode.CurrentPosition + (cableNode.Velocity * GravityDampening) + (cableNode.Acceleration * cableNode.Mass * (float)deltaTime);
            cableNode.PredictedPosition = newPosition;
            cableNode.PositionBeforeAdjustment = newPosition;
        }

        // Solver.
        for (int z = 0; z < SolverCount; z++)
        {
            totalRopeDistance = 0;

            for (int i = 0; i < NodeCount - 1; i++)
            {
                CableNode node1 = _cableNodes[i];
                CableNode node2 = _cableNodes[i + 1];


                foreach (CableVirtualPoint cableVirtualPoint in VirtualPoints)
                {
                    if (i == cableVirtualPoint.NodeIndex || i + 1 == cableVirtualPoint.NodeIndex)
                    {
                        if (cableVirtualPoint.VirtualPointType.Equals(VirtualPointType.DYNAMIC))
                        {
                            Vector3 dir = cableVirtualPoint.TargetPosition - _cableNodes[cableVirtualPoint.NodeIndex].PredictedPosition;
                            calculatedForce = dir * (float)deltaTime * magic;
                            forceStrength = calculatedForce.magnitude;
                            forceStrength = Mathf.Clamp(forceStrength, 0, MaxStrength);
                            _cableNodes[cableVirtualPoint.NodeIndex].PredictedPosition += dir.normalized * forceStrength;
                        }
                        else if (cableVirtualPoint.VirtualPointType.Equals(VirtualPointType.STATIC))
                        {
                            _cableNodes[cableVirtualPoint.NodeIndex].PredictedPosition = cableVirtualPoint.TargetPosition;
                        }
                    }
                }

                Vector3 direction = node2.PredictedPosition - node1.PredictedPosition; // Towards Node 2.
                float distance = direction.magnitude;

                float pushPullStrength = (distance - NodeDistance) / distance;

                if (pushPullStrength > 0)
                {
                    node1.Distance = distance;
                    node1.PushPullStrength = pushPullStrength;
                    Vector3 neededTranslation = direction * Elasticity * pushPullStrength;
                    node1.PredictedPosition += neededTranslation;
                    node2.PredictedPosition -= neededTranslation;
                    node1.PositionBeforeAdjustment = node1.PredictedPosition;
                }

                totalRopeDistance += distance;

                _collisionHandler.ResolveSphereCollisionForNode(node1, CableThickness);
                _collisionHandler.ResolveBoxCollisionForNode(node1, CableThickness);
                _collisionHandler.ResolveSphereCollisionForNode(node2, CableThickness);
                _collisionHandler.ResolveBoxCollisionForNode(node2, CableThickness);
            }
        }

        for (int i = 0; i < NodeCount; i++)
        {
            CableNode cableNode = _cableNodes[i];

            cableNode.OldPosition = cableNode.CurrentPosition;
            cableNode.CurrentPosition = cableNode.PredictedPosition;
            foreach (CableVirtualPoint cableVirtualPoint in VirtualPoints)
            {
                if (cableVirtualPoint.NodeIndex == i)
                {
                    if (cableVirtualPoint.VirtualPointType.Equals(VirtualPointType.DYNAMIC))
                    {
                        cableVirtualPoint.ActualPosition = cableNode.CurrentPosition;
                        cableVirtualPoint.Velocity = (cableNode.CurrentPosition - cableNode.OldPosition) / (float)deltaTime;
                    }
                }
            }
        }
    }
}
