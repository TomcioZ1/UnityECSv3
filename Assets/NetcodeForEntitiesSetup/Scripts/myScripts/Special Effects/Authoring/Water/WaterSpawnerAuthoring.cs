using Unity.Entities;
using UnityEngine;

// Komponent z danymi (nadal mo¿e siê nazywaæ Config, 
// ale nazwa klasy Authoring jest teraz zgodna z Twoj¹ proœb¹)
public struct WaterPrefabConfig : IComponentData
{
    public Entity DropPrefab;
}

public class WaterSpawnerAuthoring : MonoBehaviour
{
    public GameObject dropPrefab;

    class Baking : Baker<WaterSpawnerAuthoring>
    {
        public override void Bake(WaterSpawnerAuthoring authoring)
        {
            // Tworzymy encjê dla spawner-a. 
            // U¿ywamy TransformUsageFlags.None, bo spawner sam w sobie 
            // nie musi siê ruszaæ ani byæ widoczny – to tylko "kontener" na dane.
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new WaterPrefabConfig
            {
                // Rejestrujemy prefab jako encjê, aby system móg³ go u¿ywaæ w ecb.Instantiate
                DropPrefab = GetEntity(authoring.dropPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}