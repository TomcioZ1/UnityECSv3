using UnityEngine;
using Unity.Entities;

public class WeaponSpawerAuthoring : MonoBehaviour
{
    public GameObject PistolPrefab;

    public class Baker : Baker<WeaponSpawerAuthoring>
    {
        public override void Bake(WeaponSpawerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new WeaponResources
            {
                // Wa¿ne: GetEntity zamienia Prefab GameObject na Entity
                Pistol = GetEntity(authoring.PistolPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}