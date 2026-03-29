using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UnitCannonFireAttackAnimation : UnitAttackAnimation {

    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float shotInterval = .1f;
    [SerializeField] private AudioClip shotSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<Transform> muzzles = new();

    private void OnValidate() {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public override IEnumerator AttackAnimation(IAttackTarget target) {

        Debug.Assert(muzzles.Count > 0);

        var muzzleIndex = 0;
        for (var i = 0; i < burstCount; i++) {

            if (!target.ObjectExists)
                yield break;

            var position = muzzles[muzzleIndex].position;
            var direction = target.AttackPosition - position;
            var rotation = Quaternion.LookRotation(direction);
            OwningUnit.World.Spawn(projectilePrefab, projectile => {
                projectile.Initialize(OwningUnit.World, projectilePrefab, OwningUnit, target.AttackPosition);
                projectile.transform.SetPositionAndRotation(position, rotation);
            });

            if (shotSound)
                OwningUnit.World.AudioSystem.PlayOneShotWithCooldown(audioSource, shotSound);

            var pauseStartTime = Time.time;
            while (Time.time < pauseStartTime + shotInterval)
                yield return null;
        }
    }
}