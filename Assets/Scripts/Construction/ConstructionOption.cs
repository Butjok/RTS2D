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

[Serializable]
public class ConstructionOption {

    [Serializable]
    public struct Prerequisite {
        [SerializeField] private Building buildingType;
        [SerializeField] private int requiredCount;

        public Building BuildingType => buildingType;
        public int RequiredCount => requiredCount;
    }

    [SerializeField] private Building sourceBuildingType; // which building type (prefab) is the source of this construction option
    [SerializeField] private Unit unitPrefab;
    [SerializeField] private Building buildingPrefab;
    [SerializeField] private float cost;
    [SerializeField] private float buildTime;
    [SerializeField] private List<Prerequisite> prerequisites = new();

    public Object Prefab => unitPrefab ? unitPrefab : buildingPrefab;
    public float Cost => cost;
    public float BuildTime => buildTime;
    public IReadOnlyList<Prerequisite> Prerequisites => prerequisites;
    public Building SourceBuildingType => sourceBuildingType;

    public bool MeetsPrerequisites(Player player) {
        foreach (var prerequisite in prerequisites) {
            var count = player.GetBuildingsCountOf(prerequisite.BuildingType);
            if (count < prerequisite.RequiredCount)
                return false;
        }
        return true;
    }
}