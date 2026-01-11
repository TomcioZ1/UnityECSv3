using Unity.Entities;
using UnityEngine;
using Unity.Collections;

[DisallowMultipleComponent]
public class PlayerNameAuthoring : MonoBehaviour
{
    [SerializeField] private string defaultName = "Player";

    class Baker : Baker<PlayerNameAuthoring>
    {
        public override void Bake(PlayerNameAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Dodajemy PlayerName z domyœln¹ wartoœci¹
            AddComponent(entity, new PlayerName
            {
                Value = new FixedString64Bytes(authoring.defaultName)
            });
        }
    }
}
