using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

public class World : MonoBehaviour {
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
    [SerializeField] private Grid grid;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private DamageStats damageStats;
    [SerializeField] private InGameMenuUI inGameMenuUI;
    [SerializeField] private GameplayStateMachine gameplayStateMachine;

    [SerializeField] private List<Unit> units = new();

    [SerializeField] private List<Building> buildings = new();
    private readonly HashSet<Building> buildingsSet = new(); // is used for quick lookup in Contains(Building) method

    private readonly List<ISelectable> selectables = new();
    private IEnumerator animationCoroutine;

    public event Action<Object> onObjectSpawned;
    public event Action<Object> onObjectDestroyed;

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
    public Grid Grid => grid;
    public DialogueUI DialogueUI => dialogueUI;
    public DamageStats DamageStats => damageStats;
    public InGameMenuUI InGameMenuUI => inGameMenuUI;
    public GameplayStateMachine GameplayStateMachine => gameplayStateMachine;

    private void Awake() {
        
        grid.Initialize();

        var redPlayer = Spawn<Player>(player => {
            player.Initialize(this, null, 0, Player.Kind.Player);
            player.Color = Color.red;
        });
        players.Add(redPlayer);

        if (playerControllerPrefab) {
            playerController = Spawn(playerControllerPrefab, playerController => {
                playerController.Initialize(this, playerControllerPrefab, redPlayer);
                playerController.enabled = false;
                worldCamera = playerController.PlayerCamera;
            });
            redPlayer.Controller = playerController;
        }
        
        var bluePlayer = Spawn<Player>(player => {
            player.Initialize(this, null, 1, Player.Kind.AI);
            player.Color = Color.blue;
        });
        players.Add(bluePlayer);

        var prePlacedInfos = FindObjectsByType<PrePlacedInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var prePlacedInfo in prePlacedInfos) {

            var canBePrePlaced = prePlacedInfo.Target as ICanBePrePlaced;
            Debug.Assert(canBePrePlaced != null, $"PrePlacedInfo {prePlacedInfo.name} has target {prePlacedInfo.Target.name} which doesn't implement ICanBePrePlaced interface.");
            canBePrePlaced.InitializeFromPrePlacedInfo(prePlacedInfo);
            prePlacedInfo.gameObject.SetActive(true);

            if (prePlacedInfo.Target is Building prePlacedBuilding) {
                buildings.Add(prePlacedBuilding);
                buildingsSet.Add(prePlacedBuilding);
                grid.SetNotWalkable(prePlacedBuilding);
            }
            if (prePlacedInfo.Target is ISelectable selectable)
                selectables.Add(selectable);
            if (prePlacedInfo.Target is Unit unit)
                units.Add(unit);
        }
        
        damageStats.EnsureTablesArePrecached();
    }

    public T Spawn<T>(Action<T> setup = null) where T : WorldBehaviour {
        var instance = new GameObject(typeof(T).Name).AddComponent<T>();
        setup?.Invoke(instance);
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
            buildingsSet.Add(building);
            building.OwningPlayer.IncrementBuildingsCountOf(prefab as Building);
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

        Player buildingOwner = null;

        if (obj is Unit unit)
            units.Remove(unit);
        if (obj is Building building) {
            buildings.Remove(building);
            buildingsSet.Remove(building);
            building.OwningPlayer.DecrementBuildingsCountOf(building);
            buildingOwner = building.OwningPlayer;
        }

        if (obj is ISelectable selectable)
            selectables.Remove(selectable);

        Object.Destroy(obj.gameObject);

        if (buildingOwner) {
            // check if player has any building left
            var hasAnyBuildingLeft = false;
            foreach (var building2 in buildings)
                if (building2.OwningPlayer == buildingOwner) {
                    hasAnyBuildingLeft = true;
                    break;
                }

            if (!hasAnyBuildingLeft) {
                playerController.enabled = false;

                var isVictory = buildingOwner.IsAi;
                var animationCoroutine = MatchEndAnimationCoroutine(isVictory);
                StartCoroutine(animationCoroutine);
            }
        }
    }

    public bool Contains(Building building) {
        return buildingsSet.Contains(building);
    }

    private IEnumerator MatchEndAnimationCoroutine(bool isVictory) {
        yield return levelScript.MatchEndAnimation(isVictory);
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    private void Update() {
        if (animationCoroutine != null) {
            if (!animationCoroutine.MoveNext())
                animationCoroutine = null;
        }
        if (animationCoroutine == null)
            animationCoroutine = levelScript.RequestAnimation();
    }
}

public abstract class WorldBehaviour : MonoBehaviour {

    [SerializeField] private WorldBehaviour prefab;
    [SerializeField] private World world;

    public World World => world;
    public WorldBehaviour Prefab => prefab;

    private bool wasInitialized;
    protected void Initialize(World world, WorldBehaviour prefab = null) {
        Debug.Assert(!wasInitialized, $"WorldBehaviour {name} was already initialized.");
        wasInitialized = true;

        this.world = world;
        this.prefab = prefab;
    }
}