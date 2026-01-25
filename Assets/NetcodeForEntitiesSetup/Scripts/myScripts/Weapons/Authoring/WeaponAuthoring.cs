using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

public class WeaponAuthoringComponent : MonoBehaviour
{
    public GameObject ProjectileSpawner;
    class Baker : Baker<WeaponAuthoringComponent>
    {
        public override void Bake(WeaponAuthoringComponent authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var projectileSpawnerEntity = GetEntity(authoring.ProjectileSpawner, TransformUsageFlags.Dynamic);
            AddComponent(entity, new WeaponData
            {
                ProjectileSpawner = projectileSpawnerEntity
            });
            


            //skala
            AddComponent(entity, new BaseScale { Value = authoring.transform.localScale });

            // 2. SKALA NIEJEDNOLITA: Wymagane, aby X, Y i Z mog³y mieæ ró¿ne wartoœci w ECS
            AddComponent<PostTransformMatrix>(entity);

            // Musisz dodaæ te komponenty tutaj, aby broñ mog³a byæ "dzieckiem" socketu
            AddComponent<LocalTransform>(entity);
            AddComponent<Parent>(entity);

            AddComponent<WeaponOwner>(entity);
            AddComponent<GhostAuthoringComponent>(entity);
            AddComponent<GhostOwner>(entity);
        }
    }
}