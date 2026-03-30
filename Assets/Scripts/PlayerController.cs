using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class PlayerController : WorldBehaviour {

    private Player owningPlayer;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerCameraManager playerCameraManagerPrefab;
    private PlayerCameraManager playerCameraManager;

    [NonSerialized] private Vector2? marqueeStart;
    [NonSerialized] private Vector2 marqueeEnd;

    private readonly List<ISelectable> selectedEntities = new();
    private readonly HashSet<ISelectable> oldSelectedEntitiesSet = new();
    private readonly HashSet<ISelectable> selectedEntitiesSet = new();
    private readonly List<Unit> selectedUnits = new();

    private Dictionary<Unit, Vector2> formationPositions = new();

    [SerializeField] private PlayerHUD playerHUDPrefab;
    private PlayerHUD playerHUD;

    [SerializeField] private Unit unitPrefab;

    private LayerMask unitLayerMask;
    private LayerMask buildingLayerMask;
    private LayerMask ignoreRaycastMask;
    private LayerMask defaultLayerMask;
    private LayerMask rayTraceDefaultMask;
    private float? lastLeftMouseButtonClickTime;

    public Camera PlayerCamera => playerCamera;

    public bool enableSelection = true;
    public bool enableUnitOrders = true;
    public bool enableBuildingPlacement = true;

    private Building buildingPrefabToPlace;

    private float buildingPlacementYaw = 0;

    [SerializeField] private List<Building> availableBuildingsToPlace = new();
    private Building buildingGhost;

    [SerializeField] private int spawnedUnitsCount = 0;
    public bool showMovePathCells;

    public IReadOnlyList<Building> AvailableBuildingsToPlace => availableBuildingsToPlace;

    public Vector2? MarqueeStart => marqueeStart;
    public Vector2 MarqueeEnd => marqueeEnd;
    public PlayerHUD PlayerHUD => playerHUD;
    public Player OwningPlayer => owningPlayer;

    public event Action<Building, Building> onBuildingConstructionComplete;
    public event Action<Building, Unit> onUnitConstructionComplete;
    public event Action<Building> onPrimaryBuildingSelected;
    public event Action<RefineryBuilding,float> onGoldAdded;

    private readonly Dictionary<Building, Building> primaryBuildings = new();

    public void Initialize(World world, PlayerController prefab, Player player) {
        base.Initialize(world, prefab);
        owningPlayer = player;
    }

    private void Awake() {
        unitLayerMask = LayerMask.GetMask("Unit");
        buildingLayerMask = LayerMask.GetMask("Building");
        ignoreRaycastMask = LayerMask.GetMask("Ignore Raycast");
        defaultLayerMask = LayerMask.GetMask("Default");
        rayTraceDefaultMask = ~(ignoreRaycastMask | unitLayerMask | buildingLayerMask);

        if (playerCameraManagerPrefab)
            playerCameraManager = World.Spawn(playerCameraManagerPrefab, playerCameraManager => { playerCameraManager.Initialize(this); });
        if (playerHUDPrefab)
            playerHUD = World.Spawn(playerHUDPrefab, World.PlayerHUDContainer, playerHUD => { playerHUD.Initialize(World, playerHUDPrefab, this); });

        UpdatePlayerCameraTransform();

        if (TryTraceRay(new Vector2(Screen.width, Screen.height) / 2, out var hitInfo, ~unitLayerMask & ~ignoreRaycastMask))
            for (var i = 0; i < spawnedUnitsCount; i++)
                World.Spawn(unitPrefab, unit => {
                    unit.Initialize(World, unitPrefab, owningPlayer);
                    unit.transform.position = hitInfo.point;
                });
    }

    private static Unit FindLoudestUnit(IReadOnlyCollection<Unit> units) {
        Unit loudestUnit = null;
        var maxVoicePriority = int.MinValue;
        foreach (var unit in units) {
            if (unit.VoicePriority > maxVoicePriority) {
                maxVoicePriority = unit.VoicePriority;
                loudestUnit = unit;
            }
        }
        return loudestUnit;
    }

    private void Update() {
        var movementInput = Vector2.zero;
        if (Input.GetKey(KeyCode.W))
            movementInput.y += 1;
        if (Input.GetKey(KeyCode.S))
            movementInput.y -= 1;
        if (Input.GetKey(KeyCode.A))
            movementInput.x -= 1;
        if (Input.GetKey(KeyCode.D))
            movementInput.x += 1;
        if (movementInput != Vector2.zero)
            playerCameraManager.AddMovementVector(movementInput.normalized);

        var rotationInput = 0;
        if (Input.GetKey(KeyCode.Q))
            rotationInput += 1;
        if (Input.GetKey(KeyCode.E))
            rotationInput -= 1;
        if (rotationInput != 0)
            playerCameraManager.TryRotateCamera(rotationInput);

        UpdatePlayerCameraTransform();

        if (enableSelection) {
            if (Input.GetMouseButtonDown(MouseButton.left)) {
                marqueeStart = Input.mousePosition;
                marqueeEnd = marqueeStart.Value;

                if (!Input.GetKey(KeyCode.LeftShift))
                    selectedEntities.Clear();

                lastLeftMouseButtonClickTime = Time.time;
            }

            else if (Input.GetMouseButton(MouseButton.left)) {
                marqueeEnd = Input.mousePosition;

                var shouldSelectSingleEntity = Vector2.Distance(marqueeStart.Value, marqueeEnd) < 10;
                var shouldSelectUnitsOfType = shouldSelectSingleEntity && (Time.time - lastLeftMouseButtonClickTime) < 0.3f;

                if (!Input.GetKey(KeyCode.LeftShift))
                    selectedEntities.Clear();

                Unit closestUnit = null;
                Building closestBuilding = null;
                var closestDistanceSquared = float.MaxValue;
                foreach (var selectable in World.Selectables) {
                    var onScreenBounds = PlayerHUD.GetOnScreenBounds(selectable.SelectionBounds, playerCamera);
                    var onScreenCenter = onScreenBounds.center;
                    var marqueeMin = Vector2.Min(marqueeStart.Value, marqueeEnd);
                    var marqueeMax = Vector2.Max(marqueeStart.Value, marqueeEnd);
                    var marqueeRect = Rect.MinMaxRect(marqueeMin.x, marqueeMin.y, marqueeMax.x, marqueeMax.y);

                    if (shouldSelectSingleEntity ? marqueeRect.Overlaps(onScreenBounds) : marqueeRect.Contains(onScreenCenter)) {
                        selectedEntities.Add(selectable);

                        var distanceSquared = Vector2.SqrMagnitude(onScreenCenter - marqueeStart.Value);
                        if (shouldSelectSingleEntity && distanceSquared < closestDistanceSquared) {
                            closestDistanceSquared = distanceSquared;
                            if (selectable is Unit unit2)
                                closestUnit = unit2;
                            else if (selectable is Building building)
                                closestBuilding = building;
                        }
                    }
                }

                if (shouldSelectSingleEntity)
                    selectedEntities.RemoveAll(e => e != (object)closestUnit && e != (object)closestBuilding);

                foreach (var selectable in selectedEntities)
                    if (selectable.ObjectExists && !oldSelectedEntitiesSet.Contains(selectable))
                        selectable.IsSelected = true;

                selectedEntitiesSet.Clear();
                selectedEntitiesSet.UnionWith(selectedEntities);

                foreach (var selectable in oldSelectedEntitiesSet)
                    if (selectable.ObjectExists && !selectedEntitiesSet.Contains(selectable))
                        selectable.IsSelected = false;

                oldSelectedEntitiesSet.Clear();
                oldSelectedEntitiesSet.UnionWith(selectedEntitiesSet);
            }

            else if (Input.GetMouseButtonUp(MouseButton.left)) {
                marqueeStart = null;
                marqueeEnd = Vector2.zero;

                selectedUnits.Clear();
                selectedUnits.AddRange(selectedEntities.OfType<Unit>());
                if (selectedUnits.Count > 0) {
                    var loudestUnit = FindLoudestUnit(selectedUnits);
                    World.AudioSystem.SayRandomVoiceLine(loudestUnit, loudestUnit.OnSelectedVoiceLines);
                }
            }
        }

        if (enableUnitOrders) {
            if (Input.GetMouseButtonDown(MouseButton.right) && selectedEntities.Count > 0) {
                if (TryTraceRay(Input.mousePosition, out var hitInfo, ~ignoreRaycastMask)) {
                    selectedUnits.Clear();
                    selectedUnits.AddRange(selectedEntities.OfType<Unit>());

                    var targetPosition = hitInfo.point.ToVector2();
                    var targetCell = World.Grid.WorldPositionToCell(targetPosition);
                    var targetBuilding = hitInfo.collider.GetComponent<Building>();
                    var targetUnit = hitInfo.collider.GetComponent<Unit>();

                    formationPositions.Clear();
                    foreach (var unit in selectedUnits)
                        formationPositions[unit] = Vector2.zero;
                    UnitFormation.FormAround(targetPosition, formationPositions);

                    UnitFormation.ProjectToWalkable(World.Grid, formationPositions);

                    if (targetUnit && targetBuilding)
                        targetBuilding = null;

                    foreach (var unit in selectedUnits) {
                        if (targetUnit)
                            unit.SetOrder(UnitOrder.Attack(this,targetUnit, formationPositions[unit]));
                        else if (targetBuilding)
                            unit.SetOrder(UnitOrder.Attack(this,targetBuilding, formationPositions[unit]));
                        else if (World.Grid[targetCell].HasGold && unit.GetComponent<HarvesterLogic>())
                            unit.SetOrder(UnitOrder.Harvest(this, formationPositions[unit]));
                        else
                            unit.SetOrder(UnitOrder.Move(this,formationPositions[unit]));
                    }

                    if (selectedUnits.Count > 0) {
                        var isAttackOrder = (bool)targetBuilding;
                        var loudestUnit = FindLoudestUnit(selectedUnits);
                        World.AudioSystem.SayRandomVoiceLine(loudestUnit, isAttackOrder ? loudestUnit.OnAttackOrderVoiceLines : loudestUnit.OnMoveOrderVoiceLines);
                    }
                }
                else
                    foreach (var unit in selectedUnits)
                        unit.CancelOrder();
            }
        }

        if (enableBuildingPlacement) {
            if (buildingPrefabToPlace) {
                buildingPlacementYaw += Input.mouseScrollDelta.y * 5;

                buildingGhost = World.BuildingGhostsSystem.Get(buildingPrefabToPlace);
                if (TryTraceRay(Input.mousePosition, out var hitInfo)) {
                    World.BuildingGhostsSystem.Show(buildingPrefabToPlace);
                    buildingGhost.transform.position = hitInfo.point;
                    buildingGhost.transform.rotation = Quaternion.Euler(0, buildingPlacementYaw, 0);

                    var canBePlaced = CanBePlaced(buildingPrefabToPlace, hitInfo.point, buildingPlacementYaw);
                    buildingGhost.IsValidGhostPlacement = canBePlaced;

                    if (canBePlaced && Input.GetMouseButtonDown(MouseButton.left)) {
                        var building = World.Spawn(buildingPrefabToPlace, building => {
                            building.Initialize(World, buildingPrefabToPlace, owningPlayer);
                            building.transform.position = hitInfo.point;
                            building.transform.rotation = Quaternion.Euler(0, buildingPlacementYaw, 0);
                            building.SetPlayConstructionAnimationOnStart(true);
                        });
                        StopBuildingPlacement();
                    }
                }
                else
                    World.BuildingGhostsSystem.Hide(buildingPrefabToPlace);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            if (TryTraceRay(Input.mousePosition, out var hitInfo))
                World.Spawn(unitPrefab, unit => {
                    unit.Initialize(World, unitPrefab, owningPlayer);
                    unit.transform.position = hitInfo.point;
                });
        }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            World.GameplayStateMachine.Push(World.GameplayStateMachine.Create<InGameMenuGameplayState>());
        }
    }

    public void StartBuildingPlacement(Building building) {
        buildingPrefabToPlace = building;
    }

    public void StopBuildingPlacement() {
        if (buildingGhost) {
            buildingGhost.gameObject.SetActive(false);
            buildingGhost = null;
        }
        buildingPrefabToPlace = null;
    }


    private bool TryTraceRay(Vector2 screenPosition, out RaycastHit hitInfo, LayerMask? layerMask = null) {
        var ray = playerCamera.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out hitInfo, 100, layerMask ?? rayTraceDefaultMask);
    }

    private void UpdatePlayerCameraTransform() {
        if (playerCameraManager) {
            var cameraPosition = Vector3.zero;
            var cameraRotation = Quaternion.identity;
            playerCameraManager.GetView(out cameraPosition, out cameraRotation);
            playerCamera.transform.position = cameraPosition;
            playerCamera.transform.rotation = cameraRotation;
        }
    }

    private bool CanBePlaced(Building building, Vector3 position, float yaw) {
        foreach (var cell in World.Grid.EnumerateIndicesInside(building.BoxCollider.bounds)) {
            if (World.Grid[cell].occupiedBy || World.Grid[cell].reservedBy)
                return false;
        }
        return true;
    }

    public void SetPrimaryBuilding(Building buildingPrefab, Building building) {
        if (primaryBuildings.ContainsKey(buildingPrefab)) {
            primaryBuildings[buildingPrefab] = building;
            onPrimaryBuildingSelected?.Invoke(building);
        }
        else
            primaryBuildings.Add(buildingPrefab, building);
    }

    public void RemovePrimaryBuilding(Building building) {
        if (primaryBuildings.TryGetValue((Building)building.Prefab, out var primaryBuilding) && primaryBuilding == building)
            primaryBuildings.Remove((Building)building.Prefab);
    }

    public void NotifyConstructionComplete(Building factory, Object prefab) {
        var unit = prefab as Unit;
        var building = prefab as Building;
        if (building)
            onBuildingConstructionComplete?.Invoke(factory, building);
        if (unit)
            onUnitConstructionComplete?.Invoke(factory, unit);
    }
    
    public void NotifyGoldAdded(RefineryBuilding refinery, float amountAdded) {
        onGoldAdded?.Invoke(refinery, amountAdded);
    }
}