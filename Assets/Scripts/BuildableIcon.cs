using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BuildableIcon : Image {

    private static int progressPropertyId = Shader.PropertyToID("_Progress");

    [SerializeField] private World world;
    [SerializeField] private Material baseMaterial;
    [SerializeField] private float progress;
    private Material materialInstance;
    private Building.BuildingQueueItem itemInConstruction;

    public Building testBuilding;
    public Unit unitPrefab;

    [ContextMenu("Instantiate Material")]
    private void InstantiateMaterial() {
        if (baseMaterial) {
            materialInstance = Instantiate(baseMaterial);
            material = materialInstance;
        }
    }

    protected override void Awake() {
        base.Awake();
        InstantiateMaterial();
        Progress = 1;
    }

    protected override void OnValidate() {
        Progress = progress;
    }

    private void Update() {
        if (itemInConstruction != null)
            Progress = itemInConstruction.Progress;
    }

    public float Progress {
        get => progress;
        set {
            progress = value;
            if (materialInstance)
                materialInstance.SetFloat(progressPropertyId, progress);
        }
    }

    public void StartBuilding() {
        if (!testBuilding)
            testBuilding = FindObjectsByType<Building>(FindObjectsSortMode.None).FirstOrDefault(b => !b.OwningPlayer.IsAi);
        itemInConstruction = testBuilding.StartBuilding(unitPrefab);
    }

    protected override void OnPopulateMesh(VertexHelper toFill) {
        base.OnPopulateMesh(toFill);

        for (var i = 0; i < toFill.currentVertCount; i++) {
            UIVertex uiVertex = default;
            toFill.PopulateUIVertex(ref uiVertex, i);
            if (sprite) {
                var min = sprite.textureRect.min;
                var max = sprite.textureRect.max;
                min /= sprite.texture.width;
                max /= sprite.texture.height;
                uiVertex.uv0 = i switch {
                    3 => new Vector2(max.x, min.y),
                    0 => new Vector2(min.x, min.y),
                    1 => new Vector2(min.x, max.y),
                    2 => new Vector2(max.x, max.y),
                    _ => throw new System.IndexOutOfRangeException($"Unexpected vertex index {i} when populating BuildableIcon mesh")
                };
            }
            uiVertex.uv1 = i switch {
                0 => new Vector2(0, 0),
                1 => new Vector2(1, 0),
                2 => new Vector2(1, 1),
                3 => new Vector2(0, 1),
                _ => throw new System.IndexOutOfRangeException($"Unexpected vertex index {i} when populating BuildableIcon mesh")
            };
            toFill.SetUIVertex(uiVertex, i);
        }
    }
}