using UnityEngine;

public class RefineryBuilding : Building {

    [SerializeField] private Transform goldDepositPoint;

    public Vector2 GoldDepositPosition => goldDepositPoint.position.ToVector2();
    public Vector2Int GoldDepositionCell => World.Grid.WorldPositionToCell(GoldDepositPosition);

    public void AddGold(float amount) {
        OwningPlayer.AddGold(this, amount);
    }
}