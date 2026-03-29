using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class Unit : WorldBehaviour, ISelectable, IHasHealth, IAttackTarget, IBuildable, ICanBePrePlaced {

    public enum Kind {
        Infantry,
        Vehicle
    }

    [Serializable]
    public struct UnitTurret {
        [SerializeField] private Transform turretTransform;
        [SerializeField] private Transform barrelMuzzleTransform;

        public Transform TurretTransform => turretTransform;
        public Transform BarrelMuzzleTransform => barrelMuzzleTransform;
    }

    private static readonly int shader_playerColor = Shader.PropertyToID("_PlayerColor");

    [SerializeField] private UnitMovement movement;
    [SerializeField] private Player owningPlayer;

    [SerializeField] private float health = 1;
    [SerializeField] private int cost = 1000;
    [SerializeField] private float buildTime = 15;
    [SerializeField] private float radiusInFormation = .5f;

    [SerializeField] private List<Renderer> renderers = new();
    [SerializeField] private LineRenderer pathRenderer;
    [SerializeField] private Renderer selectionCircleRenderer;

    [SerializeField] private float attackRange = 3;
    [SerializeField] private float attackCooldown = .5f;
    [SerializeField] private float? lastAttackTime;
    [SerializeField] private UnitAttackAnimation attackAnimation;

    [SerializeField] private int voicePriority;
    [SerializeField] private List<AudioClip> onSelectedVoiceLines = new();
    [SerializeField] private List<AudioClip> onMoveOrderVoiceLines = new();
    [SerializeField] private List<AudioClip> onAttackOrderVoiceLines = new();
    [SerializeField] private List<AudioClip> deathScreams = new();

    [SerializeField] private UnitTurret turret;
    [SerializeField] private float turretRelativeYaw = 0;
    [SerializeField] private float turretRotationSpeed = 90;

    [SerializeField] private UnitOrder currentOrder = new();
    [SerializeField] private Kind kind = Kind.Infantry;

    [SerializeField] private AudioSource effectsAudioSource;

    private MaterialPropertyBlock materialPropertyBlock;
    private Color? playerColor;

    private readonly List<(Vector2 start, Vector2 end)> movePathSegments = new();

    private IEnumerator attackAnimationCoroutine;

    public UnitMovement Movement => movement;
    public float RadiusInFormation => radiusInFormation;
    public Vector2? MoveDestination => currentOrder.MoveDestination;
    public int VoicePriority => voicePriority;
    public IReadOnlyList<AudioClip> OnSelectedVoiceLines => onSelectedVoiceLines;
    public IReadOnlyList<AudioClip> OnMoveOrderVoiceLines => onMoveOrderVoiceLines;
    public IReadOnlyList<AudioClip> OnAttackOrderVoiceLines => onAttackOrderVoiceLines;
    public Kind UnitKind => kind;
    public AudioSource EffectsAudioSource => effectsAudioSource;

    public Bounds SelectionBounds {
        get {
            Debug.Assert(renderers.Count > 0, "There should be at least one renderer assigned to the unit for selection bounds to work.");
            return renderers[0].bounds;
        }
    }

    private bool isSelected = false;

    public bool IsSelected {
        get => isSelected;
        set {
            isSelected = value;
            selectionCircleRenderer.enabled = value;
        }
    }

    public float Health {
        get => health;
        set {
            health = Mathf.Clamp01(value);
            if (health <= 0)
                Die();
        }
    }

    public void Initialize(World world, Unit prefab, Player owningPlayer) {
        base.Initialize(world, prefab);
        this.owningPlayer = owningPlayer;
    }
    public void InitializeFromPrePlacedInfo(PrePlacedInfo info) {
        Initialize(info.World, info.Prefab as Unit, info.Player);
    }

    private void OnValidate() {
        if (renderers.Count == 0)
            renderers.AddRange(GetComponentsInChildren<Renderer>());
    }

    private void Awake() {
        PlayerColor = owningPlayer ? owningPlayer.Color : Color.white;
        World.onObjectDestroyed += CancelOrderIfTargetIsDestroyed;
    }

    private void OnDestroy() {
        World.onObjectDestroyed -= CancelOrderIfTargetIsDestroyed;
    }

    public Color PlayerColor {
        get {
            Debug.Assert(playerColor.HasValue, "Player color should have a value");
            return playerColor.Value;
        }
        set {
            playerColor = value;
            materialPropertyBlock ??= new MaterialPropertyBlock();
            materialPropertyBlock.SetColor(shader_playerColor, value);
            foreach (var renderer in renderers)
                renderer.SetPropertyBlock(materialPropertyBlock);
        }
    }

    public Player OwningPlayer => owningPlayer;

    public void SetOrder(UnitOrder newOrder) {
        currentOrder = newOrder;
        World.RepathSystem.EnqueueUnitForRepath(this);
        World.MovingUnitsSet.Add(this);
    }

    public void CancelOrder() {
        CancelAttackAnimation();
        currentOrder.Clear();
        World.MovingUnitsSet.Remove(this);
        movement.ClearPath();
        movement.shouldMoveAlongPath = true;
    }
    
    public bool AttackNow(IAttackTarget target) {

        if (Time.time - lastAttackTime < attackCooldown)
            return false;
        lastAttackTime = Time.time;

        if (attackAnimationCoroutine != null)
            StopCoroutine(attackAnimationCoroutine);

        attackAnimationCoroutine = AttackAnimation(target);
        StartCoroutine(attackAnimationCoroutine);

        return true;
    }

    private IEnumerator AttackAnimation(IAttackTarget target) {
        lastAttackTime = Time.time;
        yield return attackAnimation.AttackAnimation(target);
        attackAnimationCoroutine = null;
    }

    private void Update() {

        pathRenderer.enabled = IsSelected && movement.MovePath.Count >= 2;
        if (pathRenderer.enabled) {

            movePathSegments.Clear();
            for (var segmentIndex = 0; segmentIndex < movement.MovePath.Count - 1; segmentIndex++) {
                var start = movement.MovePath[segmentIndex];
                var end = movement.MovePath[segmentIndex + 1];
                movePathSegments.Add((start, end));
            }

            pathRenderer.positionCount = movePathSegments.Count + 1;
            if (movePathSegments.Count > 0) {
                pathRenderer.SetPosition(0, movePathSegments[0].start.ToVector3());
                for (var i = 0; i < movePathSegments.Count; i++)
                    pathRenderer.SetPosition(i + 1, movePathSegments[i].end.ToVector3());
            }
        }

        // If we have an attack target and it's in range -> attack it, otherwise move towards it 
        if (currentOrder.AttackTarget is { ObjectExists: true } && DistanceTo(currentOrder.AttackTarget) <= attackRange) {
            AttackNow(currentOrder.AttackTarget);
            movement.shouldMoveAlongPath = false;
        }
        else
            movement.shouldMoveAlongPath = true;
    }

    public void Die() {

        //if (effectsAudioSource && deathScreams.Count > 0)
            //World.AudioSystem.PlayRandomOneShotWithCooldown(World.AudioSystem.effe, deathScreams);

        CancelAttackAnimation();
        World.Destroy(this);
    }

    private void CancelAttackAnimation() {
        if (attackAnimationCoroutine != null) {
            StopCoroutine(attackAnimationCoroutine);
            attackAnimation.OnCancelled();
            attackAnimationCoroutine = null;
        }
    }

    public Vector3 PositionToBeAttackedAt => transform.position;

    public void ReceiveAttackFrom(Unit attacker) {
        var damage = World.DamageStats.GetDamage((Unit)attacker.Prefab, (Unit)Prefab);
        Health -= damage;
    }

    public bool ObjectExists => this;

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

    private void CancelOrderIfTargetIsDestroyed(Object obj) {
        if (obj == currentOrder.TargetUnit || obj == currentOrder.TargetBuilding)
            CancelOrder();
    }

    public bool CanAttack(Building building) {
        return building && owningPlayer != building.OwningPlayer;
    }
    public bool CanAttack(Unit otherUnit) {
        return otherUnit && owningPlayer != otherUnit.OwningPlayer;
    }

    public int? Cost => cost;
    public float BuildTime => buildTime;
    public bool PrerequisitesSatisfied => true;
}