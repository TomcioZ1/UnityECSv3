using Unity.Entities;
using UnityEngine;

public struct IcePrefabConfig : IComponentData
{
    public Entity IceDropPrefab;
}


class IceSpawnerAuthoring : MonoBehaviour
{
    public GameObject iceDropPrefab;
    class IceSpawnerAuthoringBaker : Baker<IceSpawnerAuthoring>
    {
        public override void Bake(IceSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new IcePrefabConfig
            {
                // Zamieniamy GameObject na Entity typu Dynamic
                IceDropPrefab = GetEntity(authoring.iceDropPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }

}

