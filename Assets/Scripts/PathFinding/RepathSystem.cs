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
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class RepathSystem : WorldBehaviour {

    [SerializeField] private int maxPathFindingQueriesPerFrame = 5;

    private readonly HashSet<Unit> unitsToRepath = new();
    private readonly List<Bounds> dirtyBoundsList = new();

    private IEnumerable<Unit> FindAffectedUnits() {
        foreach (var unit in World.MovingUnitsSet)
        foreach (var bounds in dirtyBoundsList) {
            var affected = false;
            var (minIndex, maxIndex) = World.Grid.GetMinMaxIndices(bounds);
            foreach (var cell in unit.Movement.MovePathCells)
                if (cell.x >= minIndex.x && cell.x <= maxIndex.x &&
                    cell.y >= minIndex.y && cell.y <= maxIndex.y) {
                    affected = true;
                    break;
                }
            if (affected) {
                yield return unit;
                break;
            }
        }
    }

    private void Awake() {
        World.onObjectSpawned += AddDirtyBoundsForBuilding;
    }
    private void OnDestroy() {
        World.onObjectSpawned -= AddDirtyBoundsForBuilding;
    }
    private void AddDirtyBoundsForBuilding(Object obj) {
        if (obj is Building building)
            dirtyBoundsList.Add(building.BoxCollider.bounds);
    }

    private void Update() {
        
        if (dirtyBoundsList.Count > 0) {
            unitsToRepath.UnionWith(FindAffectedUnits());
            dirtyBoundsList.Clear();
        }

        var repathed = 0;
        for (var i = 0; i < maxPathFindingQueriesPerFrame && unitsToRepath.Count > 0; i++) {
            var unit = unitsToRepath.First();
            unitsToRepath.Remove(unit);
            if (unit.CurrentOrder != null) {
                unit.Movement.FindPathToDestination();
                repathed++;
            }
        }
        if (repathed > 0)
            Debug.Log($"Recalculated paths for {repathed} units. {unitsToRepath.Count} units left to recalculate.");
    }

    public void EnqueueUnitForRepath(Unit unit) {
        unitsToRepath.Add(unit);
    }
}