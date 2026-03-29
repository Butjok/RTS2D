using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : WorldBehaviour {

    public enum Kind {
        Player, AI
    }
    
    [SerializeField] private int id = -1;
    [SerializeField] private PlayerController controller;
    [SerializeField] private Color color;
    [SerializeField] private int credits;
    [SerializeField] private Kind kind = Kind.Player;
    [SerializeField] private readonly Dictionary<Building, int> buildingsOfTypeCount = new();
    
    public event Action<Building> onBuildingConstructionComplete;
    public event Action<Building> onUnitConstructionComplete;

    public int Id => id;
    public bool IsAi => kind == Kind.AI;

    public PlayerController Controller {
        get => controller;
        set => controller = value;
    }

    public void Initialize(World world, Player prefab, int id, Kind kind) {
        base.Initialize(world, prefab);
        this.id = id;
        this.kind = kind;
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

    public void IncrementBuildingsCountOf(Building building) {
        buildingsOfTypeCount[building] = buildingsOfTypeCount.TryGetValue(building, out var count) ? count + 1 : 1;
    }

    public void DecrementBuildingsCountOf(Building building) {
        buildingsOfTypeCount[building] = buildingsOfTypeCount.TryGetValue(building, out var count) ? count - 1 : 0;
    }

    public int GetBuildingsCountOf(Building building) {
        return buildingsOfTypeCount.TryGetValue(building, out var count) ? count : 0;
    }
    
    public void NotifyBuildingConstructionComplete(Building building) {
        onBuildingConstructionComplete?.Invoke(building);
    }
    public void NotifyUnitConstructionComplete(Building building) {
        onUnitConstructionComplete?.Invoke(building);
    }
}