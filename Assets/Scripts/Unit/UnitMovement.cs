using System;
using System.Collections;
using System.Collections.Generic;
//using Drawing;
using UnityEngine;

public class UnitMovement : MonoBehaviour {

    [SerializeField] private Unit unit;
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
    public List<Unit> wasRequestToStepAsideBy = new();

    public bool ShouldStepAside => wasRequestToStepAsideBy.Count > 0;

    /// Raw A* output with axis-aligned nodes with ever tile of the path represented as grid index-based node.
    /// It is dense, meaning that every single tile of the path is represented as a node, even if there are multiple consecutive tiles in a straight line.
    [SerializeField] private List<GridBasedAStar.Node> aStarPath = new();

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
        return otherUnit != null && otherUnit != unit && otherUnit.OwningPlayer != unit.OwningPlayer &&
               unit.UnitKind == Unit.Kind.Vehicle && otherUnit.UnitKind == Unit.Kind.Infantry;
    }

    public bool FindPathToDestination() {

        ClearPath();

        if (unit.MoveDestination is { } actualMoveDestination &&
            unit.World.Grid.GridBasedAStar.FindPath(unit, actualMoveDestination, aStarPath)) {

            foreach (var node in aStarPath)
                notSmoothPath.Add(unit.World.Grid.IndexToWorldPosition(node.index));
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
        var enemyUnit = unit.World.Grid[endCell].occupiedBy;
        Debug.Assert(!enemyUnit || CanCrush(enemyUnit));
        Debug.Assert(unit.World.Grid[endCell].reservedBy == unit);

        ReservedCell = endCell;

        var start = transform.position.ToVector2();

        var desiredRotation = Quaternion.LookRotation((end - start).ToVector3());
        if (hasRotationSpeed) {
            if (!Mathf.Approximately(desiredRotation.eulerAngles.y, transform.rotation.eulerAngles.y)) {
                var elapsedTime = 0f;
                var startRotation = transform.rotation;
                var angleDifference = Quaternion.Angle(startRotation, desiredRotation);
                var duration = angleDifference / rotationSpeed;
                while (elapsedTime < duration) {
                    var a = elapsedTime / duration;
                    transform.rotation = Quaternion.Slerp(startRotation, desiredRotation, a);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                transform.rotation = desiredRotation;
            }
        }
        else
            transform.rotation = desiredRotation;

        {
            var elapsedTime = Time.deltaTime;
            var distance = Vector2.Distance(start, end);
            var duration = distance / moveSpeed;
            while (elapsedTime < duration) {
                var t = elapsedTime / duration;
                transform.position = Vector2.Lerp(start, end, t).ToVector3();
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.position = end.ToVector3();
        }

        unit.World.Grid[cell].occupiedBy = null;

        if (enemyUnit) {
            // if we get here it means there is an enemy unit on this cell and we can crush it, so we destroy the enemy unit

            enemyUnit.Die();

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
        cell = unit.World.Grid.WorldPositionToCell(transform.position.ToVector2());

        Debug.Assert(unit.World.Grid[cell].isWalkable);
        Debug.Assert(!unit.World.Grid[cell].occupiedBy);
        Debug.Assert(!unit.World.Grid[cell].reservedBy);

        transform.position = unit.World.Grid.IndexToWorldPosition(cell).ToVector3();
        unit.World.Grid[cell].occupiedBy = unit;
    }

    private void OnDestroy() {
        if (unit && unit.World && unit.World.Grid) {
            unit.World.Grid[cell].occupiedBy = null;
            if (ReservedCell is {} actualReservedCell) {
                Debug.Assert(unit.World.Grid[actualReservedCell].reservedBy == unit);
                unit.World.Grid[actualReservedCell].reservedBy = null;
            }
        }
    }

    private void Update() {
        
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
                var start = unit.World.Grid.IndexToWorldPosition(smoothPathCells[segmentIndex]);
                var end = unit.World.Grid.IndexToWorldPosition(smoothPathCells[segmentIndex + 1]);
                //Draw.ingame.Arrow(start.ToVector3(), end.ToVector3(), Vector3.up, .1f, Color.cyan);
            }
        }

        // If we reached the end of the path but we don't have an attack target -> cancel the order
        if (MovePathCells.Count > 0 &&
            MovePathCells[MovePathCells.Count - 1] == Cell)
            ClearPath();

        if (movementAnimationCoroutine == null && ShouldStepAside) {
            wasRequestToStepAsideBy.Clear();

            // find a random nearby cell that is not occupied or reserved and move there
            Vector2Int? freeCell = null;
            foreach (var offset in Grid.moveDirections) {
                var nearbyCell = cell + offset;
                if (unit.World.Grid.InBounds(nearbyCell) && unit.World.Grid[nearbyCell].isWalkable && !unit.World.Grid[nearbyCell].occupiedBy && !unit.World.Grid[nearbyCell].reservedBy) {
                    freeCell = nearbyCell;
                    break;
                }
            }

            if (freeCell is { } actualFreeCell) {
                unit.World.Grid[actualFreeCell].reservedBy = unit;
                movementAnimationCoroutine = MoveToCell(
                    unit.World.Grid.IndexToWorldPosition(actualFreeCell),
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
                        MathUtility.FindClosestPointOnPolyline(unit.World.Grid.IndexToWorldPosition(actualNextCell), smoothPath, out _, out _, out var isEndOfPolyline),
                        actualNextCell);
                    StartCoroutine(movementAnimationCoroutine);
                }
                else if (unitInTheWay && unitInTheWay.Movement.movementAnimationCoroutine == null) {
                    if (requestOtherUnitToStepAsideTimer == null)
                        requestOtherUnitToStepAsideTimer = 0;
                    else {
                        requestOtherUnitToStepAsideTimer += Time.deltaTime;
                        if (requestOtherUnitToStepAsideTimer > requestOtherUnitToStepAsideTimeThreshold) {
                            unitInTheWay.Movement.wasRequestToStepAsideBy.Add(unit);
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

                //Draw.ingame.Label3D(transform.position + Vector3.up, Quaternion.identity, "Off the path!", .1f);

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