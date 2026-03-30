using System;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class UnitOrder {

    public enum Kind {
        Move,
        Attack,
        Harvest,
        Unload
    }

    [SerializeField] private Object source;
    [SerializeField] private Kind kind;
    [SerializeField] private Vector2 moveDestination;
    [SerializeField] private Unit targetUnit;
    [SerializeField] private Building targetBuilding;
    [SerializeField] private float creationTime;

    public Object Source => source;
    public Vector2 MoveDestination => moveDestination;
    public Unit TargetUnit => targetUnit;
    public Building TargetBuilding => targetBuilding;
    public IAttackTarget AttackTarget => targetUnit ? targetUnit : targetBuilding;
    public float Age => Time.time - creationTime;
    public Kind OrderKind => kind;
    
    public static UnitOrder Move(Object source,Vector2 destination) {
        return new UnitOrder {
            kind = Kind.Move,
            source = source,
            moveDestination = destination
        };
    }

    public static UnitOrder Attack(Object source,Unit targetUnit, Vector2 destination) {
        return new UnitOrder {
            kind = Kind.Attack,
            source = source,
            moveDestination = destination,
            targetUnit = targetUnit
        };
    }

    public static UnitOrder Attack(Object source,Building targetBuilding, Vector2 destination) {
        return new UnitOrder {
            kind = Kind.Attack,
            source = source,
            moveDestination = destination,
            targetBuilding = targetBuilding
        };
    }

    public static UnitOrder Harvest(Object source, Vector2Int harvestingCell, Vector2 destination) {
        return new UnitOrder {
            kind = Kind.Harvest,
            source = source,
            moveDestination = destination
        };
    }

    public static UnitOrder Unload(Object source,RefineryBuilding refinery) {
        return new UnitOrder {
            kind = Kind.Unload,
            source = source,
            moveDestination = refinery.transform.position.ToVector2() + refinery.GoldDepositPosition,
            targetBuilding = refinery
        };
    }
}