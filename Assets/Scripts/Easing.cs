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