using System;
using System.Collections.Generic;

[Serializable]
public class NodeCollisionInfo
{
    public int IndexInCollision;
    public List<int> Neighbours = new List<int>();
}
