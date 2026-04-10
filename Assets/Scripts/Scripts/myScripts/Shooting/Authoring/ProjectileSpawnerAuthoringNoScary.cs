using Unity.Entities;
using UnityEngine;

class ProjectileSpawnerAuthoringNoScary : MonoBehaviour
{
    public GameObject ProjectilePrefabNoScary;
    class ProjectileSpawnerAuthoringNoScaryBaker : Baker<ProjectileSpawnerAuthoringNoScary>
    {
        public override void Bake(ProjectileSpawnerAuthoringNoScary authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ProjectilePrefabNoScary { Value = GetEntity(authoring.ProjectilePrefabNoScary, TransformUsageFlags.Dynamic) });
        }
    }
}


