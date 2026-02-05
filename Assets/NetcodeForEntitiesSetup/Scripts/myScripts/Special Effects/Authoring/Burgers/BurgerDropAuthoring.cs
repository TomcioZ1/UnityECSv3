using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

class BurgerDropAuthoring : MonoBehaviour
{
    class BurgerDropAuthoringBaker : Baker<BurgerDropAuthoring>
    {
        public override void Bake(BurgerDropAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BurgerDrop());
        }
    }

}
public struct BurgerDrop : IComponentData
{
    public float3 Velocity;      // Prêdkoœæ lotu
    public float RemainingLife;  // Czas do znikniêcia
}


