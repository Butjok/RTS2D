using System;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;

public class LevelGridBasedAStar {

    [Serializable]
    public struct Node {

        public Vector2Int index;
        public float gCost;
        public ulong runSerial;
        public Vector2Int? cameFrom;

        public float HCost(Vector2Int goalIndex) => LevelGrid.Distance(index, goalIndex);
        public float FCost(Vector2Int goalIndex) => gCost + HCost(goalIndex);

        public Node(Vector2Int index) {
            this.index = index;
            gCost = int.MaxValue;
            runSerial = 0;
            cameFrom = null;
        }
    }

    private readonly LevelGrid grid;
    private readonly Node[,] nodes;
    private readonly SimplePriorityQueue<Vector2Int> openSet = new();

    /// A monotonic sequential number of A* run. Used to determine if a node's data is from the current or previous run.
    /// This allows us to avoid having to clear the nodes' data after each run, which would be costly.
    private ulong runSerial = 0;

    public LevelGridBasedAStar(LevelGrid grid) {
        this.grid = grid;
        nodes = new Node[grid.Size.x, grid.Size.y];
        for (var x = 0; x < grid.Size.x; x++)
        for (var y = 0; y < grid.Size.y; y++) {
            var index = new Vector2Int(x, y);
            nodes[x, y] = new Node(index);
        }
    }

    private ref Node this[Vector2Int index] => ref nodes[index.x, index.y];

    public bool FindPath(Unit unit, Vector2 goalPosition, List<Node> path) {

        var startIndex = unit.Movement.ReservedCell ?? unit.Movement.Cell;
        var goalIndex = grid.WorldPositionToCell(goalPosition);
        Debug.Assert(grid.InBounds(goalIndex));

        if (!grid[goalIndex].isWalkable)
            return false;

        ref var startNode = ref this[startIndex];
        runSerial++;
        startNode.runSerial = runSerial;
        startNode.gCost = 0;
        startNode.cameFrom = null;

        openSet.Clear();
        openSet.Enqueue(startIndex, startNode.FCost(goalIndex));

        while (openSet.Count > 0) {
            var index = openSet.Dequeue();
            if (index == goalIndex) {
                ReconstructPath(index, path);
                return true;
            }

            ref var current = ref this[index];

            foreach (var neighbourIndex in grid.EnumerateNeighborIndices(index)) {
                if (!grid[neighbourIndex].isWalkable)
                    continue;

                var unitAtNeighbour = grid[neighbourIndex].occupiedBy;

                var offset = neighbourIndex - index;
                var isDiagonal = offset.x != 0 && offset.y != 0;
                var moveDistance = isDiagonal ? LevelGrid.sqrt2 : 1;
                var moveCost = moveDistance * (unitAtNeighbour ? 5 : 1);
                var tentativeGCost = current.gCost + moveCost;

                ref var neighbour = ref this[neighbourIndex];
                var updateNeighbor = neighbour.runSerial != runSerial || tentativeGCost < neighbour.gCost;
                if (updateNeighbor) {
                    neighbour.runSerial = runSerial;
                    neighbour.gCost = tentativeGCost;
                    neighbour.cameFrom = index;
                    var neighborFCost = neighbour.FCost(goalIndex);
                    if (openSet.Contains(neighbourIndex))
                        openSet.UpdatePriority(neighbourIndex, neighborFCost);
                    else
                        openSet.Enqueue(neighbourIndex, neighborFCost);
                }
            }
        }

        return false;
    }

    private void ReconstructPath(Vector2Int endIndex, List<Node> path) {
        path.Clear();
        for (Vector2Int? index = endIndex; index.HasValue; index = this[index.Value].cameFrom) {
            var node = this[index.Value];
            Debug.Assert(node.runSerial == runSerial);
            path.Add(node);
        }
        path.Reverse();
    }
}