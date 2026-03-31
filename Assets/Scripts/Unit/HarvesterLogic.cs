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

using System.Collections;
using UnityEngine;

public class HarvesterLogic : MonoBehaviour {

    [SerializeField] private Unit unit;
    [SerializeField] private RefineryBuilding homeBase;
    [SerializeField] private float loadedAmount;
    [SerializeField] private float harvestingSpeed = .2f;
    [SerializeField] private float unloadingSpeed = 1;
    [SerializeField] private Transform harvesterScoop;

    private IEnumerator harvestingAnimationCoroutine;
    private IEnumerator unloadingAnimationCoroutine;

    private void Update() {

        if (!unit.CurrentOrder) {
            if (loadedAmount < 1) {
                // find closest cell with gold (closest in terms of spiral order, not actual distance, for performance reasons)
                var foundGold = false;
                foreach (var offset in UnitFormation.EnumerateInSpiral(50)) {
                    var cell = unit.Movement.Cell + offset;
                    if (!unit.World.Grid.InBounds(cell) || !unit.World.Grid[cell].HasGold)
                        continue;
                    if (!unit.World.Grid[cell].isWalkable) {
                        Debug.LogWarning($"Gold at position {unit.World.Grid.CellToWorldPosition(cell)} is not walkable.");
                        continue;
                    }
                    unit.SetOrder(UnitOrder.Harvest(this, unit.World.Grid.CellToWorldPosition(cell)));
                    foundGold = true;
                    break;
                }
                if (!foundGold && loadedAmount > 0)
                    unit.SetOrder(UnitOrder.Unload(this, homeBase));
            }
            else if (homeBase)
                unit.SetOrder(UnitOrder.Unload(this, homeBase));
        }

        else if (unit.CurrentOrder.OrderKind == UnitOrder.Kind.Unload &&
                 unit.Movement.Cell == homeBase.GoldDepositionCell) {

            if (unloadingAnimationCoroutine == null) {
                unloadingAnimationCoroutine = UnloadingAnimation();
                StartCoroutine(unloadingAnimationCoroutine);
            }
        }

        else if (loadedAmount >= 1) {
            if (unit.CurrentOrder.OrderKind != UnitOrder.Kind.Unload && unit.CurrentOrder.Source == this && homeBase)
                unit.SetOrder(UnitOrder.Unload(this, homeBase));
        }

        else if (
            unit.CurrentOrder.OrderKind == UnitOrder.Kind.Harvest &&
            unit.Movement.Cell == unit.World.Grid.WorldPositionToCell(unit.CurrentOrder.MoveDestination)) {

            if (unit.World.Grid[unit.Movement.Cell].HasGold) {
                if (harvestingAnimationCoroutine == null) {
                    harvestingAnimationCoroutine = HarvestingAnimation();
                    StartCoroutine(harvestingAnimationCoroutine);
                }
            }
            else
                unit.CancelOrder();
        }
    }

    private float ScoopAngle {
        set => harvesterScoop.localRotation = Quaternion.Euler(value, 0, 0);
    }

    private IEnumerator HarvestingAnimation() {
        Debug.Assert(unit.World.Grid[unit.Movement.Cell].HasGold);

        var goldInCell = unit.World.Grid[unit.Movement.Cell].goldAmount;
        var elapsed = 0f;
        while (goldInCell > 0 && loadedAmount < 1) {
            var maxTransferredThisFrame = Time.deltaTime * harvestingSpeed;
            var transferredThisFrame = Mathf.Min(maxTransferredThisFrame, goldInCell, 1 - loadedAmount);
            loadedAmount = Mathf.Min(loadedAmount + transferredThisFrame, 1);
            goldInCell = Mathf.Max(goldInCell - transferredThisFrame, 0);
            unit.World.Grid[unit.Movement.Cell].goldAmount = goldInCell;

            elapsed += Time.deltaTime;
            ScoopAngle = Mathf.PingPong(elapsed * 360, 90);

            yield return null;
        }

        harvestingAnimationCoroutine = null;
        unit.CancelOrder();
    }

    private IEnumerator UnloadingAnimation() {
        Debug.Assert(unit.Movement.Cell == homeBase.GoldDepositionCell);

        while (loadedAmount > 0) {
            var maxTransferredThisFrame = Time.deltaTime * unloadingSpeed;
            var transferredThisFrame = Mathf.Min(maxTransferredThisFrame, loadedAmount);

            loadedAmount = Mathf.Max(loadedAmount - transferredThisFrame, 0);
            homeBase.AddGold(transferredThisFrame);

            yield return null;
        }

        unloadingAnimationCoroutine = null;
        unit.CancelOrder();
    }
}