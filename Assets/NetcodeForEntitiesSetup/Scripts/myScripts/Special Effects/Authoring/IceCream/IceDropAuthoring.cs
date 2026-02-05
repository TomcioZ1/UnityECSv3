using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class IceDropAuthoring : MonoBehaviour
{
    class IceDropAuthoringBaker : Baker<IceDropAuthoring>
    {
        public override void Bake(IceDropAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new IceDrop());
        }
    }

}

public struct IceDrop : IComponentData
{
    public float3 Velocity;      // Prêdkoœæ lotu
    public float RemainingLife;  // Czas do znikniêcia
}


