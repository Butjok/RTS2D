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

using UnityEngine;

/*
 * This class is used for units and buildings which are placed in the scene using Unity editor.
 * It contains all the necessary information to initialize the unit or building at the start of the game, such as which player it belongs to and which prefab to use for it.
 */

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