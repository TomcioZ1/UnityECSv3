using Unity.Entities;
using UnityEngine;

class BurgerTagAuthoring : MonoBehaviour
{
    class BurgerTagAuthoringBaker : Baker<BurgerTagAuthoring>
    {
        public override void Bake(BurgerTagAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Dodajemy tylko znacznik typu
            AddComponent(entity, new BurgerTag { IsDestroyed = false });
        }
    }
}



public struct BurgerTag : IComponentData
{
    public bool IsDestroyed;
}