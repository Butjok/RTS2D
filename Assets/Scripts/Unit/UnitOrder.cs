using System;
using UnityEngine;

/**
 * <summary>
 * Represents an order for a unit to execute. This can be either a move order (with a destination) or an attack order (with a target unit or building).
 * </summary>
 */
[Serializable]
public struct UnitOrder {

    [SerializeField] private Vector2? moveDestination;
    [SerializeField] private Unit targetUnit;
    [SerializeField] private Building targetBuilding;

    public Vector2? MoveDestination => moveDestination;
    public Unit TargetUnit => targetUnit;
    public Building TargetBuilding => targetBuilding;

    public UnitOrder(Vector2 moveDestination, Unit targetUnit = null, Building targetBuilding = null) {
        Debug.Assert(!targetUnit && !targetBuilding ||
                     targetUnit ^ targetBuilding,
            "An order can have either a target unit or a target building, but not both");

        this.moveDestination = moveDestination;
        this.targetUnit = targetUnit;
        this.targetBuilding = targetBuilding;
    }

    public void Clear() {
        moveDestination = null;
        targetUnit = null;
        targetBuilding = null;
    }
}