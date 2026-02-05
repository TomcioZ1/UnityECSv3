using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class WaterDropAuthoring : MonoBehaviour
{
    class Baking : Baker<WaterDropAuthoring>
    {
        public override void Bake(WaterDropAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            // Dodajemy komponent danych kropelki
            AddComponent(entity, new WaterDrop());
        }
    }
}

public struct WaterDrop : IComponentData
{
    public float3 Velocity;      // Kierunek i si³a lotu
    public float RemainingLife;  // Czas do znikniêcia
}