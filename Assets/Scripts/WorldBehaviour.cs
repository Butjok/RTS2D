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

public abstract class WorldBehaviour : MonoBehaviour {

    [SerializeField] private WorldBehaviour prefab;
    [SerializeField] private World world;

    public World World => world;

    public T GetPrefab<T>() where T : WorldBehaviour {
        Debug.Assert(prefab is T, $"Prefab of {name} is of type {prefab.GetType().Name} but requested type is {typeof(T).Name}.");
        return (T)prefab;
    }

    private bool wasInitialized;
    protected void Initialize(World world, WorldBehaviour prefab = null) {
        Debug.Assert(!wasInitialized, $"WorldBehaviour {name} was already initialized.");

        wasInitialized = true;

        this.world = world;
        this.prefab = prefab;
    }
}