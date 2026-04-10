using Unity.Entities;
using UnityEngine;

public class LeaderboardAuthoring : MonoBehaviour
{
    class Baker : Baker<LeaderboardAuthoring>
    {
        public override void Bake(LeaderboardAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<LeaderboardTag>(entity);
            // Dodajemy pusty bufor elementów
            AddBuffer<LeaderboardElement>(entity);
        }
    }
}

public struct LeaderboardTag : IComponentData { }