using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

namespace Unity.Multiplayer.Center.NetcodeForEntitiesSetup
{
    public struct Cube : IComponentData { }

    [DisallowMultipleComponent]
    public class CubeAuthoring : MonoBehaviour
    {
        public GameObject WeaponSocket;

        class CubeBaker : Baker<CubeAuthoring>
        {
            public override void Bake(CubeAuthoring authoring)
            {
                // Encja g³ównego gracza (Cube)
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<Cube>(entity);

                AddComponent(entity, new HealthComponent
                {
                    HealthPoints = 100
                });

                AddComponent(entity, new WeaponSocket
                {
                    WeaponSocketEntity = GetEntity(authoring.WeaponSocket, TransformUsageFlags.Dynamic)
                });

                AddComponent(entity, new ActiveWeapon { });


            }
        }
    }
}