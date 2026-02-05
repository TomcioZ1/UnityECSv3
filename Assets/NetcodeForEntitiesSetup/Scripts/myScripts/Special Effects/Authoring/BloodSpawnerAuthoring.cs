using Unity.Entities;
using UnityEngine;

public struct BloodPrefabConfig : IComponentData
{
    public Entity BloodDropPrefab;
}

class BloodSpawnerAuthoring : MonoBehaviour
{
    public GameObject bloodDropPrefab;

    class BloodSpawnerAuthoringBaker : Baker<BloodSpawnerAuthoring>
    {
        public override void Bake(BloodSpawnerAuthoring authoring)
        {
            // Tworzymy encjê-kontener na dane (nie potrzebuje transformu)
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new BloodPrefabConfig
            {
                // Zamieniamy GameObject na Entity typu Dynamic
                BloodDropPrefab = GetEntity(authoring.bloodDropPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}