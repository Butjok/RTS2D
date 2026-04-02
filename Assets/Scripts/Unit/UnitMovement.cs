// Copyright 2026 Viktor Fedotov
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;

//using Drawing;
using UnityEngine;

/*
 * CONCEPT
 * 
 * Units are moving on a grid. Each unit occupies exactly one cell where it stands. 
 * When unit is about to move it 'reserves' the next cell on its path. Each unit can have only one reservation.
 * If that cell is already occupied or reserved by another unit -> it means the unit is stuck.
 * 
 * In this case the moving unit asks the unit in the way to step aside. The unit which tries to step aside simply finds a free nearby cell and moves there.
 * TODO: Edge case: currently if there is no free cell to step aside the moving unit gets completely stuck.
 * This should be mitigated by 'recursively' asking units to step aside and do it in a 'wave' fashion.
 * In case the unit gets stuck for too long then it should find another path by also marking the unit in the way as an obstacle for pathfinding. 
 */

public class UnitMovement : MonoBehaviour {

    [SerializeField] private Unit unit;
    [SerializeField] private Transform viewBase; // this is a transform which is actually getting rotated in a 3D view (not translated!)
    [SerializeField] private Vector2Int cell;
    [SerializeField] private LineRenderer pathRenderer;

    public bool shouldMoveAlongPath = true;

    [SerializeField] private float moveSpeed = 3;
    [SerializeField] private bool hasRotationSpeed = true;
    [SerializeField] private float rotationSpeed = 360;

    [SerializeField] private float stuckTimeThresholdForRepath = 1;
    [SerializeField] private float offThePathTimeThresholdForRepath = 1;
    [SerializeField] private float requestOtherUnitToStepAsideTimeThreshold = .5f;

    [SerializeField] private AudioClip crushingAudioClip;

    private readonly List<(Vector2 start, Vector2 end)> movePathSegments = new();

    private IEnumerator movementAnimationCoroutine;

    private float? stuckTime;
    private float? requestOtherUnitToStepAsideTimer;
    private float? offThePathTime;

    /// If true, the unit is standing in the way of another unit, and will try to move to a nearby cell to get out of the way.
    public List<Unit> wasRequestedToStepAsideBy = new();

    public bool ShouldStepAside => wasRequestedToStepAsideBy.Count > 0;

    /// Raw A* output with axis-aligned nodes with ever tile of the path represented as grid index-based node.
    /// It is dense, meaning that every single tile of the path is represented as a node, even if there are multiple consecutive tiles in a straight line.
    [SerializeField] private List<LevelGridBasedAStar.Node> aStarPath = new();

    /// Same as pathFinderPath but converted to world positions and with the last point being the actual move destination (not necessarily the center of the cell).
    [SerializeField] private List<Vector2> notSmoothPath = new();

    /// The final path that the unit will move along. It is smoothened and simplified, meaning that it has fewer points than the notSmoothMovePath and the points are not necessarily on the centers of the tiles. 
    [SerializeField] private List<Vector2> smoothPath = new();

    /// Every grid cell affected by the move path. It is used for checking if the unit is on the path and for finding the next cell to move to.  
    [SerializeField] private List<Vector2Int> smoothPathCells = new();

    public Vector2Int Cell => cell;
    public float MoveSpeed => moveSpeed;
    public IReadOnlyList<Vector2> MovePath => smoothPath;
    public IReadOnlyList<Vector2Int> MovePathCells => smoothPathCells;

    public Vector2Int? ReservedCell { get; private set; }

    public bool CanCrush(Unit otherUnit) {
        return otherUnit && otherUnit != unit && otherUnit.OwningPlayer != unit.OwningPlayer &&
               unit.UnitKind == Unit.Kind.Vehicle && otherUnit.UnitKind == Unit.Kind.Infantry;
    }

    public bool FindPathToDestination() {
        ClearPath();

        if (unit.CurrentOrder.MoveDestination is { } actualMoveDestination &&
            unit.World.Grid.GridBasedAStar.FindPath(unit, actualMoveDestination, aStarPath)) {
            foreach (var node in aStarPath)
                notSmoothPath.Add(unit.World.Grid.CellToWorldPosition(node.index));
            notSmoothPath[notSmoothPath.Count - 1] = actualMoveDestination;

            PathSmoother.SmoothenPath(unit.World.Grid, smoothPath, notSmoothPath, smoothPathCells);

            return true;
        }

        // ReSharper disable once RedundantIfElseBlock
        else {
            ClearPath();
            return false;
        }
    }

