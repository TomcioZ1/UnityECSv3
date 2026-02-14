// Dodaj to do klasy ProjectilePrefabAuthoring lub stwórz now¹ dla samej kuli
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public class ProjectileAuthoring : MonoBehaviour
{
    class Baker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Dodajemy komponent pocisku z domyœlnymi wartoœciami
            // Dziêki temu Instantiate w systemie bêdzie mia³o co nadpisaæ
            AddComponent(entity, new ProjectileComponent());
            //AddComponent<DisableRendering>(entity);
        }
    }
}