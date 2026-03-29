using System.Collections.Generic;
using UnityEngine;

public static class PathSmoother {

    private static readonly List<Vector2Int> segmentCells = new();
    private static readonly List<Vector2Int> walkableSegmentCells = new();

    public static void SmoothenPath(Grid grid, List<Vector2> smoothPath, IReadOnlyList<Vector2> notSmoothPath, List<Vector2Int> smoothPathCells) {
        smoothPathCells.Clear();
        smoothPath.Clear();

        var fixedEndIndex = notSmoothPath.Count - 1;
        var potentialSegmentStartIndex = fixedEndIndex - 2;

        smoothPath.Add(notSmoothPath[fixedEndIndex]); // add the end point to the result to start with
        smoothPathCells.Add(grid.WorldPositionToCell(notSmoothPath[fixedEndIndex]));

        while (potentialSegmentStartIndex >= 0) {
            segmentCells.Clear();
            segmentCells.AddRange(AmanatidesWoo(grid, notSmoothPath[fixedEndIndex], notSmoothPath[potentialSegmentStartIndex]));

            var segmentIsWalkable = true;
            foreach (var cell in segmentCells) {
                if (!grid[cell].isWalkable) {
                    segmentIsWalkable = false;
                    break;
                }
            }
            if (segmentIsWalkable) {
                // if walkable, move potentialSegmentStart back by one and try again
                potentialSegmentStartIndex--;
                walkableSegmentCells.Clear();
                walkableSegmentCells.AddRange(segmentCells);
            }
            else {
                // if not walkable, add the last walkable segment to the result and move fixedEnd and potentialSegmentStart back by one
                smoothPath.Add(notSmoothPath[potentialSegmentStartIndex + 1]);
                smoothPathCells.AddRange(walkableSegmentCells);
                walkableSegmentCells.Clear();

                fixedEndIndex = potentialSegmentStartIndex + 1;
                potentialSegmentStartIndex = fixedEndIndex - 2;
            }
        }

        smoothPathCells.AddRange(AmanatidesWoo(grid, smoothPath[smoothPath.Count - 1], notSmoothPath[0]));

        smoothPath.Add(notSmoothPath[0]); // add the start point to the result at the end
        smoothPath.Reverse();
        smoothPathCells.Reverse();
    }

    public static IEnumerable<Vector2Int> AmanatidesWoo(Grid grid, Vector2 start, Vector2 end) {
        const float cellSize = Grid.cellSize;

        var startIndex = grid.WorldPositionToCell(start);
        var x = startIndex.x;
        var y = startIndex.y;

        var endIndex = grid.WorldPositionToCell(end);
        if (startIndex == endIndex)
            yield break;

        var offset = end - start;

        var startCellPosition = grid.IndexToWorldPosition(startIndex);
        var border = startCellPosition + cellSize * new Vector2(
            offset.x > 0 ? .5f : -.5f,
            offset.y > 0 ? .5f : -.5f);

        var tMaxX = Mathf.Approximately(offset.x, 0) ? 999 : (border.x - start.x) / offset.x;
        var tMaxY = Mathf.Approximately(offset.y, 0) ? 999 : (border.y - start.y) / offset.y;

        var tDelta = new Vector2(cellSize / Mathf.Abs(offset.x), cellSize / Mathf.Abs(offset.y));
        var step = new Vector2Int(offset.x > 0 ? 1 : -1, offset.y > 0 ? 1 : -1);

        while (true) {
            if (tMaxX >= 1 && tMaxY >= 1) break;

            if (tMaxX < tMaxY) {
                tMaxX += tDelta.x;
                x += step.x;
            }
            else {
                tMaxY += tDelta.y;
                y += step.y;
            }

            yield return new Vector2Int(x, y);
        }
    }

    // This eventually might be used for manual construction of straight line movement.
    public static IEnumerable<Vector2Int> AmanatidesWoo(Vector2Int start, Vector2Int end) {
        if (start == end)
            yield break;

        var x = start.x;
        var y = start.y;

        var offset = end - start;

        var border = new Vector2(
            offset.x > 0 ? x + .5f : x - .5f,
            offset.y > 0 ? x + .5f : x - .5f);

        var tMaxX = Mathf.Approximately(offset.x, 0) ? 999 : (border.x - start.x) / offset.x;
        var tMaxY = Mathf.Approximately(offset.y, 0) ? 999 : (border.y - start.y) / offset.y;

        var tDelta = new Vector2(1f / Mathf.Abs(offset.x), 1f / Mathf.Abs(offset.y));
        var step = new Vector2Int(offset.x > 0 ? 1 : -1, offset.y > 0 ? 1 : -1);

        while (true) {
            if (tMaxX >= 1 && tMaxY >= 1) break;

            if (tMaxX < tMaxY) {
                tMaxX += tDelta.x;
                x += step.x;
            }
            else {
                tMaxY += tDelta.y;
                y += step.y;
            }

            yield return new Vector2Int(x, y);
        }
    }
}