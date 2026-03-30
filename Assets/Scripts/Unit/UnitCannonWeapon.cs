using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UnitCannonWeapon : UnitWeapon {

    [Serializable]
    private class UnitTurret {
        public Transform turretTransform;
        public List<Transform> muzzleTransforms = new();
        public float turretRotationSpeed = 90;
    }

    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float shotInterval = .1f;
    [SerializeField] private AudioClip shotSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private UnitTurret turret;

    private void OnValidate() {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    protected override bool CanAttackNow(IAttackTarget attackTarget) {
        if (!base.CanAttackNow(attackTarget))
            return false;
        var turretRotation = turret.turretTransform.rotation;
        var directionToTarget = attackTarget.PositionToBeAttackedAt - turret.turretTransform.position;
        directionToTarget.Scale(new Vector3(1, 0, 1)); // ignore vertical difference for turret rotation
        var rotationToTarget = Quaternion.LookRotation(directionToTarget);
        var angleDifference = Quaternion.Angle(turretRotation, rotationToTarget);
        return angleDifference <= 5; // Allow a small angle difference to account for precision issues
    }

    // rotate turret to target or forward if no target
    private void Update() {
        var desiredRotation = OwningUnit.transform.rotation;
        if (OwningUnit.CurrentAttackTarget != null && OwningUnit.CurrentAttackTarget.ObjectExists) {
            var targetPosition = OwningUnit.CurrentAttackTarget.PositionToBeAttackedAt;
            var direction = targetPosition - turret.turretTransform.position;
            direction.Scale(new Vector3(1, 0, 1)); // ignore vertical difference for turret rotation
            if (direction.sqrMagnitude > 0.001f) // avoid zero direction which causes LookRotation to throw an error
                desiredRotation = Quaternion.LookRotation(direction);
        }
        turret.turretTransform.rotation = Quaternion.RotateTowards(turret.turretTransform.rotation, desiredRotation, turret.turretRotationSpeed * Time.deltaTime);
    }

    protected override IEnumerator AttackAnimation_Internal(IAttackTarget target) {

        Debug.Assert(turret.muzzleTransforms.Count > 0);

        var muzzleIndex = 0;
        for (var i = 0; i < burstCount; i++) {

            if (!target.ObjectExists)
                yield break;

            var position = turret.muzzleTransforms[muzzleIndex].position;
            var direction = target.PositionToBeAttackedAt - position;
            var rotation = Quaternion.LookRotation(direction);
            OwningUnit.World.Spawn(projectilePrefab, projectile => {
                projectile.Initialize(OwningUnit.World, projectilePrefab, OwningUnit, target.PositionToBeAttackedAt);
                projectile.transform.SetPositionAndRotation(position, rotation);
            });

            if (shotSound)
                OwningUnit.World.AudioSystem.PlayOneShotWithCooldown(audioSource, shotSound);

            muzzleIndex = (muzzleIndex + 1) % turret.muzzleTransforms.Count;

            var pauseStartTime = Time.time;
            while (Time.time < pauseStartTime + shotInterval)
                yield return null;
        }
    }
}