using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public static class UnitFormation {

    private static readonly List<Vector2Int> projectionSearchArea = new();
    static UnitFormation() {
        projectionSearchArea.AddRange(EnumerateInSpiral(3));
    }

    public static IEnumerable<Vector2Int> EnumerateInSpiral(int maxRadius) {
        for (var radius = 0; radius <= maxRadius; radius++) {
            if (radius == 0) {
                yield return Vector2Int.zero;
                continue;
            }
            var edgeLength = radius * 2 + 1;
            var stepsOverEdge = edgeLength - 1;
            var position = new Vector2Int(radius, radius);
            var direction = new Vector2Int(-1, 0);
            for (var side = 0; side < 4; side++) {
                for (var step = 0; step < stepsOverEdge; step++) {
                    position += direction;
                    yield return position;
                }
                direction = new Vector2Int(-direction.y, direction.x); // rotate direction 90 degrees
            }
        }
    }

    private static readonly List<Unit> units = new();

    public static void FormAround(Vector2 center, Dictionary<Unit, Vector2> positions) {
        var unitRadius = 0f;

        float Circumference(float radius) {
            return 2 * Mathf.PI * radius;
        }

        float RingRadius(int ringIndex) {
            return ringIndex * 2 * unitRadius;
        }

        float RingLength(int ringIndex) {
            return Circumference(RingRadius(ringIndex));
        }

        int RingCapacity(int ringIndex) {
            if (ringIndex == 0)
                return 1;
            var length = RingLength(ringIndex);
            return Mathf.FloorToInt(length / (2 * unitRadius));
        }

        // we assume all the units have the same radius for now

        var positionAccumulator = Vector2.zero;
        var placedCount = 0;

        var ringIndex = 0;
        var indexInRing = 0;
        units.Clear();
        units.AddRange(positions.Keys);
        foreach (var unit in units) {
            if (unitRadius == 0)
                unitRadius = unit.RadiusInFormation;

            var ringCapacity = RingCapacity(ringIndex);
            if (indexInRing >= ringCapacity) {
                ringIndex++;
                indexInRing = 0;
                ringCapacity = RingCapacity(ringIndex);
            }

            var angle = (float)indexInRing / ringCapacity * 2 * Mathf.PI;
            var radius = RingRadius(ringIndex);
            var position = new Vector2(
                center.x + Mathf.Cos(angle) * radius,
                center.y + Mathf.Sin(angle) * radius);

            positions[unit] = position;
            positionAccumulator += position;
            placedCount++;

            indexInRing++;
        }

        if (placedCount > 0) {
            var averagePosition = positionAccumulator / placedCount;
            foreach (var unit in units) {
                var position = positions[unit];
                var offset = position - averagePosition;
                positions[unit] = center + offset;
            }
        }
    }

    private static readonly HashSet<Vector2Int> occupied = new();
    public static void ProjectToWalkable(Grid grid, Dictionary<Unit, Vector2> positions) {
        units.Clear();
        occupied.Clear();
        units.AddRange(positions.Keys);
        foreach (var unit in units) {
            var cell = grid.WorldPositionToCell(positions[unit]);
            foreach (var offset in projectionSearchArea) {
                var projectedCell = cell + offset;
                if (grid[projectedCell].isWalkable && !occupied.Contains(projectedCell)) {
                    occupied.Add(projectedCell);
                    var projectedPosition = grid.IndexToWorldPosition(projectedCell);
                    positions[unit] = projectedPosition;
                    break;
                }
            }
        }
    }
}