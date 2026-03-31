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

using System;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public struct UnitOrder {

    public enum Kind {
        None,
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
    
    public static implicit operator bool(UnitOrder order) {
        return order.OrderKind != Kind.None;
    }

    public static UnitOrder Move(Object source,Vector2 destination) {
        return new UnitOrder {
            kind = Kind.Move,
            source = source,
            moveDestination = destination,
            creationTime =  Time.time
        };
    }

    public static UnitOrder Attack(Object source,Unit targetUnit, Vector2 destination) {
        return new UnitOrder {
            kind = Kind.Attack,
            source = source,
            moveDestination = destination,
            targetUnit = targetUnit,
            creationTime =  Time.time
        };
    }

    public static UnitOrder Attack(Object source,Building targetBuilding, Vector2 destination) {
        return new UnitOrder {
            kind = Kind.Attack,
            source = source,
            moveDestination = destination,
            targetBuilding = targetBuilding,
            creationTime =  Time.time
        };
    }

    public static UnitOrder Harvest(Object source, Vector2 destination) {
        return new UnitOrder {
            kind = Kind.Harvest,
            source = source,
            moveDestination = destination,
            creationTime =  Time.time
        };
    }

    public static UnitOrder Unload(Object source,RefineryBuilding refinery) {
        return new UnitOrder {
            kind = Kind.Unload,
            source = source,
            moveDestination = refinery.GoldDepositPosition,
            targetBuilding = refinery,
            creationTime =  Time.time
        };
    }
}