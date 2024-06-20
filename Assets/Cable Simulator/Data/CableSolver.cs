using UnityEngine;

public class CableSolver : MonoBehaviour
{
    [Header("Settings:")]
    public int SolverCount = 10;
    public int NodeCount = 40;
    [Range(0, 0.2f)] public float NodeDistance = 0.1f;
    public float Gravity = 9.81f;
    [Range(0.01f, .9f)] public float Elasticity = 1;
    [Range(0.9f, 1.0f)] public float GravityDampening = 1;

    public bool EndHandle = default;
    public Transform EndHanlePoint = default;

    [Header("Runtime Info:")]
    [SerializeField] private CableNode[] _cableNodes = default;

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
            cableNode.Velocity = Vector3.down * Gravity * Time.deltaTime;
        }

        // Gravity pass + main rope pass.
        for (int i = 0; i < NodeCount; i++)
        {
            CableNode cableNode = _cableNodes[i];

            Vector3 currentPosition = cableNode.CurrentPosition;
            cableNode.CurrentPosition += ((cableNode.CurrentPosition - cableNode.OldPosition) * GravityDampening) + (cableNode.Velocity * Time.deltaTime);
            cableNode.OldPosition = currentPosition;
        }

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
        }
    }
}
