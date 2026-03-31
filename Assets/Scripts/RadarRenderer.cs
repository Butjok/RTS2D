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
using UnityEngine.UI;

public class RadarRenderer : MonoBehaviour {

    [SerializeField] private PlayerHUD playerHUD;
    [SerializeField] private Vector2Int size = new(256, 256);
    [SerializeField] private RenderTexture texture;
    [SerializeField] private RawImage targetImage;
    [SerializeField] private Material material;

    public RenderTexture Texture => texture;

    private void Awake() {
        texture = new RenderTexture(size.x, size.y, 0);
        texture.Create();

        targetImage.texture = texture;
    }

    private void OnDestroy() {
        if (texture)
            texture.Release();
    }

    public void RenderRadar() {
        
        // all of this does not work currently
        
        var previousRenderTexture = RenderTexture.active;
        RenderTexture.active = texture;

        GL.Clear(true, true, Color.black);
        
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, size.x, 0, size.y);
        
        material.SetPass(0);

        var world = playerHUD.World;
        var lowerLeft = world.WorldBounds.bounds.min.ToVector2();
        var topRight = world.WorldBounds.bounds.max.ToVector2();

        foreach (var unit in world.Units) {
            var unitPosition2D = unit.transform.position.ToVector2();
            var normalizedPosition = (unitPosition2D - lowerLeft) / (topRight - lowerLeft);
            var pixelPosition = new Vector2(normalizedPosition.x * size.x, normalizedPosition.y * size.y);
            var rectangle = Rect.MinMaxRect(pixelPosition.x - 10, pixelPosition.y - 10, pixelPosition.x + 10, pixelPosition.y + 10);

            var pos = unit.transform.position.ToVector2();
            var norm = (pos - lowerLeft) / (topRight - lowerLeft);

            var px = norm.x * size.x;
            var py = norm.y * size.y;

            float half = 10f;

            GL.Begin(GL.QUADS);
            GL.Color(unit.OwningPlayer.Color);

            GL.Vertex3(px - half, py - half, 0);
            GL.Vertex3(px + half, py - half, 0);
            GL.Vertex3(px + half, py + half, 0);
            GL.Vertex3(px - half, py + half, 0);

            GL.End();
        }

        GL.PopMatrix();

        RenderTexture.active = previousRenderTexture;
    }

    public void Update() {
        RenderRadar();
    }
}