using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class Unit : WorldBehaviour, ISelectable, IHasHealth, IAttackTarget, ICanBePrePlaced {

    public enum Kind {
        Infantry,
        Vehicle
    }

    private static readonly int shader_playerColor = Shader.PropertyToID("_PlayerColor");

    [SerializeField] private UnitMovement movement;
    [SerializeField] private Player owningPlayer;

    [SerializeField] private float health = 1;
    private bool isSelected = false;
    [SerializeField] private float radiusInFormation = .5f;

    [SerializeField] private List<Renderer> renderers = new();
    [SerializeField] private Renderer selectionCircleRenderer;

    [SerializeField] private UnitWeapon weapon;

    [SerializeField] private int voicePriority;
    [SerializeField] private List<AudioClip> onSelectedVoiceLines = new();
    [SerializeField] private List<AudioClip> onMoveOrderVoiceLines = new();
    [SerializeField] private List<AudioClip> onAttackOrderVoiceLines = new();
    [SerializeField] private List<AudioClip> deathScreams = new();

    [SerializeField] private UnitOrder currentOrder;
    [SerializeField] private Kind kind = Kind.Infantry;

    [SerializeField] private AudioSource effectsAudioSource;
    [SerializeField] private MonoBehaviour orderSource;

    private MaterialPropertyBlock materialPropertyBlock;
    private Color? playerColor;
    private float? lastDamageTime;

    public UnitMovement Movement => movement;
    public float RadiusInFormation => radiusInFormation;
    public ref UnitOrder CurrentOrder => ref currentOrder;
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

    public float? LastDamageTime => lastDamageTime;

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
        CancelOrder();
        currentOrder = newOrder;
        World.RepathSystem.EnqueueUnitForRepath(this);
        World.MovingUnitsSet.Add(this);
    }

    public void CancelOrder() {
        currentOrder = new();
        World.MovingUnitsSet.Remove(this);
        movement.ClearPath();
        movement.shouldMoveAlongPath = true;
    }

    private void Update() {
        if (weapon) {
            if ((bool)currentOrder && currentOrder.AttackTarget != null && currentOrder.AttackTarget.ObjectExists && weapon.IsInAttackRange(currentOrder.AttackTarget)) {
                weapon.AttackNow(currentOrder.AttackTarget);
                movement.shouldMoveAlongPath = false;
            }
            else
                movement.shouldMoveAlongPath = true;
        }
    }

    public void Die() {
        //if (effectsAudioSource && deathScreams.Count > 0)
        //World.AudioSystem.PlayRandomOneShotWithCooldown(World.AudioSystem.effe, deathScreams);

        World.Destroy(this);
    }

    public Vector3 PositionToBeAttackedAt => transform.position;

    public void ReceiveAttackFrom(Unit attacker) {
        lastDamageTime = Time.time;
        var damage = World.DamageStats.GetDamage(attacker.GetPrefab<Unit>(), GetPrefab<Unit>());
        Health -= damage;
    }

    public bool ObjectExists => this;
    
    public bool CanReceiveOrderFrom(PlayerController playerController) {
        return playerController && playerController.Player == owningPlayer;
    }

    private void CancelOrderIfTargetIsDestroyed(Object obj) {
        if (currentOrder && (obj == currentOrder.TargetUnit || obj == currentOrder.TargetBuilding))
            CancelOrder();
    }

    public bool CanAttack(Building building) {
        return building && owningPlayer != building.OwningPlayer;
    }

    public bool CanAttack(Unit otherUnit) {
        return otherUnit && owningPlayer != otherUnit.OwningPlayer;
    }
}