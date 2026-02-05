using Unity.Entities;
using UnityEngine;
public struct BurgerPrefabConfig : IComponentData
{
    public Entity BurgerDropPrefab;
}


class BurgerSpawnerAuthoring : MonoBehaviour
{
    public GameObject burgerDropPrefab;
    class BurgerSpawnerAuthoringBaker : Baker<BurgerSpawnerAuthoring>
    {
        public override void Bake(BurgerSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new BurgerPrefabConfig
            {
                // Zamieniamy GameObject na Entity typu Dynamic
                BurgerDropPrefab = GetEntity(authoring.burgerDropPrefab, TransformUsageFlags.Dynamic)
            });

        }
    }

}


