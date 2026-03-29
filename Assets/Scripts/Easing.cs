public static class Easing {
    
    public delegate float EasingFunction(float t);

    public enum Type {
        Linear,
        InOutQuadratic
    }
    
    public static float Linear(float t) {
        return t;
    }
    public static float InOutQuadratic(float t) {
        return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
    }
    
    public static EasingFunction GetEasingFunction(Type type) {
        return type switch {
            Type.Linear => Linear,
            Type.InOutQuadratic => InOutQuadratic,
            _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
    public static float Evaluate(Type type, float t) {
        return GetEasingFunction(type)(t);
    }
}