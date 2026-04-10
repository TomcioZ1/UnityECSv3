using UnityEngine;
using Unity.Entities;

public class HandsSpawerAuthoring : MonoBehaviour
{
    public GameObject LeftHandPrefab;
    public GameObject RightHandPrefab;

    public class Baker : Baker<HandsSpawerAuthoring>
    {
        public override void Bake(HandsSpawerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new HandsResources
            {
                // Wa¿ne: GetEntity zamienia Prefab GameObject na Entity
                LeftHand = GetEntity(authoring.LeftHandPrefab, TransformUsageFlags.Dynamic),
                RightHand = GetEntity(authoring.RightHandPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}