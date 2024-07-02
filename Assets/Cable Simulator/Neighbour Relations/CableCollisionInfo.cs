using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CableCollisionInfo
{
    private const int ExtraRange = 5;
    public List<NodeCollisionInfo> Collisions = new List<NodeCollisionInfo>();
    public HashSet<int> HashSetUniqueIndexNeighbours = new HashSet<int>();
    public List<int> UniqueIndexNeighbours = new List<int>();

    public void AddCollisionIndex(int index, int nodeCount, CableNode[] cableNodes)
    {
        NodeCollisionInfo nodeCollisionInfo = Collisions.Where(nci => nci.IndexInCollision == index).FirstOrDefault();
        if (nodeCollisionInfo != null) return;
        NodeCollisionInfo neighbourIndice = new NodeCollisionInfo();
        neighbourIndice.IndexInCollision = index;

        for (int j = index - ExtraRange; j <= index + ExtraRange; j++)
        {
            if (j < 0 || j > nodeCount - 1) continue;
            neighbourIndice.Neighbours.Add(j);
            //cableNodes[j].Elasticity = 0.1f;
        }

        Collisions.Add(neighbourIndice);

        HashSetUniqueIndexNeighbours.UnionWith(neighbourIndice.Neighbours);
        UniqueIndexNeighbours = HashSetUniqueIndexNeighbours.ToList();

    }

    public void RemoveCollisionIndex(int index, CableNode[] cableNodes)
    {
        NodeCollisionInfo nodeCollisionInfo = Collisions.Where(nci => nci.IndexInCollision == index).FirstOrDefault();
        if (nodeCollisionInfo == null) return;
        Collisions.Remove(nodeCollisionInfo);

        // Check which one we've lost.
        HashSet<int> temp = new HashSet<int>();
        foreach (NodeCollisionInfo nci in Collisions)
        {
            temp.UnionWith(nci.Neighbours);
        }

        foreach (int value in HashSetUniqueIndexNeighbours)
        {
            if (temp.Contains(value))
            {
                // we still have it.
            }
            else
            {
                // we have just removed it!
                Debug.Log("Removing: " + value);
                //cableNodes[value].Elasticity = 0.9f;
            }
        }

        HashSetUniqueIndexNeighbours = temp;
        UniqueIndexNeighbours = temp.ToList();
    }
}
