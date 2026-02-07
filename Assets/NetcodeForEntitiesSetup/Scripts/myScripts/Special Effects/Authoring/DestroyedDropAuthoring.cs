using Unity.Entities;
using UnityEngine;

class DestroyedDropAuthoring : MonoBehaviour
{
    class DestroyedDropAuthoringBaker : Baker<DestroyedDropAuthoring>
    {
        public override void Bake(DestroyedDropAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            float uniformScale = authoring.transform.localScale.x;
            // Dodajemy komponent danych kropelki
            AddComponent(entity, new DestroyedDrop());
            AddComponent(entity, new BaseScale { Value = authoring.transform.localScale });
        }
    }
}


