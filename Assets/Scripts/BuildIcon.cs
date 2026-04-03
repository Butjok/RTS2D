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

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildIcon : Image, IPointerClickHandler {

    private static int progressPropertyId = Shader.PropertyToID("_Progress");

    [SerializeField] private PlayerHUD owningHUD;
    [SerializeField] private Material baseMaterial;
    [SerializeField] private float progress;
    [SerializeField] private TMP_Text label;

    private Material materialInstance;
    private ConstructionOption constructionOption;
    private ConstructionQueueItem constructionQueueItem;

    public ConstructionOption ConstructionOption => constructionOption;

    public void Initialize(ConstructionOption constructionOption) {
        this.constructionOption = constructionOption;
        InstantiateMaterial();
        Progress = 1;
        if (label)
            label.text = constructionOption.Prefab.name;
    }

    [ContextMenu("Instantiate Material")]
    private void InstantiateMaterial() {
        if (baseMaterial) {
            materialInstance = Instantiate(baseMaterial);
            material = materialInstance;
        }
    }

    protected override void OnValidate() {
        Progress = progress;
        label = GetComponentInChildren<TMP_Text>();
    }

    private void Update() {
        if (constructionQueueItem && constructionQueueItem.BuildStatus == ConstructionQueueItem.Status.QueuedOrBuilding)
            Progress = constructionQueueItem.Progress;
    }

    public float Progress {
        get => progress;
        set {
            progress = value;
            if (materialInstance)
                materialInstance.SetFloat(progressPropertyId, progress);
        }
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

    public void OnPointerClick(PointerEventData eventData) {

        if (eventData.button == PointerEventData.InputButton.Left) {

            if (!constructionQueueItem) {
                var primaryBuilding = owningHUD.PlayerController.Player.GetPrimaryBuilding(constructionOption.SourceBuildingType);
                Debug.Assert(primaryBuilding);
                constructionQueueItem = primaryBuilding.ConstructionQueue.StartBuilding(constructionOption);
            }

            else if (constructionQueueItem.BuildStatus == ConstructionQueueItem.Status.OnHold)
                constructionQueueItem.BuildStatus = ConstructionQueueItem.Status.QueuedOrBuilding;

            else if (constructionQueueItem.BuildStatus == ConstructionQueueItem.Status.ConstructionComplete) {
                Debug.Assert(constructionQueueItem.ConstructionOption.Prefab is Building);
                Debug.Assert(owningHUD.PlayerController);
                owningHUD.PlayerController.StartBuildingPlacement(this);
            }

            else if (constructionQueueItem.BuildStatus == ConstructionQueueItem.Status.QueuedOrBuilding && constructionQueueItem.ConstructionOption.Prefab is Unit)
                constructionQueueItem.amount++;
        }

        else if (eventData.button == PointerEventData.InputButton.Right) {
            if (constructionQueueItem) {

                if (constructionQueueItem.BuildStatus == ConstructionQueueItem.Status.QueuedOrBuilding)
                    constructionQueueItem.BuildStatus = ConstructionQueueItem.Status.OnHold;

                else if (constructionQueueItem.BuildStatus == ConstructionQueueItem.Status.OnHold) {
                    if (constructionQueueItem.amount > 1)
                        constructionQueueItem.amount--;
                    else {
                        constructionQueueItem.BuildStatus = ConstructionQueueItem.Status.Cancelled;
                        constructionQueueItem = null;
                        Progress = 1;
                    }
                }
            }
        }
    }

    public void NotifyWasPlaced() {
        constructionQueueItem.Invalidate();
        constructionQueueItem = null;
        Progress = 1;
    }
}