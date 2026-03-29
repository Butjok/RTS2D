using System.Collections.Generic;

public class BuildingGhostsSystem : WorldBehaviour {

    private readonly Dictionary<Building, Building> buildingGhosts = new();

    private void EnsureBuildingGhostExists(Building buildingPrefab) {
        if (!buildingGhosts.ContainsKey(buildingPrefab)) {
            var ghost = World.Spawn(buildingPrefab, ghost => ghost.SetUpAsGhost());
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