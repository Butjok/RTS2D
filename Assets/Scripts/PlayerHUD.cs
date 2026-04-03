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

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class PlayerHUD : WorldBehaviour {

    [Serializable]
    private struct HealthColorRampPoint {
        [SerializeField] public float end;
        [SerializeField] public Color color;
        [NonSerialized] public Texture2D onePixelTexture;
        [NonSerialized] public GUIStyle style;
    }

    [SerializeField] private PlayerController playerController;

    [Header("Selection Marquee")] [SerializeField]
    private Color marqueeColor = new(1, 1, 1, .5f);

    private Texture2D marqueeBackgroundTexture;
    private GUIStyle marqueeGUIStyle;

    [Header("Unit Health Bar")] [SerializeField]
    private Color unitHealthBarBackgroundColor = new(0, 0, 0, .5f);

    private Texture2D unitHealthBarBackgroundTexture;
    private GUIStyle unitHealthBarGUIStyle;

    [SerializeField] private float unitHealthBarHeight = 2.5f;
    [SerializeField] private Vector2 unitHealthBarPadding = new(1, 1);

    [SerializeField] private List<HealthColorRampPoint> unitHealthBarColorRamp = new() {
        new HealthColorRampPoint { end = .25f, color = Color.red },
        new HealthColorRampPoint { end = .5f, color = Color.yellow },
        new HealthColorRampPoint { end = 1, color = Color.green },
    };

    [SerializeField] private RadarRenderer radarRenderer;

    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject defeatScreen;

    [SerializeField] private BuildIcon buildIconTemplate;
    
    private List<BuildIcon > existingBuildIcons = new();
    private List<ConstructionOption> buildIconsToBuild = new();

    public PlayerController PlayerController => playerController;

    public void Initialize(World world, PlayerHUD prefab, PlayerController playerController) {
        base.Initialize(world, prefab);
        this.playerController = playerController;
    }

    private void Awake() {

        // Hide screen just in case

        victoryScreen.SetActive(false);
        defeatScreen.SetActive(false);

        // Background color texture for selection marquee

        marqueeBackgroundTexture = new Texture2D(1, 1);
        marqueeBackgroundTexture.SetPixel(0, 0, marqueeColor);
        marqueeBackgroundTexture.Apply();
        marqueeGUIStyle = new GUIStyle {
            normal = {
                background = marqueeBackgroundTexture
            }
        };

        // Background color textures for unit health bars

        for (var i = 0; i < unitHealthBarColorRamp.Count; i++) {
            var rampPoint = unitHealthBarColorRamp[i];
            rampPoint.onePixelTexture = new Texture2D(1, 1);
            rampPoint.onePixelTexture.SetPixel(0, 0, rampPoint.color);
            rampPoint.onePixelTexture.Apply();
            rampPoint.style = new GUIStyle {
                normal = {
                    background = rampPoint.onePixelTexture
                }
            };
            unitHealthBarColorRamp[i] = rampPoint;
        }

        unitHealthBarBackgroundTexture = new Texture2D(1, 1);
        unitHealthBarBackgroundTexture.SetPixel(0, 0, unitHealthBarBackgroundColor);
        unitHealthBarBackgroundTexture.Apply();
        unitHealthBarGUIStyle = new GUIStyle {
            normal = {
                background = unitHealthBarBackgroundTexture
            }
        };
    }

    public static Rect GetOnScreenBounds(Bounds bounds, Camera camera) {
        var corners = new Vector3[8];
        corners[0] = bounds.min;
        corners[1] = bounds.max;
        corners[2] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[5] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[6] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);

        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        foreach (var corner in corners) {
            var screenPoint = camera.WorldToScreenPoint(corner);
            min = Vector2.Min(min, screenPoint);
            max = Vector2.Max(max, screenPoint);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public static Rect ToGUICoordinates(Rect rect) {
        return new Rect(rect.x, Screen.height - rect.yMax, rect.width, rect.height);
    }

    public static void DrawRectangle(Rect rectangle, GUIStyle style = null) {
        GUI.Box(rectangle, "", style ?? GUI.skin.box);
    }

    private void OnGUI() {
        GUI.depth = -100;

        if (playerController.MarqueeStart is { } actualMarqueeStart) {
            var min = Vector2.Min(actualMarqueeStart, playerController.MarqueeEnd);
            var max = Vector2.Max(actualMarqueeStart, playerController.MarqueeEnd);
            var rectangle = new Rect(min, max - min);
            DrawRectangle(ToGUICoordinates(rectangle), marqueeGUIStyle);
        }

        foreach (var selectable in World.Selectables)
            if (selectable != null && selectable.ObjectExists && selectable is IHasHealth health && (selectable.IsSelected || health.LastDamageTime.HasValue && Time.time - health.LastDamageTime.Value <= 5)) {
                var onScreenBounds = GetOnScreenBounds(selectable.SelectionBounds, playerController.PlayerCamera);
                var healthBarRectangle = ToGUICoordinates(new Rect(
                    onScreenBounds.xMin, onScreenBounds.yMin - unitHealthBarHeight,
                    onScreenBounds.width, unitHealthBarHeight
                ));

                var healthBarBackgroundRectangle = healthBarRectangle;
                healthBarBackgroundRectangle.x -= unitHealthBarPadding.x;
                healthBarBackgroundRectangle.width += unitHealthBarPadding.x * 2;
                healthBarBackgroundRectangle.y -= unitHealthBarPadding.y;
                healthBarBackgroundRectangle.height += unitHealthBarPadding.y * 2;
                DrawRectangle(healthBarBackgroundRectangle, unitHealthBarGUIStyle);

                var healthBarFilledRectangle = healthBarRectangle;
                healthBarFilledRectangle.width = healthBarRectangle.width * health.Health;

                var intervalStart = .0f;
                GUIStyle fillStyle = null;
                for (var i = 0; i < unitHealthBarColorRamp.Count; i++) {
                    var rampPoint = unitHealthBarColorRamp[i];
                    var intervalEnd = rampPoint.end;
                    if (health.Health >= intervalStart && health.Health <= intervalEnd) {
                        fillStyle = rampPoint.style;
                        break;
                    }
                    intervalStart = intervalEnd;
                }
                if (fillStyle != null)
                    DrawRectangle(healthBarFilledRectangle, fillStyle);
            }
    }

    public void ShowVictoryScreen() {
        victoryScreen.SetActive(true);
    }

    public void ShowDefeatScreen() {
        defeatScreen.SetActive(true);
    }

    public void UpdateConstructionOptionsButtons(HashSet<ConstructionOption> constructionOptions) {

        existingBuildIcons.Clear();
        buildIconTemplate.transform.parent.GetComponentsInChildren(false, existingBuildIcons);
        
        buildIconsToBuild.Clear();
        foreach (var constructionOption in constructionOptions)
            if (!existingBuildIcons.Exists(icon => icon.ConstructionOption == constructionOption))
                buildIconsToBuild.Add(constructionOption);

        foreach (var existingBuildIcon in existingBuildIcons)
            if (!constructionOptions.Contains(existingBuildIcon.ConstructionOption))
                Destroy(existingBuildIcon.gameObject);
        
        foreach (var constructionOption in buildIconsToBuild) {
            var buildIcon = Instantiate(buildIconTemplate, buildIconTemplate.transform.parent);
            buildIcon.Initialize(constructionOption);
            buildIcon.gameObject.SetActive(true);
        }
    }
}