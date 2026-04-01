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

using System.Collections.Generic;
using UnityEngine;

public class RoundTableSprite : MonoBehaviour {

    [SerializeField] private Unit owningUnit;
    [SerializeField] private Transform sourceTransform;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private List<Sprite> roundTableSprites = new();

    private float Yaw {
        set {
            var frames = roundTableSprites.Count;
            if (frames == 0)
                return;
            var angle = -value;
            angle -= 180;
            angle -= 45; 
            var frame = (Mathf.RoundToInt(angle / 360 * frames) % frames + frames) % frames;
            spriteRenderer.sprite = roundTableSprites[frame];    
        }
    }

    private void Start() {
        spriteRenderer.transform.rotation = owningUnit.World.WorldCamera.transform.rotation;
    }

    private void Update() {
        Yaw = sourceTransform.rotation.eulerAngles.y;
    }
}