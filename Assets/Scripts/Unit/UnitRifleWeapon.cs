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

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))]
public class UnitRifleWeapon : UnitWeapon {

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float shotInterval = .05f;
    [SerializeField] private float tracerLifetime = .025f;
    [SerializeField] private AudioClip shotSound;
    [SerializeField] private AudioSource audioSource;

    private void OnValidate() {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Awake() {
        lineRenderer.enabled = false;
    }

    protected override IEnumerator AttackAnimation_Internal(IAttackTarget target) {
        if (target.ObjectExists) {

            target.ReceiveAttackFrom(OwningUnit.GetPrefab<Unit>());

            for (var i = 0; i < burstCount; i++) {

                if (!target.ObjectExists)
                    yield break;

                StartCoroutine(Tracer(transform.position, target.PositionToBeAttackedAt));

                if (i == 0 && shotSound)
                    OwningUnit.World.AudioSystem.PlayOneShotWithCooldown(audioSource, shotSound);

                var pauseStartTime = Time.time;
                while (Time.time < pauseStartTime + shotInterval)
                    yield return null;
            }
        }
    }

    private IEnumerator Tracer(Vector3 start, Vector3 end) {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.enabled = true;
        var startTime = Time.time;
        while (Time.time < startTime + tracerLifetime)
            yield return null;
        lineRenderer.enabled = false;
    }

    protected override void OnCancelled() {
        StopAllCoroutines();
        lineRenderer.enabled = false;
    }
}