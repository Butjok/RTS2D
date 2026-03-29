using System.Collections.Generic;
using UnityEngine;

public class PrePlacedInfo : MonoBehaviour {

    private static readonly List<Color> inEditorPreviewPlayerColors = new() {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.cyan,
        Color.magenta,
        Color.gray
    };

    [SerializeField] private World world;
    [SerializeField] private WorldBehaviour target;
    [SerializeField] private WorldBehaviour prefab;
    [SerializeField] private int playerId = -1;

    public World World => world;
    public WorldBehaviour Target => target;
    public WorldBehaviour Prefab => prefab;
    public int PlayerId => playerId;

    public Player Player {
        get {
            foreach (var player in World.Players)
                if (player.Id == playerId)
                    return player;
            return null;
        }
    }

    public void UpdateInEditorPlayerColor() {
        if (playerId < 0 || playerId >= inEditorPreviewPlayerColors.Count)
            return;
        var playerColor = inEditorPreviewPlayerColors[playerId];
        if (target is Unit unit)
            unit.PlayerColor = playerColor;
        else if (target is Building building)
            building.PlayerColor = playerColor;
    }
    private void OnValidate() {
        UpdateInEditorPlayerColor();
    }
}

public interface ICanBePrePlaced {
    public void InitializeFromPrePlacedInfo(PrePlacedInfo info);
}