using UnityEngine;

[RequireComponent(typeof(CableSolver))]
public class CableGizmoVisualizer : MonoBehaviour
{
    private CableSolver _cableSolver = default;

    private CableSolver CableSolver
    {
        get
        {
            if (_cableSolver == null)
            {
                _cableSolver = GetComponent<CableSolver>();
            }
            return _cableSolver;
        }
    }
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        for (int i = 0; i < CableSolver.NodeCount; i++)
        {
            if (i % 2 == 0)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.white;
            }
            //Gizmos.DrawLine(CableSolver.CableNodes[i].CurrentPosition, CableSolver.CableNodes[i + 1].CurrentPosition);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(CableSolver.CableNodes[i].CurrentPosition, 0.025f);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(CableSolver.CableNodes[i].PositionBeforeAdjustment, 0.02f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(CableSolver.CableNodes[i].CurrentPosition, CableSolver.CableThickness);
            //Gizmos.color = Color.cyan;
            //Gizmos.DrawRay(CableSolver.CableNodes[i].CurrentPosition, CableSolver.CableNodes[i].Velocity.normalized);
        }
        //Gizmos.color = Color.red;
        //Gizmos.DrawSphere(CableSolver.CableNodes[CableSolver.NodeCount - 1].CurrentPosition, 0.025f);
        //Gizmos.color = Color.green;
        //Gizmos.DrawWireSphere(CableSolver.CableNodes[CableSolver.NodeCount - 1].CurrentPosition, 0.025f + CableSolver.CableThickness / 2f);

        //if (_cableColliders.Count > 0)
        //    for (int i = 0; i < _cableColliders.Count; i++)
        //    {
        //        Gizmos.color = Color.green;
        //        Gizmos.DrawWireSphere(_cableColliders[i].position, CableThickness / 2);
        //    }
    }
}
