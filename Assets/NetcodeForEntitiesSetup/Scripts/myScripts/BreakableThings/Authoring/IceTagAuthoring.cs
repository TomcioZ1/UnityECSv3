using Unity.Entities;
using UnityEngine;

class IceTagAuthoring : MonoBehaviour
{
    class IceTagAuthoringBaker : Baker<IceTagAuthoring>
    {
        public override void Bake(IceTagAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Dodajemy tylko znacznik typu
            AddComponent(entity, new IceTag { IsDestroyed = false });
        }
    }
}
public struct IceTag : IComponentData
{
    public bool IsDestroyed;
}
