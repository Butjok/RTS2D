using UnityEngine;

public class RefineryBuilding : Building {

    [SerializeField] private Vector2 goldDepositPosition;
    
    public Vector2 GoldDepositPosition => goldDepositPosition;
    public Vector2Int GoldDepositionCell => World.Grid.WorldPositionToCell(transform.position.ToVector2() + goldDepositPosition);

    public void AddGold(float amount) {
        OwningPlayer.AddGold(this, amount);
    }
}