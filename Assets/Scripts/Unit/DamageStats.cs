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
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = nameof(DamageStats), menuName = nameof(DamageStats))]
public class DamageStats : ScriptableObject {

    [Serializable]
    private struct UnitUnitPair {
        [SerializeField] private Unit attackerPrefab;
        [SerializeField] private Unit targetPrefab;
        [SerializeField] private float damage;

        public Unit AttackerPrefab => attackerPrefab;
        public Unit TargetPrefab => targetPrefab;
        public float Damage => damage;
    }

    [Serializable]
    private struct UnitBuildingPair {
        [SerializeField] private Unit attackerPrefab;
        [SerializeField] private Building targetPrefab;
        [SerializeField] private float damage;

        public Unit AttackerPrefab => attackerPrefab;
        public Building TargetPrefab => targetPrefab;
        public float Damage => damage;
    }

    [SerializeField] private List<UnitUnitPair> unitUnitPairs = new();
    [SerializeField] private List<UnitBuildingPair> unitBuildingPairs = new();
    private Dictionary<(Unit, Unit), float> unitUnitDamageDict = new();
    private Dictionary<(Unit, Building), float> unitBuildingDamageDict = new();
    private bool isPrecached;

#if UNITY_EDITOR
    private void OnValidate() {
        foreach (var pair in unitUnitPairs) {
            Debug.Assert(pair.AttackerPrefab);
            Debug.Assert(pair.TargetPrefab);
            Debug.Assert(PrefabUtility.IsPartOfPrefabAsset(pair.AttackerPrefab));
            Debug.Assert(PrefabUtility.IsPartOfPrefabAsset(pair.TargetPrefab));
        }
        isPrecached = false;
    }
#endif

    public void EnsureTablesArePrecached() {
        if (isPrecached)
            return;

        unitUnitDamageDict = new Dictionary<(Unit, Unit), float>();
        foreach (var pair in unitUnitPairs) {
            var key = (pair.AttackerPrefab, pair.TargetPrefab);
            Debug.Assert(!unitUnitDamageDict.ContainsKey(key), $"Duplicate damage entry for attacker {pair.AttackerPrefab.name} and target {pair.TargetPrefab.name}");
            unitUnitDamageDict[key] = pair.Damage;
        }

        unitBuildingDamageDict = new Dictionary<(Unit, Building), float>();
        foreach (var pair in unitBuildingPairs) {
            var key = (pair.AttackerPrefab, pair.TargetPrefab);
            Debug.Assert(!unitBuildingDamageDict.ContainsKey(key), $"Duplicate damage entry for attacker {pair.AttackerPrefab.name} and target {pair.TargetPrefab.name}");
            unitBuildingDamageDict[key] = pair.Damage;
        }
        
        isPrecached = true;
    }

    public float GetDamage(Unit attacker, Unit target) {
        EnsureTablesArePrecached();
        if (unitUnitDamageDict.TryGetValue((attacker, target), out var damage))
            return damage;
        Debug.LogWarning($"No damage entry found for attacker {attacker.name} and target {target.name}. Returning 0 damage.");
        return 0;
    }

    public float GetDamage(Unit attacker, Building target) {
        EnsureTablesArePrecached();
        if (unitBuildingDamageDict.TryGetValue((attacker, target), out var damage))
            return damage;
        Debug.LogWarning($"No damage entry found for attacker {attacker.name} and target {target.name}. Returning 0 damage.");
        return 0;
    }
}