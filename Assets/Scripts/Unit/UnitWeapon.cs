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