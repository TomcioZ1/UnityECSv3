using Unity.Entities;
using UnityEngine;

class LeftButtonHistoryAuthoring : MonoBehaviour
{
    class LeftButtonHistoryAuthoringBaker : Baker<LeftButtonHistoryAuthoring>
    {
        public override void Bake(LeftButtonHistoryAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new LeftButtonBefore
            {
                wasHeld = false
            });
        }
    }
}


