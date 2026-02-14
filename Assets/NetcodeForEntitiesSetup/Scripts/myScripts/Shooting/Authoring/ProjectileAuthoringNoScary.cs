using Unity.Entities;
using UnityEngine;

class ProjectileAuthoringNoScary : MonoBehaviour
{
    class ProjectileAuthoringNoScaryBaker : Baker<ProjectileAuthoringNoScary>
    {
        public override void Bake(ProjectileAuthoringNoScary authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new VisualProjectile());
        }
    }
}


