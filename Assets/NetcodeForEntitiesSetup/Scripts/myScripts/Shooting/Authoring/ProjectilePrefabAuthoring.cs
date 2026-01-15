using Unity.Entities;
using UnityEngine;

public class ProjectilePrefabAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    class Baker : Baker<ProjectilePrefabAuthoring>
    {
        public override void Bake(ProjectilePrefabAuthoring auth)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ProjectilePrefab { Value = GetEntity(auth.Prefab, TransformUsageFlags.Dynamic) });
        }
    }
}