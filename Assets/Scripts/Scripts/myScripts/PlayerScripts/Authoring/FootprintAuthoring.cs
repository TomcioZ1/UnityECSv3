using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using MaterialPropertyBaseColor = Unity.Rendering.URPMaterialPropertyBaseColor;

public class FootprintAuthoring : MonoBehaviour
{
    public float FootpringlifeTime = 1f; // Czas ¿ycia œladu, mo¿na go ustawiæ w Inspectorze
    class Baker : Baker<FootprintAuthoring>
    {
        public override void Bake(FootprintAuthoring authoring)
        {
            // U¿ywamy TransformUsageFlags.Dynamic, bo œlady s¹ spawnowane w ró¿nych miejscach
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // Dodajemy komponent czasu ¿ycia bezpoœrednio do prefaba
            AddComponent(entity, new FootprintLifeTime
            {
                Value = authoring.FootpringlifeTime,
                MaxValue = authoring.FootpringlifeTime
            });
            //AddComponent(entity, new MaterialPropertyBaseColor { Value = 1 });
        }
    }
}

