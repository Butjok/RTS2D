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
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/*
 * This is the main container class for the game.
 *
 * It is used to spawn and despawn 'WorldBehaviour' objects.
 * It also tracks the players, units, buildings and other important objects in the game.
 * It also spawn players on game start.
 */

public class World : MonoBehaviour {

    [Serializable]
    public struct PlayerSpawnInfo {
        [SerializeField] private int id;
        [SerializeField] private Player.Kind kind;
        [SerializeField] private Color color;

        public int Id => id;
        public Player.Kind Kind => kind;
        public Color Color => color;
    }

    [SerializeField] private List<PlayerSpawnInfo> playerSpawnInfos = new();
    private readonly HashSet<int> usedPlayerIds = new();
    [SerializeField] private List<Player> players = new();
    [SerializeField] private PlayerController playerControllerPrefab;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private RectTransform playerHUDContainer;
    [SerializeField] private BoxCollider worldBounds;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private RepathSystem repathSystem;
    [SerializeField] private BuildingGhostsSystem buildingGhostsSystem;
    [SerializeField] private LevelScriptBase levelScript;
    [SerializeField] private AudioSystem audioSystem;
    [SerializeField] private LevelGrid grid;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private DamageStats damageStats;
    [SerializeField] private InGameMenuUI inGameMenuUI;
    [SerializeField] private GameplayStateMachine gameplayStateMachine;

    [SerializeField] private List<Unit> units = new();
    [SerializeField] private List<Building> buildings = new();
    private readonly List<ISelectable> selectables = new();

    public event Action<Object> onObjectSpawned;
    public event Action<Object> onObjectDestroyed;

    public IReadOnlyList<PlayerSpawnInfo> PlayerSpawnInfos => playerSpawnInfos;
    public IReadOnlyList<Player> Players => players;
    public RectTransform PlayerHUDContainer => playerHUDContainer;
    public BoxCollider WorldBounds => worldBounds;
    public Camera WorldCamera => worldCamera;
    public IReadOnlyList<Unit> Units => units;
    public IReadOnlyList<Building> Buildings => buildings;
    public IReadOnlyList<ISelectable> Selectables => selectables;
    public PlayerController PlayerController => playerController;
    public HashSet<Unit> MovingUnitsSet { get; } = new();
    public RepathSystem RepathSystem => repathSystem;
    public BuildingGhostsSystem BuildingGhostsSystem => buildingGhostsSystem;
    public AudioSystem AudioSystem => audioSystem;
    public LevelGrid Grid => grid;
    public DialogueUI DialogueUI => dialogueUI;
    public DamageStats DamageStats => damageStats;
    public InGameMenuUI InGameMenuUI => inGameMenuUI;
    public GameplayStateMachine GameplayStateMachine => gameplayStateMachine;

    private void Awake() {

        grid.Initialize();

        Player humanPlayer = null;
        foreach (var playerSpawnInfo in playerSpawnInfos) {
            Debug.Assert(!usedPlayerIds.Contains(playerSpawnInfo.Id));
            usedPlayerIds.Add(playerSpawnInfo.Id);
            var player = Spawn<Player>(player => {
                player.Initialize(this, null, playerSpawnInfo.Id, playerSpawnInfo.Kind);
                player.Color = playerSpawnInfo.Color;
            });
            players.Add(player);
            if (player.IsHuman) {
                Debug.Assert(!humanPlayer);
                humanPlayer = player;
            }
        }

        var prePlacedInfos = FindObjectsByType<PrePlacedInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var prePlacedInfo in prePlacedInfos) {

            var canBePrePlaced = prePlacedInfo.Target as ICanBePrePlaced;
            Debug.Assert(canBePrePlaced != null, $"PrePlacedInfo {prePlacedInfo.name} has target {prePlacedInfo.Target.name} which doesn't implement ICanBePrePlaced interface.");
            canBePrePlaced.InitializeFromPrePlacedInfo(prePlacedInfo);
            prePlacedInfo.gameObject.SetActive(true);

            if (prePlacedInfo.Target is Building prePlacedBuilding) {
                buildings.Add(prePlacedBuilding);
                grid.SetNotWalkable(prePlacedBuilding);
                prePlacedBuilding.OwningPlayer.IncrementBuildingsCountOf(prePlacedBuilding.GetPrefab<Building>());
            }
            if (prePlacedInfo.Target is ISelectable selectable)
                selectables.Add(selectable);
            if (prePlacedInfo.Target is Unit unit)
                units.Add(unit);
        }

        damageStats.EnsureTablesArePrecached();

        if (humanPlayer && playerControllerPrefab) {
            playerController = Spawn(playerControllerPrefab, playerController => {
                playerController.Initialize(this, playerControllerPrefab, humanPlayer);
                playerController.enabled = false;
                worldCamera = playerController.PlayerCamera;
            });
            humanPlayer.PlayerController = playerController;
        }
    }

    public T Spawn<T>(Action<T> setup = null) where T : WorldBehaviour {
        var go = new GameObject(typeof(T).Name);
        go.SetActive(false);
        var instance = go.AddComponent<T>();
        setup?.Invoke(instance);
        go.SetActive(true);
        onObjectSpawned?.Invoke(instance);
        return instance;
    }

    public T Spawn<T>(T prefab, Action<T> setup = null) where T : WorldBehaviour {
        return Spawn(prefab, null, setup);
    }

    public T Spawn<T>(T prefab, Transform parent, Action<T> setup = null) where T : WorldBehaviour {

        var wasPrefabActive = prefab.gameObject.activeSelf;
        prefab.gameObject.SetActive(false);

        var instance = Instantiate(prefab, parent);
        setup?.Invoke(instance);

        if (instance is Unit unit)
            units.Add(unit);
        if (instance is Building building && !building.IsGhost) {
            buildings.Add(building);
            building.OwningPlayer.IncrementBuildingsCountOf(building.GetPrefab<Building>());
            if (building.OwningPlayer.PlayerController)
                building.OwningPlayer.PlayerController.UpdateConstructionOptions();
        }
        if (instance is ISelectable selectable && selectable.CanEverBeSelected)
            selectables.Add(selectable);

        prefab.gameObject.SetActive(wasPrefabActive);
        instance.gameObject.SetActive(wasPrefabActive);
        onObjectSpawned?.Invoke(instance);
        return instance;
    }

    public void Destroy(WorldBehaviour obj) {

        onObjectDestroyed?.Invoke(obj);

        if (obj is Unit unit)
            units.Remove(unit);
        if (obj is Building building && !building.IsGhost) {
            buildings.Remove(building);
            building.OwningPlayer.DecrementBuildingsCountOf(building.GetPrefab<Building>());
            if (building.OwningPlayer.PlayerController)
                building.OwningPlayer.PlayerController.UpdateConstructionOptions();
        }
        if (obj is ISelectable selectable)
            selectables.Remove(selectable);

        Object.Destroy(obj.gameObject);
    }

    private void OnValidate() {
        var prePlacedInfos = FindObjectsByType<PrePlacedInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var prePlacedInfo in prePlacedInfos)
            prePlacedInfo.UpdateInEditorPlayerColor();
    }
}