public interface IBuildable {
    public int? Cost { get; }
    public float BuildTime { get; }
    public bool PrerequisitesSatisfied { get; }
}