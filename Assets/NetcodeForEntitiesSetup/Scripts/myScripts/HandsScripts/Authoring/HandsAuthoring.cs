using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

class HandsAuthoring : MonoBehaviour
{
    class HandsAuthoringBaker : Baker<HandsAuthoring>
    {
        public override void Bake(HandsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Musisz dodaæ te komponenty tutaj, aby broñ mog³a byæ "dzieckiem" socketu
            AddComponent<LocalTransform>(entity);
            AddComponent<Parent>(entity);

            AddComponent<HandsOwner>(entity);
            AddComponent<GhostAuthoringComponent>(entity);
            AddComponent<GhostOwner>(entity);
        }
    }
}