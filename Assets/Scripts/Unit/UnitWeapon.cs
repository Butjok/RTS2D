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
using System.Collections;
using UnityEngine;

public abstract class UnitWeapon : MonoBehaviour {

    [SerializeField] private Unit owningUnit;
    [SerializeField] private float attackRange = 3;
    [SerializeField] private float attackCooldown = .5f;
    
    private float? lastAttackTime;
    protected IEnumerator attackAnimationCoroutine;

    protected Unit OwningUnit => owningUnit;

    private void OnDestroy() {
        CancelAttackAnimation();
    }

    protected bool IsNotOnCooldown {
        get {
            var elapsedTime = Time.time - (lastAttackTime ?? -999);
            return elapsedTime >= attackCooldown;
        }
    }

    public bool IsInAttackRange(IAttackTarget attackTarget) {
        return attackTarget != null &&
               attackTarget.ObjectExists &&
               DistanceTo(attackTarget) <= attackRange;
    }

    protected virtual bool CanAttackNow(IAttackTarget attackTarget) {
        return IsNotOnCooldown &&
               IsInAttackRange(attackTarget);
    }

    public bool AttackNow(IAttackTarget attackTarget) {
        if (!CanAttackNow(attackTarget))
            return false;

        if (attackAnimationCoroutine != null)
            StopCoroutine(attackAnimationCoroutine);

        attackAnimationCoroutine = AttackAnimation(attackTarget);
        StartCoroutine(attackAnimationCoroutine);

        return true;
    }

    public void CancelAttackAnimation() {
        if (attackAnimationCoroutine != null) {
            StopCoroutine(attackAnimationCoroutine);
            OnCancelled();
            attackAnimationCoroutine = null;
        }
    }

    public float DistanceTo(Building targetBuilding) {
        var closestPointOnBuilding = targetBuilding.BoxCollider.ClosestPoint(transform.position);
        return Vector3.Distance(transform.position, closestPointOnBuilding);
    }
    public float DistanceTo(Unit targetUnit) {
        return Vector3.Distance(transform.position, targetUnit.transform.position);
    }
    public float DistanceTo(IAttackTarget attackTarget) {
        return attackTarget switch {
            Building building => DistanceTo(building),
            Unit unit => DistanceTo(unit),
            _ => throw new ArgumentException("Unknown attack target type")
        };
    }

    private IEnumerator AttackAnimation(IAttackTarget target) {
        lastAttackTime = Time.time;
        yield return AttackAnimation_Internal(target);
        attackAnimationCoroutine = null;
    }

    protected abstract IEnumerator AttackAnimation_Internal(IAttackTarget target);
    protected virtual void OnCancelled() { }
}