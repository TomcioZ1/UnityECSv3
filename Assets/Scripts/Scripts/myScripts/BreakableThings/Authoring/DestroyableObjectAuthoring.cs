using Unity.Entities;
using UnityEngine;

class DestroyableObjectAuthoring : MonoBehaviour
{
    public GameObject dropPrefab;
    public int amount = 50;
    class DestroyableObjectAuthoringBaker : Baker<DestroyableObjectAuthoring>
    {
        public override void Bake(DestroyableObjectAuthoring authoring)
        {
            // Tworzymy encjê dla spawner-a. 
            // U¿ywamy TransformUsageFlags.None, bo spawner sam w sobie 
            // nie musi siê ruszaæ ani byæ widoczny – to tylko "kontener" na dane.
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new DestroyConfig
            {
                // Rejestrujemy prefab jako encjê, aby system móg³ go u¿ywaæ w ecb.Instantiate
                DropPrefab = GetEntity(authoring.dropPrefab, TransformUsageFlags.Dynamic),
                Amount = authoring.amount
            });
        }
    }
}


