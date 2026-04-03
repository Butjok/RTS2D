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

public class BuildingGhostsSystem : WorldBehaviour {

    private readonly Dictionary<Building, Building> buildingGhosts = new();

    private void EnsureBuildingGhostExists(Building buildingPrefab) {
        if (!buildingGhosts.ContainsKey(buildingPrefab)) {
            var ghost = World.Spawn(buildingPrefab, ghost => {
                ghost.Initialize(World, buildingPrefab, null, false, Building.Kind.Ghost);
            });
            buildingGhosts[buildingPrefab] = ghost;
            ghost.gameObject.SetActive(false);
        }
    }

    public Building Get(Building buildingPrefab) {
        EnsureBuildingGhostExists(buildingPrefab);
        return buildingGhosts[buildingPrefab];
    }
    
    public void Show(Building buildingPrefab) {
        EnsureBuildingGhostExists(buildingPrefab);
        buildingGhosts[buildingPrefab].gameObject.SetActive(true);
    }
    
    public void Hide(Building buildingPrefab) {
        if (buildingGhosts.ContainsKey(buildingPrefab))
            buildingGhosts[buildingPrefab].gameObject.SetActive(false);
    }
}