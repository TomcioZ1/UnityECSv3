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
            // Skala r¹k
            // 1. SKALA: Pobieramy localScale (x, y, z) bezpoœrednio z GameObjectu
            AddComponent(entity, new BaseScale { Value = authoring.transform.localScale });

            // 2. SKALA NIEJEDNOLITA: Wymagane, aby X, Y i Z mog³y mieæ ró¿ne wartoœci w ECS
            AddComponent<PostTransformMatrix>(entity);

            // Musisz dodaæ te komponenty tutaj, aby broñ mog³a byæ "dzieckiem" socketu
            AddComponent<LocalTransform>(entity);
            AddComponent<Parent>(entity);

            AddComponent<HandsOwner>(entity);
            AddComponent<GhostAuthoringComponent>(entity);
            //AddComponent<GhostOwner>(entity);
        }
    }
}