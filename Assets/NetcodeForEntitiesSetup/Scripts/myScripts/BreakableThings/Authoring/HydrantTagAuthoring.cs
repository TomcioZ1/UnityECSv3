using Unity.Entities;
using UnityEngine;

public class HydrantTagAuthoring : MonoBehaviour
{
    class Baking : Baker<HydrantTagAuthoring>
    {
        public override void Bake(HydrantTagAuthoring authoring)
        {
            // Pobieramy TÊ SAM¥ encjê, któr¹ pobiera HydrantAuthoring
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Dodajemy tylko znacznik typu
            AddComponent(entity, new HydrantTag { IsDestroyed = false });

        }
    }
}

public struct HydrantTag : IComponentData
{
    public bool IsDestroyed;
}   