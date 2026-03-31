using UnityEngine;

public class PrePlacedInfo : MonoBehaviour {

    [SerializeField] private World world;
    [SerializeField] private WorldBehaviour target;
    [SerializeField] private WorldBehaviour prefab;
    [SerializeField] private int playerId = -1;
    [SerializeField] private bool isPrimaryBuilding;

    public World World => world;
    public WorldBehaviour Target => target;
    public WorldBehaviour Prefab => prefab;
    public int PlayerId => playerId;
    public bool IsPrimaryBuilding => isPrimaryBuilding;
    
    public Player Player {
        get {
            foreach (var player in World.Players)
                if (player.Id == playerId)
                    return player;
            throw new System.Exception($"No player with id {playerId} found in world");
        }
    }

    public void UpdateInEditorPlayerColor() {
        Color? playerColor = null;
        foreach (var playerSpawnInfo in world.PlayerSpawnInfos)
            if (playerSpawnInfo.Id == playerId) {
                playerColor = playerSpawnInfo.Color;
                break;
            }
        if (playerColor is {} actualPlayerColor) {
            if (target is Unit unit)
                unit.PlayerColor = actualPlayerColor;
            else if (target is Building building)
                building.PlayerColor = actualPlayerColor;
        }
    }
    private void OnValidate() {
        UpdateInEditorPlayerColor();
    }
}

public interface ICanBePrePlaced {
    public void InitializeFromPrePlacedInfo(PrePlacedInfo info);
}