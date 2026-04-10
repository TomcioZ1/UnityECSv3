using Unity.Entities;
using UnityEngine;

class PerformanceStatsAuthoring : MonoBehaviour
{
    class Baker : Baker<PerformanceStatsAuthoring>
    {
        public override void Bake(PerformanceStatsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PerformanceStats());
        }
    }
}