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
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(ConstructionQueue))]
public class Building : WorldBehaviour, ISelectable, IHasHealth, IAttackTarget, ICanBePrePlaced {


    private static readonly int shader_playerColor = Shader.PropertyToID("_PlayerColor");
    private static readonly int shader_baseColor = Shader.PropertyToID("_Color");

    [SerializeField] private Player owningPlayer;
    [SerializeField] private List<Renderer> renderers = new();
    [SerializeField] private List<Material> sharedGhostMaterials = new();

    [SerializeField] private BoxCollider boxCollider;

    [SerializeField] private bool isFullyBuilt = true;
    [SerializeField] private bool canEverBeSelected = true;
    [SerializeField] private List<ConstructionOption> constructionOptions = new();

    [SerializeField] private ConstructionQueue constructionQueue;
    private float health = 1;
    private float? lastDamageTime;

    private bool playConstructionAnimationOnStart;
    private IEnumerator constructionAnimationCoroutine;

    private MaterialPropertyBlock materialPropertyBlock;
    private Color? playerColor;

    public BoxCollider BoxCollider => boxCollider;
    public ConstructionQueue ConstructionQueue => constructionQueue;

    public IReadOnlyList<ConstructionOption> ConstructionOptions {
        get {
#if UNITY_EDITOR
            Debug.Assert(PrefabUtility.IsPartOfPrefabAsset(this), $"Building {name} should be a prefab asset to access construction options. If you want to access construction options of an instance, consider making construction options static or accessing them through the prefab.");
#endif
            return constructionOptions;
        }
    }

    public void Initialize(World world, Building prefab, Player owningPlayer, bool isPrimaryBuilding, bool isGhost) {
        base.Initialize(world, prefab);
        OwningPlayer = owningPlayer;
        if (isPrimaryBuilding)
            OwningPlayer.SetPrimaryBuilding(this);
        if (isGhost) {
            IsGhost = true;
            if (boxCollider)
                boxCollider.enabled = false;
            foreach (var renderer in renderers)
                renderer.SetSharedMaterials(sharedGhostMaterials);
            canEverBeSelected = false;
        }
    }

    public void InitializeFromPrePlacedInfo(PrePlacedInfo info) {
        Initialize(info.World, (Building)info.Prefab, info.Player, info.IsPrimaryBuilding, false);
    }

    private void Awake() {
        if (!IsGhost && owningPlayer.GetPrimaryBuilding(GetPrefab<Building>()) == null)
            owningPlayer.SetPrimaryBuilding(this);
    }

    private void OnDestroy() {
        if (!IsGhost && owningPlayer)
            owningPlayer.RemovePrimaryBuilding(this);
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

    private void OnValidate() {
        if (!constructionQueue)
            constructionQueue = GetComponent<ConstructionQueue>();
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

    public bool CanBeSelectedBy(PlayerController playerController) {
        return playerController && playerController.Player == owningPlayer;
    }

    public bool IsSelected { get; set; } = false;
    public bool CanEverBeSelected => canEverBeSelected;

    public void SetPlayConstructionAnimationOnStart(bool value) {
        playConstructionAnimationOnStart = value;
        isFullyBuilt = !playConstructionAnimationOnStart;
    }

    // Ghost is a building visualization used when player is placing a building. It has different visuals and doesn't have a collider.
    public bool IsGhost { get; private set; }

    protected virtual void OnBuilt() { }

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
        OnBuilt();
    }

    private void Die() {
        World.Destroy(this);
    }

    public Vector3 PositionToBeAttackedAt => renderers[0].bounds.center;

    public void ReceiveAttackFrom(Unit attackerType) {
        lastDamageTime = Time.time;
        var damage = World.DamageStats.GetDamage(attackerType, GetPrefab<Building>());
        Health -= damage;
    }

    public bool ObjectExists => this;

    public float? LastDamageTime => lastDamageTime;

    public bool CanReceiveOrderFrom(PlayerController playerController) {
        return playerController && playerController.Player == owningPlayer;
    }
}