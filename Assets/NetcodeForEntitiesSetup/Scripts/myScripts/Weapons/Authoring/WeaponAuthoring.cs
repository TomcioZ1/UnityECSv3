using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

public class WeaponAuthoringComponent : MonoBehaviour
{
    [Header("Muzzle Point")]
    public GameObject ProjectileSpawner;

    [Header("Weapon Stats")]
    public int maxAmmo = 120;
    public int MagSize = 30;
    public float FireRate = 0.1f;
    public float ReloadTime = 2.0f;
    public float ProjectileSpeed = 20f;
    public int Damage = 10;

    class Baker : Baker<WeaponAuthoringComponent>
    {
        public override void Bake(WeaponAuthoringComponent authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Pobieramy encjê punktu wylotu lufy
            var projectileSpawnerEntity = GetEntity(authoring.ProjectileSpawner, TransformUsageFlags.Dynamic);

            // PRZYPISUJEMY WARTOŒCI Z INSPEKTORA DO KOMPONENTU ECS
            AddComponent(entity, new WeaponData
            {
                ProjectileSpawner = projectileSpawnerEntity,
                magSize = authoring.MagSize,
                currentAmmo = authoring.MagSize, // Startujemy z pe³nym magazynkiem
                fireRate = authoring.FireRate,
                reloadTime = authoring.ReloadTime,
                projectileSpeed = authoring.ProjectileSpeed,
                damage = authoring.Damage,
                maxAmmo = authoring.maxAmmo
            });

            // Stan roboczy broni (timery)
            AddComponent<WeaponWorkState>(entity);

            // Skala i transformacje
            AddComponent(entity, new BaseScale { Value = authoring.transform.localScale });
            AddComponent<PostTransformMatrix>(entity);
            AddComponent<LocalTransform>(entity);
            AddComponent<Parent>(entity);

            // Netcode
            AddComponent<WeaponOwner>(entity);
            AddComponent<GhostAuthoringComponent>(entity);
            //AddComponent<GhostOwner>(entity);
        }
    }
}