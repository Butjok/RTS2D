using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))]
public class UnitRifleFireAttackAnimation : UnitAttackAnimation {

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

    public override IEnumerator AttackAnimation(IAttackTarget target) {
        if (target.ObjectExists) {

            target.ReceiveAttackFrom(OwningUnit);

            for (var i = 0; i < burstCount; i++) {

                if (!target.ObjectExists)
                    yield break;

                StartCoroutine(Tracer(transform.position, target.AttackPosition));

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

    public override void OnCancelled() {
        StopAllCoroutines();
        lineRenderer.enabled = false;
    }
}