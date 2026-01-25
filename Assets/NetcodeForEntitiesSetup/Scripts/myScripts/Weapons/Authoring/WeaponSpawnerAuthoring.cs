using Unity.Entities;
using UnityEngine;

public class WeaponSpawerAuthoring : MonoBehaviour
{
    public GameObject PistolPrefab;
    public GameObject ShotgunPrefab;
    public GameObject SniperPrefab;
    public GameObject AK47Prefab;

    public class Baker : Baker<WeaponSpawerAuthoring>
    {
        public override void Bake(WeaponSpawerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new WeaponResources
            {
                Pistol = GetEntity(authoring.PistolPrefab, TransformUsageFlags.Dynamic),
                Shotgun = GetEntity(authoring.ShotgunPrefab, TransformUsageFlags.Dynamic),
                Sniper = GetEntity(authoring.SniperPrefab, TransformUsageFlags.Dynamic),
                ak47 = GetEntity(authoring.AK47Prefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}