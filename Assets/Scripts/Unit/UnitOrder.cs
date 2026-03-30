using System;
using UnityEngine;

[Serializable]
public class UnitOrder {

    public enum Kind {
        Move,
        Attack,
        Harvest,
        Unload
    }

    [SerializeField] private Kind kind;
    [SerializeField] private Vector2 moveDestination;
    [SerializeField] private Unit targetUnit;
    [SerializeField] private Building targetBuilding;
    [SerializeField] private Vector2Int? harvestingCell;
    private float creationTime;

    public Vector2 MoveDestination => moveDestination;
    public Unit TargetUnit => targetUnit;
    public Building TargetBuilding => targetBuilding;
    public IAttackTarget AttackTarget => targetUnit ? targetUnit : targetBuilding;
    public float Age => Time.time - creationTime;
    public Kind OrderKind => kind;

    public static UnitOrder Move(Vector2 destination) {
        return new UnitOrder {
            kind = Kind.Move,
            moveDestination = destination
        };
    }

    public static UnitOrder Attack(Unit targetUnit, Vector2 destination) {
        return new UnitOrder {
            kind = Kind.Attack,
            moveDestination = destination,
            targetUnit = targetUnit
        };
    }

    public static UnitOrder Attack(Building targetBuilding, Vector2 destination) {
        return new UnitOrder {
            kind = Kind.Attack,
            moveDestination = destination,
            targetBuilding = targetBuilding
        };
    }

    public static UnitOrder Harvest(Vector2Int harvestingCell, Vector2 destination) {
        return new UnitOrder {
            kind = Kind.Harvest,
            moveDestination = destination,
            harvestingCell = harvestingCell
        };
    }

    public static UnitOrder Unload(RefineryBuilding refinery) {
        return new UnitOrder {
            kind = Kind.Unload,
            moveDestination = refinery.transform.position.ToVector2() + refinery.GoldDepositPosition,
            targetBuilding = refinery
        };
    }
}