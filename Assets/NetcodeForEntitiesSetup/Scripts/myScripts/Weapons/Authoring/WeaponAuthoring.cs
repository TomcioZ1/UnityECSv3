using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

public class WeaponAuthoringComponent : MonoBehaviour
{
    class Baker : Baker<WeaponAuthoringComponent>
    {
        public override void Bake(WeaponAuthoringComponent authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Musisz dodaæ te komponenty tutaj, aby broñ mog³a byæ "dzieckiem" socketu
            AddComponent<LocalTransform>(entity);
            AddComponent<Parent>(entity);

            AddComponent<WeaponOwner>(entity);
            AddComponent<GhostAuthoringComponent>(entity);
            AddComponent<GhostOwner>(entity);
        }
    }
}