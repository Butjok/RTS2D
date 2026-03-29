using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Building : WorldBehaviour, ISelectable, IHasHealth, IAttackTarget, IBuildable, ICanBePrePlaced {
    [Serializable]
    public struct BuildingPrerequisite {
        [SerializeField] private Building buildingPrefab;
        [SerializeField] private int requiredCount;

        public Building BuildingPrefab => buildingPrefab;
        public int RequiredCount => requiredCount;
    }

    [Serializable]
    public struct BuildingQueueItem {
        [SerializeField] private Unit unitPrefab;
        [SerializeField] private Building buildingPrefab;
        [SerializeField] private float timeElapsed;

        public Unit UnitPrefab => unitPrefab;
        public Building BuildingPrefab => buildingPrefab;

        public float TimeElapsed {
            get => timeElapsed;
            set => timeElapsed = value;
        }
    }

    private static readonly int shader_playerColor = Shader.PropertyToID("_PlayerColor");
    private static readonly int shader_baseColor = Shader.PropertyToID("_Color");

    [SerializeField] private Player owningPlayer;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private List<Unit> buildableUnits = new();
    [SerializeField] private List<Material> sharedGhostMaterials = new();

    [FormerlySerializedAs("buildingBoxCollider")] [SerializeField]
    private BoxCollider boxCollider;

    [SerializeField] private bool isFullyBuilt = true;
    [SerializeField] private bool canEverBeSelected = true;
    [SerializeField] private int cost = 1000;
    [SerializeField] private float buildTime = 60;
    [SerializeField] private List<BuildingPrerequisite> prerequisites = new();
    [SerializeField] private List<BuildingQueueItem> buildingQueue = new();

    private float health = 1;

    private bool playConstructionAnimationOnStart;
    private IEnumerator constructionAnimationCoroutine;

    private MaterialPropertyBlock materialPropertyBlock;
    private Color? playerColor;

    public BoxCollider BoxCollider => boxCollider;

    public void Initialize(World world, Building prefab, Player owningPlayer) {
        base.Initialize(world, prefab);
        OwningPlayer = owningPlayer;
    }
    public void InitializeFromPrePlacedInfo(PrePlacedInfo info) {
        Initialize(info.World, info.Prefab as Building, info.Player);
    }

    public Color PlayerColor {
        get {
            Debug.Assert(playerColor.HasValue, "Player color should have a value by the time it's accessed");
            return playerColor.Value;
        }
        set {
            playerColor = value;
            materialPropertyBlock ??= new MaterialPropertyBlock();
            materialPropertyBlock.SetColor(shader_playerColor, value);
            meshRenderer.SetPropertyBlock(materialPropertyBlock);
        }
    }

    public Vector3 PlacementExtents => meshRenderer.bounds.extents;

    public bool IsValidGhostPlacement {
        set {
            var color = value ? Color.green : Color.red;
            foreach (var sharedMaterial in sharedGhostMaterials)
                sharedMaterial.SetColor(shader_baseColor, color);
        }
    }

    private void Start() {
        if (playConstructionAnimationOnStart) {
            constructionAnimationCoroutine = ConstructionAnimation();
            StartCoroutine(constructionAnimationCoroutine);
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

    // Public setter because building can change their alliance
    public Player OwningPlayer {
        get => owningPlayer;
        set {
            owningPlayer = value;
            PlayerColor = owningPlayer ? owningPlayer.Color : Color.white;
        }
    }

    public Bounds SelectionBounds => meshRenderer.bounds;

    public bool IsSelected { get; set; } = false;
    public bool CanEverBeSelected => canEverBeSelected;

    public void SetPlayConstructionAnimationOnStart(bool value) {
        playConstructionAnimationOnStart = value;
        isFullyBuilt = !playConstructionAnimationOnStart;
    }

    // Ghost is a building visualization used when player is placing a building. It has different visuals and doesn't have a collider.
    public bool IsGhost { get; private set; }

    public void SetUpAsGhost() {
        IsGhost = true;
        if (boxCollider)
            boxCollider.enabled = false;
        meshRenderer.SetSharedMaterials(sharedGhostMaterials);
        canEverBeSelected = false;
    }

    private IEnumerator ConstructionAnimation() {
        var startScale = meshRenderer.transform.localScale;
        var startLocalPosition = meshRenderer.transform.localPosition;
        var elapsed = 0f;
        var duration = 1f;
        while (elapsed < duration) {
            var t = elapsed / duration;
            var scale = startScale;
            scale.y = t;
            meshRenderer.transform.localScale = scale;
            var newLocalPosition = startLocalPosition;
            newLocalPosition.Scale(new Vector3(1, t, 1));
            meshRenderer.transform.localPosition = newLocalPosition;
            elapsed += Time.deltaTime;
            yield return null;
        }

        meshRenderer.transform.localScale = startScale;
        meshRenderer.transform.localPosition = startLocalPosition;
        isFullyBuilt = true;
        owningPlayer.NotifyBuildingConstructionComplete(this);
    }

    private void Die() {
        World.Destroy(this);
    }

    public Vector3 AttackPosition => meshRenderer.bounds.center;

    public void ReceiveAttackFrom(Unit attacker) {
        var damage = World.DamageStats.GetDamage((Unit)attacker.Prefab, (Building)Prefab);
        Health -= damage;
    }

    public bool ObjectExists => this;

    public int? Cost => cost;
    public float BuildTime => buildTime;

    public bool PrerequisitesSatisfied {
        get {
            if (prerequisites.Count == 0)
                return true;
            foreach (var prerequisite in prerequisites)
                if (owningPlayer.GetBuildingsCountOf(prerequisite.BuildingPrefab) < prerequisite.RequiredCount)
                    return false;
            return true;
        }
    }
}