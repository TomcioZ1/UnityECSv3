using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class BloodDropAuthoring : MonoBehaviour
{
    class BloodDropAuthoringBaker : Baker<BloodDropAuthoring>
    {
        public override void Bake(BloodDropAuthoring authoring)
        {
            // Rejestrujemy kropelkê jako obiekt dynamiczny (bêdzie siê ruszaæ)
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Dodajemy komponent danych
            AddComponent(entity, new BloodDrop());
        }
    }
}

public struct BloodDrop : IComponentData
{
    public float3 Velocity;      // Prêdkoœæ lotu
    public float RemainingLife;  // Czas do znikniêcia
}