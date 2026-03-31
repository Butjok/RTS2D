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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent( typeof(AudioSource))]
public class Projectile : WorldBehaviour {
    
    private const int maxDamagedColliders = 10;
    
    [SerializeField] private Unit owningUnit;
    [FormerlySerializedAs("renderer")] [SerializeField] private Renderer projectileRenderer;
    [SerializeField] private Vector3 target;
    [SerializeField] private float speed = 3;
    [SerializeField] private float explosionRadius = 1;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private  Collider[] damagedColliders = new Collider[maxDamagedColliders];
    private readonly HashSet<Unit> damagedUnits = new();
    private readonly HashSet<Building> damagedBuilding = new();

    public void Initialize(World world, Projectile prefab, Unit owningUnit, Vector3 target) {
        base.Initialize(world, prefab);
        this.owningUnit = owningUnit;
        this.target = target;
    }
    
    private void OnValidate() {
        if (!projectileRenderer )
            projectileRenderer = GetComponent<Renderer>();
        if (!audioSource )
            audioSource = GetComponent<AudioSource>();
    }

    private void Update() {
        var direction = target - transform.position;
        var distanceToMove = speed * Time.deltaTime;
        if (direction.magnitude <= distanceToMove) {
            transform.position = target;
            Explode();
        }
        else {
            transform.position += direction.normalized * distanceToMove;
        }
    }

    private void Explode() {
        enabled = false;
        projectileRenderer.enabled = false;
        audioSource.Play();
        
        var clipLength = audioSource.clip ? audioSource.clip.length : 0;
        StartCoroutine(DelayedDestruction(clipLength));
        
        var count =  Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, damagedColliders);
        for  (var i = 0; i < count; i++) {
            var unit = damagedColliders[i].GetComponent<Unit>();
            if (unit && !damagedUnits.Contains(unit)) {
                damagedUnits.Add(unit);
                unit.ReceiveAttackFrom(owningUnit);
            }
            var building = damagedColliders[i].GetComponent<Building>();
            if (building && !damagedBuilding.Contains(building)) {
                damagedBuilding.Add(building);
                building.ReceiveAttackFrom(owningUnit);
            }
        }
    }

    private IEnumerator DelayedDestruction(float delay) {
        var elapsed = 0f;
        while (elapsed < delay) {
            elapsed += Time.deltaTime;
            yield return null;
        }
        World.Destroy(this);
    }
}