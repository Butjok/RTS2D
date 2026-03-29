using System.Collections;
using UnityEngine;

public abstract class UnitAttackAnimation : MonoBehaviour {
    [SerializeField] private Unit owningUnit;

    protected Unit OwningUnit => owningUnit;

    public abstract IEnumerator AttackAnimation(IAttackTarget target);
    public virtual void OnCancelled() { }
}