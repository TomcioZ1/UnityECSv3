using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class FootprintSpawnerAuthoring : MonoBehaviour
{
    public GameObject footprintPrefab;

    class Baker : Baker<FootprintSpawnerAuthoring>
    {
        public override void Bake(FootprintSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // Rejestrujemy prefab, aby ECS móg³ go u¿ywaæ
            Entity prefabEntity = GetEntity(authoring.footprintPrefab, TransformUsageFlags.Dynamic);

            AddComponent(entity, new FootprintSpawner
            {
                FootprintPrefab = prefabEntity,
            });
        }
    }
}