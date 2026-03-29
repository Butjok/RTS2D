using System.Collections.Generic;
using UnityEngine;

[RequireComponent( typeof(AudioSource))]
public class Projectile : WorldBehaviour {
    
    private const int maxDamagedColliders = 10;
    
    [SerializeField] private Unit owningUnit;
    [SerializeField] private Renderer renderer;
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
        if (!renderer )
            renderer = GetComponent<Renderer>();
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
        renderer.enabled = false;
        audioSource.Play();
        
        var clipLength = audioSource.clip ? audioSource.clip.length : 0;
        Destroy(gameObject, clipLength);
        
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
}