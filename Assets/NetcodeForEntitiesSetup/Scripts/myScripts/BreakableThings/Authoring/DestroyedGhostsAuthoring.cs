using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class DestroyedGhostsAuthoring : MonoBehaviour
{
    // Mo¿esz tu dodaæ dodatkowe pola, jeœli potrzebujesz
    class Baker : Baker<DestroyedGhostsAuthoring>
    {
        public override void Bake(DestroyedGhostsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            // Dodajemy bufor do encji
            AddBuffer<DestroyedGhostElement>(entity);
        }
    }
}

[GhostComponent]
[InternalBufferCapacity(8)]
public struct DestroyedGhostElement : IBufferElementData
{
    [GhostField] public int GhostId;
}