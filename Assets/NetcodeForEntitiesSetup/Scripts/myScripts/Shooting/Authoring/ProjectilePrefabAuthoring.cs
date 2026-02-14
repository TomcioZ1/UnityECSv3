using Unity.Entities;
using UnityEngine;

public class ProjectilePrefabAuthoring : MonoBehaviour
{
    public GameObject ProjectilePrefab;
    class Baker : Baker<ProjectilePrefabAuthoring>
    {
        public override void Bake(ProjectilePrefabAuthoring auth)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ProjectilePrefab { Value = GetEntity(auth.ProjectilePrefab, TransformUsageFlags.Dynamic) });
        }
    }
}