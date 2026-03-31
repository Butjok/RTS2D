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

using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Player : WorldBehaviour {

    public enum Kind {
        Human,
        AI
    }

    [SerializeField] private int id = -1;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Color color;
    [SerializeField] private float gold;
    [SerializeField] private Kind kind = Kind.Human;

    private readonly Dictionary<Building, int> buildingsOfTypeCount = new();
    private readonly Dictionary<Building, Building> primaryBuildings = new();

    public int Id => id;
    public bool IsHuman => kind == Kind.Human;
    public bool IsAi => kind == Kind.AI;

    public PlayerController PlayerController {
        get => playerController;
        set => playerController = value;
    }

    public void Initialize(World world, Player prefab, int id, Kind kind) {
        base.Initialize(world, prefab);
        this.id = id;
        this.kind = kind;
    }

    private void Awake() {
        World.onObjectSpawned += OnBuildingSpawned;
        World.onObjectDestroyed += OnBuildingDestroyed;
    }

    private void OnDestroy() {
        World.onObjectSpawned += OnBuildingSpawned;
        World.onObjectDestroyed += OnBuildingDestroyed;
    }

    private void OnBuildingSpawned(Object obj) {
        if (obj is Building building && building.OwningPlayer == this && !building.IsGhost)
            IncrementBuildingsCountOf(building.GetPrefab<Building>());
    }

    private void OnBuildingDestroyed(Object obj) {
        if (obj is Building building && building.OwningPlayer == this && !building.IsGhost)
            DecrementBuildingsCountOf(building.GetPrefab<Building>());
    }

    public Color Color {
        get => color;
        set {
            color = value;
            foreach (var unit in World.Units)
                if (unit.OwningPlayer == this)
                    unit.PlayerColor = color;
            foreach (var building in World.Buildings)
                if (building.OwningPlayer == this)
                    building.PlayerColor = color;
        }
    }

    public void IncrementBuildingsCountOf(Building buildingType) {
#if UNITY_EDITOR
        Debug.Assert(PrefabUtility.IsPartOfPrefabAsset(buildingType), $"Building type {buildingType.name} should be a prefab asset.");
#endif
        buildingsOfTypeCount[buildingType] = buildingsOfTypeCount.TryGetValue(buildingType, out var count) ? count + 1 : 1;
    }

    public void DecrementBuildingsCountOf(Building buildingType) {
#if UNITY_EDITOR
        Debug.Assert(PrefabUtility.IsPartOfPrefabAsset(buildingType), $"Building type {buildingType.name} should be a prefab asset.");
#endif
        buildingsOfTypeCount[buildingType] = buildingsOfTypeCount.TryGetValue(buildingType, out var count) ? count - 1 : 0;
    }

    public int GetBuildingsCountOf(Building buildingType) {
#if UNITY_EDITOR
        Debug.Assert(PrefabUtility.IsPartOfPrefabAsset(buildingType), $"Building type {buildingType.name} should be a prefab asset.");
#endif
        return buildingsOfTypeCount.TryGetValue(buildingType, out var count) ? count : 0;
    }

    public IEnumerable<Building> EnumerateBuildingTypes() {
        foreach (var (type, count) in buildingsOfTypeCount)
            if (count > 0)
                yield return type;
    }

    public void AddGold(RefineryBuilding refinery, float amount) {
        gold += amount;
        if (playerController)
            playerController.NotifyGoldAdded(refinery, amount);
    }

    public void SetPrimaryBuilding(Building building) {
        if (primaryBuildings.ContainsKey(building.GetPrefab<Building>())) {
            primaryBuildings[building.GetPrefab<Building>()] = building;
            if (playerController)
                playerController.NotifyPrimaryBuildingSelected(building);
        }
        else
            primaryBuildings.Add(building.GetPrefab<Building>(), building);
    }

    public void RemovePrimaryBuilding(Building building) {
        if (primaryBuildings.TryGetValue(building.GetPrefab<Building>(), out var primaryBuilding) && primaryBuilding == building)
            primaryBuildings.Remove(building.GetPrefab<Building>());
    }

    public Building GetPrimaryBuilding(Building buildingType) {
        return primaryBuildings.TryGetValue(buildingType, out var primaryBuilding) ? primaryBuilding : null;
    }
}