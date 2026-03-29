using UnityEngine;

public interface IAttackTarget {
     public Vector3 AttackPosition { get; }
     public void ReceiveAttackFrom(Unit attacker);
     public bool ObjectExists { get; }
}