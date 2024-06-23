using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data
{
    public class Simulation : MonoBehaviour
    {
        enum ColliderType
        {
            Circle,
            Box,
            None,
        }

    class CollisionInfo
    {
        public int id;

        public ColliderType colliderType;
        public Vector2 colliderSize;
        public Vector2 position;
        public Vector2 scale;
        public Matrix4x4 wtl;
        public Matrix4x4 ltw;
        public int numCollisions;
        public int[] collidingNodes; // You probably want to use byte[] here instead, unless you have >255 nodes.

        public CollisionInfo(int maxCollisions)
        {
            this.id = -1;
            this.colliderType = ColliderType.None;
            this.colliderSize = Vector2.zero;
            this.position = Vector2.zero;
            this.scale = Vector2.zero;
            this.wtl = Matrix4x4.zero;
            this.ltw = Matrix4x4.zero;

            this.numCollisions = 0;
            this.collidingNodes = new int[maxCollisions];
        }
    }

    [Serializable]
        public class VerletNode
        {
            public Vector2 position;
            public Vector2 oldPosition;

            public VerletNode(Vector2 startPos)
            {
                this.position = startPos;
                this.oldPosition = startPos;
            }
        }

        [Header("Collider Settings:")]
        // Maximum total number of colliders that the rope can touch.
        public int MAX_ROPE_COLLISIONS = 32;
        // Collision radius around each node.  Set it high to avoid tunneling.
        public float COLLISION_RADIUS = .5f;
        // Collider buffer size; the maximum number of colliders that a single node can touch at once.
        public int COLLIDER_BUFFER_SIZE = 8;

        [Header("Rope Settings:")]
        public bool useStart;
        public Transform MovableTransformStart = default;
        public bool useMiddle;
        public Transform MovableTransformMiddle = default;
        public bool useEnd;
        public Transform MovableTransformEnd = default;
        public int iterations = 80;

        [Range(0.05f, 0.95f)]
        public float elasticity = 0.5f;
        public int totalNodes = 40;
        public float nodeDistance = .1f;
        public Vector2 gravity = new Vector2(0, -9.81f);

        private VerletNode[] nodes;

        private int numCollisions;
        private bool shouldSnapshotCollision;
        private CollisionInfo[] collisionInfos;
        private Collider2D[] colliderBuffer;



        private void Awake()
        {
            nodes = new VerletNode[totalNodes];

            // Spawn nodes starting from the transform position and working down.
            Vector3 pos = transform.position;
            for (int i = 0; i < totalNodes; i++)
            {
                nodes[i] = new VerletNode(pos);
                pos.y -= nodeDistance;
            }

            //*--*snip * --
            //Allocate collision structures.
            collisionInfos = new CollisionInfo[MAX_ROPE_COLLISIONS];
            for (int i = 0; i < collisionInfos.Length; i++)
            {
                // Each collider can collide with as many nodes as are in the rope.
                collisionInfos[i] = new CollisionInfo(totalNodes);
            }

            // Buffer for `OverlapCircleNonAlloc`.
            colliderBuffer = new Collider2D[COLLIDER_BUFFER_SIZE];
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            for (int i = 0; i < nodes.Length - 1; i++)
            {
                if (i % 2 == 0)
                {
                    Gizmos.color = Color.green;
                }
                else
                {
                    Gizmos.color = Color.white;
                }
                Gizmos.DrawLine(nodes[i].position, nodes[i + 1].position);

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(nodes[i].position, 0.025f);

                //Vector3 direction = nodes[i].position - nodes[i+1].position; // Towards node 1.
                //Gizmos.color = Color.yellow;
                //DrawArrow.ForGizmo(nodes[i].position, direction, 0.05f, 20f);
            }
        }

        private void Update()
        {
            Simulate();
            for (int i = 0; i < iterations; i++)
            {
                PolishSimulatedPoints();
            }
            shouldSnapshotCollision = true;
            SnapshotCollisions();
            AdjustCollisions();
        }

        private void Simulate()
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                VerletNode node = nodes[i];

                Vector3 temp = node.position;
                node.position += (node.position - node.oldPosition) + gravity * (Time.deltaTime * Time.deltaTime);
                node.oldPosition = temp;
            }
        }

        private void PolishSimulatedPoints()
        {
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                VerletNode node1 = nodes[i];
                VerletNode node2 = nodes[i + 1];

                // First node follows the mouse, for debugging.
                if (i == 0 && useStart)
                {
                    // Camera.main is terribly inefficient here, you should cache the camera.
                    node1.position = MovableTransformStart.position;
                }
                else if (i == nodes.Length / 2 && useMiddle)
                {
                    node1.position = MovableTransformMiddle.position;
                }
                else if (i == nodes.Length - 2 && useEnd)
                {
                    node2.position = MovableTransformEnd.position;
                }

                Vector2 direction = node1.position - node2.position; // Towards node 1.
                float distance = direction.magnitude;

                float pullStrengthTowardsPreviousPoint = 0;
                // Guard against divide by 0.
                if (distance > 0)
                {
                    pullStrengthTowardsPreviousPoint = (nodeDistance - distance) / distance;
                }

                Vector2 translate = direction * elasticity * pullStrengthTowardsPreviousPoint;

                //if(i != 0)
                node1.position += translate;
                node2.position -= translate;
            }
        }

        private int SnapshotCollisions()
        {
            numCollisions = 0;
            // Loop through each node and get collisions within a radius.
            for (int i = 0; i < nodes.Length; i++)
            {
                int collisions =
                    Physics2D.OverlapCircleNonAlloc(nodes[i].position, COLLISION_RADIUS, colliderBuffer);

                for (int j = 0; j < collisions; j++)
                {
                    Collider2D col = colliderBuffer[j];
                    int id = col.GetInstanceID();

                    // Check if we already have this collider in our collisionInfos.
                    int idx = -1;
                    for (int k = 0; k < numCollisions; k++)
                    {
                        if (collisionInfos[k].id == id)
                        {
                            idx = k;
                            break;
                        }
                    }

                    // If we didn't have the collider, we need to add it.
                    if (idx < 0)
                    {
                        // Record all the data we need to use into our class.
                        CollisionInfo ci = collisionInfos[numCollisions];
                        ci.id = id;
                        ci.wtl = col.transform.worldToLocalMatrix;
                        ci.ltw = col.transform.localToWorldMatrix;
                        ci.scale.x = ci.ltw.GetColumn(0).magnitude;
                        ci.scale.y = ci.ltw.GetColumn(1).magnitude;
                        ci.position = col.transform.position;
                        ci.numCollisions = 1;   // 1 collision, this one.
                        ci.collidingNodes[0] = i;

                        switch (col)
                        {
                            case CircleCollider2D c:
                                ci.colliderType = ColliderType.Circle;
                                ci.colliderSize.x = ci.colliderSize.y = c.radius;
                                break;
                            case BoxCollider2D b:
                                ci.colliderType = ColliderType.Box;
                                ci.colliderSize = b.size;
                                break;
                            default:
                                ci.colliderType = ColliderType.None;
                                break;
                        }

                        numCollisions++;
                        if (numCollisions >= MAX_ROPE_COLLISIONS)
                        {
                            return numCollisions;
                        }
                        // If we found the collider, then we just have to increment collisions and add our node.
                    }
                    else
                    {
                        CollisionInfo ci = collisionInfos[idx];
                        if (ci.numCollisions >= totalNodes)
                        {
                            continue;
                        }

                        ci.collidingNodes[ci.numCollisions++] = i;
                    }
                }
            }

            shouldSnapshotCollision = false;
            return numCollisions;
        }

        private void AdjustCollisions()
        {
            for (int i = 0; i < numCollisions; i++)
            {
                CollisionInfo ci = collisionInfos[i];

                switch (ci.colliderType)
                {
                    case ColliderType.Circle:
                        {
                            float radius = ci.colliderSize.x * Mathf.Max(ci.scale.x, ci.scale.y);

                            for (int j = 0; j < ci.numCollisions; j++)
                            {
                                VerletNode node = nodes[ci.collidingNodes[j]];
                                float distance = Vector2.Distance(ci.position, node.position);

                                // Early out if we're not colliding.
                                if (distance - radius > 0)
                                {
                                    continue;
                                }

                                // Push point outside circle.
                                Vector2 dir = (node.position - ci.position).normalized;
                                Vector2 hitPos = ci.position + dir * radius;
                                node.position = hitPos;
                            }
                        }
                        break;

                    case ColliderType.Box:
                        {
                            for (int j = 0; j < ci.numCollisions; j++)
                            {
                                VerletNode node = nodes[ci.collidingNodes[j]];
                                Vector2 localPoint = ci.wtl.MultiplyPoint(node.position);


                                // If distance from center is more than box "radius", then we can't be colliding.
                                Vector2 half = ci.colliderSize * .5f;
                                Vector2 scalar = ci.scale;
                                float dx = localPoint.x;
                                float px = half.x - Mathf.Abs(dx);
                                if (px <= 0)
                                {
                                    continue;
                                }

                                float dy = localPoint.y;
                                float py = half.y - Mathf.Abs(dy);
                                if (py <= 0)
                                {
                                    continue;
                                }

                                // Push node out along closest edge.
                                // Need to multiply distance by scale or we'll mess up on scaled box corners.
                                if (px * scalar.x < py * scalar.y)
                                {
                                    float sx = Mathf.Sign(dx);
                                    localPoint.x = half.x * sx;
                                }
                                else
                                {
                                    float sy = Mathf.Sign(dy);
                                    localPoint.y = half.y * sy;
                                }

                                Vector2 hitPos = ci.ltw.MultiplyPoint(localPoint);
                                node.position = hitPos;
                            }
                        }
                        break;
                }
            }
        }
    }
}