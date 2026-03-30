using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode]
#endif
public class PrePlacedGold : WorldBehaviour {

    [SerializeField] private float amount = .1f;
    public float Amount => amount;

    private void OnValidate() {
        gameObject.name = $"Gold {amount}";
    }

#if UNITY_EDITOR
    private Vector3? oldPosition;
    private void Update() {
        if (transform.position != oldPosition) {
            var cell = World.Grid.WorldPositionToCell(transform.position.ToVector2());
            transform.position = World.Grid.CellToWorldPosition(cell).ToVector3();
        }
        oldPosition = transform.position;
    }
#endif
}