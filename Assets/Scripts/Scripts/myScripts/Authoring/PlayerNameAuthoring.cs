using Unity.Entities;
using UnityEngine;
using Unity.Collections;

public class PlayerNameAuthoring : MonoBehaviour
{
    public string defaultName = "Player";

    class Baker : Baker<PlayerNameAuthoring>
    {
        public override void Bake(PlayerNameAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerName
            {
                Value = new FixedString64Bytes(authoring.defaultName)
            });
        }
    }
}