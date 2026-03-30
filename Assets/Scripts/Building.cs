using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

public class Building : WorldBehaviour, ISelectable, IHasHealth, IAttackTarget, IBuildable, ICanBePrePlaced {
    [Serializable]
    public struct BuildingPrerequisite {
        [SerializeField] private Building buildingPrefab;
        [SerializeField] private int requiredCount;

        public Building BuildingPrefab => buildingPrefab;
        public int RequiredCount => requiredCount;
    }

    [Serializable]
    public class BuildingQueueItem {
        [SerializeField] private Object prefab;
        [SerializeField] private float timeElapsed;

        public Object Prefab => prefab;

        public float TimeElapsed {
            get => timeElapsed;
            set => timeElapsed = value;
        }

        public float BuildTime => ((IBuildable)prefab).BuildTime;

        public float Progress => Mathf.Clamp01(timeElapsed / BuildTime);

        public BuildingQueueItem(IBuildable buildablePrefab) {
            prefab = (Object)buildablePrefab;
        }
    }

    private static readonly int shader_playerColor = Shader.PropertyToID("_PlayerColor");
    private static readonly int shader_baseColor = Shader.PropertyToID("_Color");

    [SerializeField] private Player owningPlayer;
    [SerializeField] private List<Renderer> renderers = new();
    [SerializeField] private List<Unit> buildableUnitPrefabs = new();
    [SerializeField] private List<Material> sharedGhostMaterials = new();

    [SerializeField] private BoxCollider boxCollider;

    [SerializeField] private bool isFullyBuilt = true;
    [SerializeField] private bool canEverBeSelected = true;
    [SerializeField] private int cost = 1000;
    [SerializeField] private float buildTime = 60;
    [SerializeField] private List<BuildingPrerequisite> prerequisites = new();
    private readonly Queue<BuildingQueueItem> buildingQueue = new();

    private float health = 1;
    private float? lastDamageTime;

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
            foreach (var renderer in renderers)
                renderer.SetPropertyBlock(materialPropertyBlock);
        }
    }

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

    public Bounds SelectionBounds => renderers[0].bounds;

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
        foreach (var renderer in renderers)
            renderer.SetSharedMaterials(sharedGhostMaterials);
        canEverBeSelected = false;
    }

    private IEnumerator ConstructionAnimation() {
        var startScale = renderers[0].transform.localScale;
        var startLocalPosition = renderers[0].transform.localPosition;
        var elapsed = 0f;
        var duration = 1f;
        while (elapsed < duration) {
            var t = elapsed / duration;
            var scale = startScale;
            scale.y = t;
            renderers[0].transform.localScale = scale;
            var newLocalPosition = startLocalPosition;
            newLocalPosition.Scale(new Vector3(1, t, 1));
            renderers[0].transform.localPosition = newLocalPosition;
            elapsed += Time.deltaTime;
            yield return null;
        }

        renderers[0].transform.localScale = startScale;
        renderers[0].transform.localPosition = startLocalPosition;
        isFullyBuilt = true;
    }

    private void Die() {
        World.Destroy(this);
    }

    public Vector3 PositionToBeAttackedAt => renderers[0].bounds.center;

    public void ReceiveAttackFrom(Unit attacker) {
        lastDamageTime = Time.time;
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

    public BuildingQueueItem StartBuilding(IBuildable buildable) {
        //Debug.Assert(buildableUnitPrefabs.Contains(buildable));
        var itemInConstruction = new BuildingQueueItem(buildable);
        buildingQueue.Enqueue(itemInConstruction);
        World.AudioSystem.SayAnnouncerVoiceLine(World.AudioSystem.AnnouncerBuilding);
        return itemInConstruction;
    }

    private void Update() {
        if (buildingQueue.TryPeek(out var item)) {
            item.TimeElapsed += Time.deltaTime;
            if (item.TimeElapsed >= item.BuildTime) {
                buildingQueue.Dequeue();
                if (owningPlayer.PlayerController)
                    owningPlayer.PlayerController.NotifyConstructionComplete(this, item.Prefab);
            }
        }
    }

    private void OnDestroy() {
        if (owningPlayer && owningPlayer.PlayerController)
            owningPlayer.PlayerController.RemovePrimaryBuilding(this);
    }

    public float? LastDamageTime => lastDamageTime;
}