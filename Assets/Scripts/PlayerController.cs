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
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class PlayerController : WorldBehaviour {

    private Player player;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerCameraManager playerCameraManagerPrefab;
    private PlayerCameraManager playerCameraManager;

    [NonSerialized] private Vector2? marqueeStart;
    [NonSerialized] private Vector2 marqueeEnd;

    private readonly List<ISelectable> selectedEntities = new();
    private readonly HashSet<ISelectable> oldSelectedEntitiesSet = new();
    private readonly HashSet<ISelectable> selectedEntitiesSet = new();

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
    private Building buildingGhost;

    [SerializeField] private int spawnedUnitsCount = 0;
    public bool showMovePathCells;

    public Vector2? MarqueeStart => marqueeStart;
    public Vector2 MarqueeEnd => marqueeEnd;
    public PlayerHUD PlayerHUD => playerHUD;
    public Player Player => player;

    [SerializeField] private AudioClip announcer_battleControlActivated;
    [SerializeField] private AudioClip announcer_battleControlTerminated;
    [SerializeField] private AudioClip announcer_building;
    [SerializeField] private AudioClip announcer_constructionComplete;
    [SerializeField] private AudioClip announcer_incomingTransmission;
    [SerializeField] private AudioClip announcer_insufficientFunds;
    [SerializeField] private AudioClip announcer_missionCompleted;
    [SerializeField] private AudioClip announcer_newConstructionOptions;
    [SerializeField] private AudioClip announcer_newObjective;
    [SerializeField] private AudioClip announcer_unitLost;
    [SerializeField] private AudioClip announcer_unitReady;
    [SerializeField] private AudioClip announcer_primaryBuildingSelected;
    [SerializeField] private AudioClip announcer_onHold;
    [SerializeField] private AudioClip announcer_cancelled;
    [SerializeField] private AudioClip announcer_reinforcementsHaveArrived;
    [SerializeField] private AudioClip announcer_lowPower;

    private readonly HashSet<ConstructionOption> constructionOptions = new();
    private readonly HashSet<ConstructionOption> oldConstructionOptions = new();

    public IReadOnlyList<ISelectable> SelectedEntities => selectedEntities;

    public void Initialize(World world, PlayerController prefab, Player player) {
        base.Initialize(world, prefab);
        this.player = player;
    }

    public IEnumerable<ConstructionOption> EnumerateConstructionOptions() {
        foreach (var buildingType in player.EnumerateBuildingTypes()) {
            foreach (var constructionOption in buildingType.ConstructionOptions)
                yield return constructionOption;
        }
    }

    private void Awake() {

        unitLayerMask = LayerMask.GetMask("Unit");
        buildingLayerMask = LayerMask.GetMask("Building");
        ignoreRaycastMask = LayerMask.GetMask("Ignore Raycast");
        defaultLayerMask = LayerMask.GetMask("Default");
        rayTraceDefaultMask = ~(ignoreRaycastMask | unitLayerMask | buildingLayerMask);

        if (playerCameraManagerPrefab)
            playerCameraManager = World.Spawn(playerCameraManagerPrefab, playerCameraManager => {
                playerCameraManager.Initialize(World, playerCameraManagerPrefab, this);
            });

        if (playerHUDPrefab) {
            playerHUD = World.Spawn(playerHUDPrefab, World.PlayerHUDContainer, playerHUD => {
                playerHUD.Initialize(World, playerHUDPrefab, this);
            });
            playerHUD.RespawnBuildIcons(EnumerateConstructionOptions());
        }

        UpdatePlayerCameraTransform();

        if (TryTraceRay(new Vector2(Screen.width, Screen.height) / 2, out var hitInfo, ~unitLayerMask & ~ignoreRaycastMask))
            for (var i = 0; i < spawnedUnitsCount; i++)
                World.Spawn(unitPrefab, unit => {
                    unit.Initialize(World, unitPrefab, player);
                    unit.transform.position = hitInfo.point;
                });
        
        oldConstructionOptions.UnionWith(EnumerateConstructionOptions());

        World.onObjectSpawned += UpdateConstructionOptions;
        World.onObjectDestroyed += UpdateConstructionOptions;
    }

    private void OnDestroy() {
        World.onObjectSpawned -= UpdateConstructionOptions;
        World.onObjectDestroyed += UpdateConstructionOptions;
    }

    private void UpdateConstructionOptions(Object obj) {
        if (obj is Building building && !building.IsGhost && building.OwningPlayer == player) {
            constructionOptions.Clear();
            constructionOptions.UnionWith(EnumerateConstructionOptions());
            constructionOptions.ExceptWith(oldConstructionOptions);
            if (constructionOptions.Count > 0)
                NotifyNewConstructionOptions();
            oldConstructionOptions.Clear();
            oldConstructionOptions.UnionWith(EnumerateConstructionOptions());
        }
    }

    private Unit FindLoudestUnitWhichCanReceiveOrder(IEnumerable<Unit> units) {
        Unit loudestUnit = null;
        var maxVoicePriority = int.MinValue;
        foreach (var unit in units) {
            if (unit.CanReceiveOrderFrom(this) && unit.VoicePriority > maxVoicePriority) {
                maxVoicePriority = unit.VoicePriority;
                loudestUnit = unit;
            }
        }
        return loudestUnit;
    }

    private void UpdateSelectionFlags() {
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

                UpdateSelectionFlags();
            }

            else if (Input.GetMouseButtonUp(MouseButton.left)) {
                marqueeStart = null;
                marqueeEnd = Vector2.zero;

                if (selectedEntities.OfType<Unit>().Any()) {
                    var loudestUnit = FindLoudestUnitWhichCanReceiveOrder(selectedEntities.OfType<Unit>());
                    if (loudestUnit)
                        World.AudioSystem.SayRandomVoiceLine(loudestUnit, loudestUnit.OnSelectedVoiceLines);
                }
            }
        }

        if (enableUnitOrders) {
            if (Input.GetMouseButtonDown(MouseButton.right) && selectedEntities.Count > 0) {
                if (TryTraceRay(Input.mousePosition, out var hitInfo, ~ignoreRaycastMask)) {

                    var targetPosition = hitInfo.point.ToVector2();
                    var targetBuilding = hitInfo.collider.GetComponent<Building>();
                    var targetRefinery = targetBuilding ? targetBuilding.GetComponent<RefineryBuilding>() : null;
                    var targetUnit = hitInfo.collider.GetComponent<Unit>();

                    formationPositions.Clear();
                    foreach (var unit in selectedEntities.OfType<Unit>())
                        formationPositions[unit] = Vector2.zero;
                    UnitFormation.FormAround(targetPosition, formationPositions);

                    UnitFormation.ProjectToWalkable(World.Grid, formationPositions);

                    if (targetUnit && targetBuilding)
                        targetBuilding = null;

                    selectedEntities.RemoveAll(unit => !unit.CanReceiveOrderFrom(this));
                    UpdateSelectionFlags();

                    foreach (var unit in selectedEntities.OfType<Unit>()) {

                        var harvesterLogic = unit.GetComponent<HarvesterLogic>();
                        var destination = formationPositions[unit];
                        var destinationCell = World.Grid.WorldPositionToCell(destination);

                        if (targetUnit && unit.CanAttack(targetUnit))
                            unit.SetOrder(UnitOrder.Attack(this, targetUnit, destination));
                        else if (targetBuilding && unit.CanAttack(targetBuilding))
                            unit.SetOrder(UnitOrder.Attack(this, targetBuilding, destination));
                        else if (harvesterLogic) {
                            if (World.Grid[destinationCell].HasGold)
                                unit.SetOrder(UnitOrder.Harvest(this, destination));
                            else if (targetRefinery && targetRefinery.OwningPlayer == unit.OwningPlayer) {
                                harvesterLogic.HomeBase = targetRefinery;
                                unit.SetOrder(UnitOrder.Unload(this, targetRefinery));
                            }
                        }
                        else
                            unit.SetOrder(UnitOrder.Move(this, destination));
                    }

                    if (selectedEntities.OfType<Unit>().Any()) {
                        var isAttackOrder = (bool)targetBuilding;
                        var loudestUnit = FindLoudestUnitWhichCanReceiveOrder(selectedEntities.OfType<Unit>());
                        if (loudestUnit)
                            World.AudioSystem.SayRandomVoiceLine(loudestUnit, isAttackOrder ? loudestUnit.OnAttackOrderVoiceLines : loudestUnit.OnMoveOrderVoiceLines);
                    }
                }
                else
                    foreach (var unit in selectedEntities.OfType<Unit>())
                        unit.CancelOrder();
            }
        }

        if (enableBuildingPlacement) {
            if (buildingPrefabToPlace) {

                buildingGhost = World.BuildingGhostsSystem.Get(buildingPrefabToPlace);
                if (TryTraceRay(Input.mousePosition, out var hitInfo)) {
                    World.BuildingGhostsSystem.Show(buildingPrefabToPlace);
                    buildingGhost.transform.position = hitInfo.point;

                    var canBePlaced = CanBePlaced(buildingPrefabToPlace, hitInfo.point);
                    buildingGhost.IsValidGhostPlacement = canBePlaced;

                    if (canBePlaced && Input.GetMouseButtonDown(MouseButton.left)) {
                        var buildingsCountOfThisType = player.GetBuildingsCountOf(buildingPrefabToPlace);
                        World.Spawn(buildingPrefabToPlace, building => {
                            building.Initialize(World, buildingPrefabToPlace, player, buildingsCountOfThisType == 0, false);
                            building.transform.position = hitInfo.point;
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
                    unit.Initialize(World, unitPrefab, player);
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
            playerCameraManager.GetView(out var cameraPosition, out var cameraRotation);
            playerCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        }
    }

    // TODO: does not work currently
    private bool CanBePlaced(Building building, Vector3 position) {
        foreach (var cell in World.Grid.EnumerateIndicesInside(building.BoxCollider.bounds)) {
            if (World.Grid[cell].occupiedBy || World.Grid[cell].reservedBy)
                return false;
        }
        return true;
    }

    public void NotifyStartBuilding(ConstructionQueueItem item) {
        World.AudioSystem.SayAnnouncerVoiceLine(announcer_building);
    }

    public void NotifyConstructionComplete(Building factory, Object prefab) {
        var unit = prefab as Unit;
        var building = prefab as Building;
        if (building) {
            World.AudioSystem.SayAnnouncerVoiceLine(announcer_constructionComplete);
        }
        if (unit)
            World.AudioSystem.SayAnnouncerVoiceLine(announcer_unitReady);
    }

    public void NotifyGoldAdded(RefineryBuilding refinery, float amountAdded) {
        //World.AudioSystem.SayAnnouncerVoiceLine(announcer_incomingTransmission);
    }

    public void NotifyPrimaryBuildingSelected(Building primaryBuilding) {
        World.AudioSystem.SayAnnouncerVoiceLine(announcer_primaryBuildingSelected);
    }

    public void NotifyNewConstructionOptions() {
        World.AudioSystem.SayAnnouncerVoiceLine(announcer_newConstructionOptions);
    }

    public void NotifyBattleControlActivated() {
        World.AudioSystem.SayAnnouncerVoiceLine(announcer_battleControlActivated);
    }

    public void NotifyBattleControlTerminated() {
        World.AudioSystem.SayAnnouncerVoiceLine(announcer_battleControlTerminated);
    }

    public void NotifyPutOnHold(ConstructionQueueItem item) {
        World.AudioSystem.SayAnnouncerVoiceLine(announcer_onHold);
    }
    public void NotifyCancelled(ConstructionQueueItem item) {
        World.AudioSystem.SayAnnouncerVoiceLine(announcer_cancelled);
    }
    public void NotifyResumeBuilding(ConstructionQueueItem constructionQueueItem) {
        World.AudioSystem.SayAnnouncerVoiceLine(announcer_building);
    }
    public void NotifyReinforcementsHaveArrived() {
        World.AudioSystem.SayAnnouncerVoiceLine(announcer_reinforcementsHaveArrived);
    }
    public void NotifyLowPower() {
        World.AudioSystem.SayAnnouncerVoiceLine(announcer_lowPower);
    }
}