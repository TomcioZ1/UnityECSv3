using Unity.Entities;
using UnityEngine;

class BulletSpawnTimerAuthoring : MonoBehaviour
{
    class BulletSpawnTimerAuthoringBaker : Baker<BulletSpawnTimerAuthoring>
    {
        public override void Bake(BulletSpawnTimerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BulletSpawnTimer {
                Timer = 0f,
                AddedTime = 0.5f
            });
        }
    }
}


