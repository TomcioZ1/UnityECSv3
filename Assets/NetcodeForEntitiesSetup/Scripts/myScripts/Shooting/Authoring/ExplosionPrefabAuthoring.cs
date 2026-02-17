using Unity.Entities;
using UnityEngine;

public class ExplosionPrefabAuthoring : MonoBehaviour
{
    public GameObject explosionPrefab;

    public class Baker : Baker<ExplosionPrefabAuthoring>
    {
        public override void Bake(ExplosionPrefabAuthoring authoring)
        {
            if (authoring.explosionPrefab == null) return;

            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ExplosionPrefab
            {
                // Rejestrujemy prefab jako Entity
                Value = GetEntity(authoring.explosionPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}