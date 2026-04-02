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
    [SerializeField] private Transform hullTransform;

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
        var desiredRotation = hullTransform.rotation;
        if (OwningUnit.CurrentOrder && OwningUnit.CurrentOrder.AttackTarget != null && OwningUnit.CurrentOrder.AttackTarget.ObjectExists) {
            var targetPosition = OwningUnit.CurrentOrder.AttackTarget.PositionToBeAttackedAt;
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