    private IEnumerator MoveToCell(Vector2 end, Vector2Int endCell) {
        Debug.Assert(unit.World.Grid[endCell].isWalkable);
        var otherUnit = unit.World.Grid[endCell].occupiedBy;
        Debug.Assert(!otherUnit || CanCrush(otherUnit));
        Debug.Assert(unit.World.Grid[endCell].reservedBy == unit);

        ReservedCell = endCell;

        var start = unit.transform.position.ToVector2();

        var desiredRotation = Quaternion.LookRotation((end - start).ToVector3());
        if (hasRotationSpeed) {
            if (!Mathf.Approximately(desiredRotation.eulerAngles.y, viewBase.rotation.eulerAngles.y)) {
                var elapsedTime = 0f;
                var startRotation = viewBase.rotation;
                var angleDifference = Quaternion.Angle(startRotation, desiredRotation);
                var duration = angleDifference / rotationSpeed;
                while (elapsedTime < duration) {
                    var a = elapsedTime / duration;
                    viewBase.rotation = Quaternion.Slerp(startRotation, desiredRotation, a);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                viewBase.rotation = desiredRotation;
            }
        }
        else
            viewBase.rotation = desiredRotation;

        {
            var elapsedTime = Time.deltaTime;
            var distance = Vector2.Distance(start, end);
            var duration = distance / moveSpeed;
            while (elapsedTime < duration) {
                var t = elapsedTime / duration;
                unit.transform.position = Vector2.Lerp(start, end, t).ToVector3();
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            unit.transform.position = end.ToVector3();
        }

        unit.World.Grid[cell].occupiedBy = null;

        otherUnit = unit.World.Grid[endCell].occupiedBy; // we need to update because the unit could have went away while we were moving
        if (otherUnit) {
            // if we get here it means there is an enemy unit on this cell and we can crush it, so we destroy the enemy unit

            otherUnit.Die();

            unit.World.AudioSystem.PlayOneShotWithCooldown(unit.EffectsAudioSource, crushingAudioClip);
        }

        unit.World.Grid[endCell].occupiedBy = unit;
        unit.World.Grid[endCell].reservedBy = null;

        cell = endCell;

        movementAnimationCoroutine = null;

        ReservedCell = null;
    }

    // Snap to grid and mark as occupied
    private void Awake() {
        cell = unit.World.Grid.WorldPositionToCell(unit.transform.position.ToVector2());

        Debug.Assert(unit.World.Grid[cell].isWalkable);
        Debug.Assert(!unit.World.Grid[cell].occupiedBy);
        Debug.Assert(!unit.World.Grid[cell].reservedBy);

        unit.transform.position = unit.World.Grid.CellToWorldPosition(cell).ToVector3();
        unit.World.Grid[cell].occupiedBy = unit;
    }

    private void OnDestroy() {
        if (unit && unit.World && unit.World.Grid) {
            unit.World.Grid[cell].occupiedBy = null;
            if (ReservedCell is { } actualReservedCell) {
                Debug.Assert(unit.World.Grid[actualReservedCell].reservedBy == unit);
                unit.World.Grid[actualReservedCell].reservedBy = null;
            }
        }
    }

    private void Update() {
        
        //Debug.Assert(!unit.CurrentOrder || (unit.CurrentOrder.OrderKind is UnitOrder.Kind.Harvest or UnitOrder.Kind.Unload || MovePath.Count >= 2),
        //    $"Unit {unit.name} has an order but its move path has less than 2 points. This should not happen because we should clear the order if we can't find a path to the destination, and if we have a path to the destination we should have at least 2 points in the path (the current position and the destination).");
        
        pathRenderer.enabled = unit.IsSelected && MovePath.Count >= 2;
        if (pathRenderer.enabled) {
            movePathSegments.Clear();
            for (var segmentIndex = 0; segmentIndex < MovePath.Count - 1; segmentIndex++) {
                var start = MovePath[segmentIndex];
                var end = MovePath[segmentIndex + 1];
                movePathSegments.Add((start, end));
            }

            pathRenderer.positionCount = movePathSegments.Count + 1;
            if (movePathSegments.Count > 0) {
                pathRenderer.SetPosition(0, movePathSegments[0].start.ToVector3());
                for (var i = 0; i < movePathSegments.Count; i++)
                    pathRenderer.SetPosition(i + 1, movePathSegments[i].end.ToVector3());
            }
        }

        if (unit.IsSelected && unit.OwningPlayer.PlayerController && unit.OwningPlayer.PlayerController.showMovePathCells) {
            for (var segmentIndex = 0; segmentIndex < smoothPathCells.Count - 1; segmentIndex++) {
                var start = unit.World.Grid.CellToWorldPosition(smoothPathCells[segmentIndex]);
                var end = unit.World.Grid.CellToWorldPosition(smoothPathCells[segmentIndex + 1]);

                //Draw.ingame.Arrow(start.ToVector3(), end.ToVector3(), Vector3.up, .1f, Color.cyan);
            }
        }

        // If we reached the end of the path but we don't have an attack target -> cancel the order
        if (MovePathCells.Count > 0 &&
            MovePathCells[MovePathCells.Count - 1] == Cell)
            ClearPath();

        if (movementAnimationCoroutine == null && ShouldStepAside) {
            wasRequestedToStepAsideBy.Clear();

            // find a random nearby cell that is not occupied or reserved and move there
            Vector2Int? freeCell = null;
            foreach (var offset in LevelGrid.moveDirections) {
                var nearbyCell = cell + offset;
                if (unit.World.Grid.InBounds(nearbyCell) && unit.World.Grid[nearbyCell].isWalkable && !unit.World.Grid[nearbyCell].occupiedBy && !unit.World.Grid[nearbyCell].reservedBy) {
                    freeCell = nearbyCell;
                    break;
                }
            }

            if (freeCell is { } actualFreeCell) {
                unit.World.Grid[actualFreeCell].reservedBy = unit;
                movementAnimationCoroutine = MoveToCell(
                    unit.World.Grid.CellToWorldPosition(actualFreeCell),
                    actualFreeCell);
                StartCoroutine(movementAnimationCoroutine);
            }
        }

        if (shouldMoveAlongPath && smoothPath.Count >= 2 && movementAnimationCoroutine == null) {
            Vector2Int? nextCell = null;
            for (var segmentStart = smoothPathCells.Count - 1; segmentStart >= 0; segmentStart--)
                if (smoothPathCells[segmentStart] == cell) {
                    nextCell = smoothPathCells[segmentStart + 1];
                    break;
                }

            if (nextCell is { } actualNextCell) {
                var unitInTheWay = unit.World.Grid[actualNextCell].occupiedBy;
                if (unitInTheWay == unit || CanCrush(unitInTheWay))
                    unitInTheWay = null;
                if (!unitInTheWay && !unit.World.Grid[actualNextCell].reservedBy) {
                    unit.World.Grid[actualNextCell].reservedBy = unit;
                    movementAnimationCoroutine = MoveToCell(
                        MathUtility.FindClosestPointOnPolyline(unit.World.Grid.CellToWorldPosition(actualNextCell), smoothPath, out _, out _, out var isEndOfPolyline),
                        actualNextCell);
                    StartCoroutine(movementAnimationCoroutine);
                }
                else if (unitInTheWay && unitInTheWay.Movement.movementAnimationCoroutine == null) {
                    if (requestOtherUnitToStepAsideTimer == null)
                        requestOtherUnitToStepAsideTimer = 0;
                    else {
                        requestOtherUnitToStepAsideTimer += Time.deltaTime;
                        if (requestOtherUnitToStepAsideTimer > requestOtherUnitToStepAsideTimeThreshold) {
                            unitInTheWay.Movement.wasRequestedToStepAsideBy.Add(unit);
                            requestOtherUnitToStepAsideTimer = null;
                        }
                    }
                }
                else {
                    if (stuckTime == null)
                        stuckTime = 0;
                    else {
                        stuckTime += Time.deltaTime;
                        if (stuckTime > stuckTimeThresholdForRepath) {
                            unit.World.RepathSystem.EnqueueUnitForRepath(unit);
                            stuckTime = null;
                        }
                    }
                }
            }

            // unit got off move path
            else {
                //Draw.ingame.Label3D(unit.transform.position + Vector3.up, Quaternion.identity, "Off the path!", .1f);

                if (offThePathTime == null)
                    offThePathTime = 0;
                else {
                    offThePathTime += Time.deltaTime;
                    if (offThePathTime > offThePathTimeThresholdForRepath) {
                        unit.World.RepathSystem.EnqueueUnitForRepath(unit);
                        offThePathTime = null;
                    }
                }
            }
        }
    }

    public void ClearPath() {
        aStarPath.Clear();
        notSmoothPath.Clear();
        smoothPath.Clear();
        smoothPathCells.Clear();
    }
}