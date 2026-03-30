// Disable diagonal movement for now.
// The reason is that path smoothing is using an Amanatides-Woo tracing algorithm, to trace the cells along the path in axis-aligned movement fashion.
// When trying to trace between diagonal movement it traces exactly through the cell corners and thus it can go through either left or right neighboring
// cell first, which can cause to include into path not walkable cells.
// We mitigate this by smoothing the path, so the movement of units still looks okay.

//#define USE_DIAGONAL_MOVEMENT 

using System.Collections.Generic;
//using Drawing;
using UnityEngine;
using Object = UnityEngine.Object;

public class Grid : WorldBehaviour {

    public const float cellSize = 1;
    public const float sqrt2 = 1.41421356237f;

    public struct CellInfo {
        public Unit occupiedBy;
        public Unit reservedBy;
        public bool isWalkable;
        public float goldAmount;
        public bool HasGold => goldAmount > 0;
    }

    public static readonly IReadOnlyList<Vector2Int> moveDirections = new[] {
        // axis aligned
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),

        // diagonal
#if USE_DIAGONAL_MOVEMENT
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, -1),
        new Vector2Int(-1, 1)
#endif
    };

    public static float Distance(Vector2 a, Vector2 b) {
        var delta = new Vector2(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
#if USE_DIAGONAL_MOVEMENT
        var diagonal = Mathf.Min(delta.x, delta.y);
        var axisAligned = Mathf.Max(delta.x, delta.y) - diagonal;
        return diagonal * sqrt2 + axisAligned;
#else
        return delta.x + delta.y;
#endif
    }

    public static Vector2Int WorldPositionToCell(Vector2 position, Vector2 minCellPosition) {
        return new Vector2Int(
            Mathf.RoundToInt((position.x - minCellPosition.x) / cellSize),
            Mathf.RoundToInt((position.y - minCellPosition.y) / cellSize)
        );
    }

    public static Vector2 CellToWorldPosition(Vector2Int index, Vector2 minCellPosition) {
        return new Vector2(
            (index.x + minCellPosition.x) * cellSize,
            (index.y + minCellPosition.y) * cellSize
        );
    }

    [SerializeField] private List<Collider> staticColliders = new();
    [SerializeField] private Vector2 minCellPosition;
    [SerializeField] private CellInfo[,] grid = new CellInfo[0, 0];
    [SerializeField] private Vector2Int size;
    [SerializeField] private GridBasedAStar gridBasedAStar;

    public bool showWalkableCells;
    public bool showOccupiedCells;
    public bool showReservedCells;

    public GridBasedAStar GridBasedAStar => gridBasedAStar;
    public Vector2Int Size => size;

    public bool InBounds(Vector2Int index) {
        return index.x >= 0 && index.x < grid.GetLength(0) && index.y >= 0 && index.y < grid.GetLength(1);
    }

    public ref CellInfo this[Vector2Int index] => ref grid[index.x, index.y];

    public Vector2Int WorldPositionToCell(Vector2 position) {
        return WorldPositionToCell(position, minCellPosition);
    }
    public Vector2 CellToWorldPosition(Vector2Int index) {
        return CellToWorldPosition(index, minCellPosition);
    }

    public (Vector2Int min, Vector2Int max) GetMinMaxIndices(Bounds bounds) {
        var minIndex = WorldPositionToCell(bounds.min.ToVector2());
        var maxIndex = WorldPositionToCell(bounds.max.ToVector2());
        return (minIndex, maxIndex);
    }
    public IEnumerable<Vector2Int> EnumerateIndicesInside(Bounds bounds) {
        var (minIndex, maxIndex) = GetMinMaxIndices(bounds);
        for (var x = minIndex.x; x <= maxIndex.x; x++)
        for (var y = minIndex.y; y <= maxIndex.y; y++)
            yield return new Vector2Int(x, y);
    }

    public IEnumerable<Vector2Int> EnumerateNeighborIndices(Vector2Int index) {
        foreach (var offset in moveDirections) {
            var neighborIndex = index + offset;
            if (InBounds(neighborIndex))
                yield return neighborIndex;
        }
    }

    [ContextMenu(nameof(Initialize))]
    public void Initialize() {
        if (!World)
            return;

        minCellPosition = World.WorldBounds.bounds.min.ToVector2();

        var boundsSize = World.WorldBounds.bounds.size.ToVector2();
        var width = Mathf.CeilToInt(boundsSize.x / cellSize);
        var height = Mathf.CeilToInt(boundsSize.y / cellSize);

        size = new Vector2Int(width, height);
        grid = new CellInfo[width, height];
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++) {
            var index = new Vector2Int(x, y);
            ref var cell = ref this[index];
            cell.isWalkable = true;
            cell.reservedBy = null;
        }

        foreach (var staticCollider in staticColliders)
        foreach (var index in EnumerateIndicesInside(staticCollider.bounds))
            this[index].isWalkable = false;

        gridBasedAStar = new GridBasedAStar(this);
        
        // find pre-placed gold
        var prePlacedGolds = FindObjectsByType<PrePlacedGold>(FindObjectsSortMode.None);
        foreach (var prePlacedGold in prePlacedGolds) {
            Debug.Assert(prePlacedGold.Amount > 0, $"PrePlacedGold {prePlacedGold.name} has non-positive amount {prePlacedGold.Amount}");
            var cell = WorldPositionToCell(prePlacedGold.transform.position.ToVector2());
            if (InBounds(cell)) {
                Debug.Assert(this[cell].goldAmount == 0, $"Multiple golds at location {CellToWorldPosition(cell)}");
                this[cell].goldAmount = prePlacedGold.Amount;
            }
        }
    }

    private void OnValidate() {
        Initialize();
    }

