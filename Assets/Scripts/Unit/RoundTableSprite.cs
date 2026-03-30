using System.Collections.Generic;
using UnityEngine;

public class RoundTableSprite : MonoBehaviour {

    [SerializeField] private Unit owningUnit;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private List<Sprite> roundTableSprites = new();

    public float Yaw {
        set {
            var angle = -value;
            angle -= 180;
            angle -= 45; 
            var frames = roundTableSprites.Count;
            var frame = (Mathf.RoundToInt(angle / 360 * frames) % frames + frames) % frames;
            spriteRenderer.sprite = roundTableSprites[frame];    
        }
    }

    private void Update() {
        spriteRenderer.transform.rotation = owningUnit.World.WorldCamera.transform.rotation;
    }
}