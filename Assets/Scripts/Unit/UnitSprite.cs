using System.Collections.Generic;
using UnityEngine;

public class UnitSprite : MonoBehaviour {

    [SerializeField] private Unit owningUnit;
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private List<Sprite> roundTableSprites = new();
    
    private void Update() {
        spriteTransform.rotation = owningUnit.World.WorldCamera.transform.rotation;
        var angle = -owningUnit.transform.rotation.eulerAngles.y;
        angle -= 180;
        angle -= 45; 
        var frames = roundTableSprites.Count;
        var frame = (Mathf.RoundToInt(angle / 360 * frames) % frames + frames) % frames;
        spriteRenderer.sprite = roundTableSprites[frame];
    }
}