#if UNITY_EDITOR
    private void Update() {
        for (var x = 0; x < size.x; x++)
        for (var y = 0; y < size.y; y++) {
            var index = new Vector2Int(x, y);
            ref var cell = ref this[index];
            if (showWalkableCells) {
                var color = cell.isWalkable ? Color.green : Color.red;
                //Draw.ingame.SolidBox(IndexToWorldPosition(index).ToVector3(), new Vector3(1, 0, 1) * cellSize, color);
            }
            if (showOccupiedCells && cell.occupiedBy) {
                //Draw.ingame.xz.SolidCircle(IndexToWorldPosition(index).ToVector3(), cellSize / 2, Color.blue);
                //Draw.ingame.Line(IndexToWorldPosition(index).ToVector3(), cell.occupiedBy.transform.position, Color.blue);
            }
            if (showReservedCells && cell.reservedBy) {
                // with yellow
                //Draw.ingame.xz.SolidCircle(IndexToWorldPosition(index).ToVector3(), cellSize / 2, Color.yellow);
                //Draw.ingame.Line(IndexToWorldPosition(index).ToVector3(), cell.reservedBy.transform.position, Color.yellow);
            }
        }
    }
#endif

    private void Awake() {
        World.onObjectSpawned += SetNotWalkable;
        World.onObjectDestroyed += SetWalkable;
    }
    private void OnDestroy() {
        World.onObjectSpawned -= SetNotWalkable;
        World.onObjectDestroyed -= SetWalkable;
    }

    public void SetWalkable(Object obj) {
        if (obj is Building building)
            SetWalkable(building.BoxCollider, true);
    }
    public void SetNotWalkable(Object obj) {
        if (obj is Building building)
            SetWalkable(building.BoxCollider, false);
    }
    private void SetWalkable(BoxCollider boxCollider, bool walkable) {
        foreach (var index in EnumerateIndicesInside(boxCollider.bounds)) {
            if (!walkable) {
                Debug.Assert(!this[index].occupiedBy);
                Debug.Assert(!this[index].reservedBy);
            }
            this[index].isWalkable = walkable;
        }
    }
}