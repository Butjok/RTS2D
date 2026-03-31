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

public class RefineryBuilding : Building {

    [SerializeField] private Transform goldDepositPoint;

    public Vector2 GoldDepositPosition => goldDepositPoint.position.ToVector2();
    public Vector2Int GoldDepositionCell => World.Grid.WorldPositionToCell(GoldDepositPosition);

    public void AddGold(float amount) {
        OwningPlayer.AddGold(this, amount);
    }
}