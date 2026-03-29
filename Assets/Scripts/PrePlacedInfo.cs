using UnityEngine;

public class PrePlacedInfo : MonoBehaviour {

    [SerializeField] private World world;
    [SerializeField] private WorldBehaviour target;
    [SerializeField] private WorldBehaviour prefab;
    [SerializeField] private int playerId = -1;

    public World World => world;
    public WorldBehaviour Target => target;
    public WorldBehaviour Prefab => prefab;

    public Player Player {
        get {
            foreach (var player in World.Players)
                if (player.Id == playerId)
                    return player;
            return null;
        }
    }
}

public interface ICanBePrePlaced {
    public void InitializeFromPrePlacedInfo(PrePlacedInfo info);
}