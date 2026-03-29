using UnityEngine;

public interface ISelectable {
    public Bounds SelectionBounds { get; }
    public bool IsSelected { get; set; }
    public bool CanEverBeSelected => true;
    public bool ObjectExists { get; }
}