using Unity.Entities;
using UnityEngine;

public class WeaponSpawerAuthoring : MonoBehaviour
{
    [Header("Basic")]
    public GameObject PistolPrefab;
    public GameObject ShotgunPrefab;

    [Header("Rifles & Snipers")]
    public GameObject AK47Prefab;
    public GameObject M4A1Prefab;
    public GameObject AWPPrefab;
    public GameObject PKMPrefab;

    [Header("SMGs & Others")]
    public GameObject MP5Prefab;
    public GameObject UziPrefab;
    public GameObject GunPrefab;

    public class Baker : Baker<WeaponSpawerAuthoring>
    {
        public override void Bake(WeaponSpawerAuthoring authoring)
        {
            // U¿ywamy None dla Singletona, bo on sam nie musi mieæ pozycji w œwiecie
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new WeaponResources
            {
                Pistol = GetEntity(authoring.PistolPrefab, TransformUsageFlags.Dynamic),
                Shotgun = GetEntity(authoring.ShotgunPrefab, TransformUsageFlags.Dynamic),
                ak47 = GetEntity(authoring.AK47Prefab, TransformUsageFlags.Dynamic),
                m4a1 = GetEntity(authoring.M4A1Prefab, TransformUsageFlags.Dynamic),
                mp5 = GetEntity(authoring.MP5Prefab, TransformUsageFlags.Dynamic),
                uzi = GetEntity(authoring.UziPrefab, TransformUsageFlags.Dynamic),
                gun = GetEntity(authoring.GunPrefab, TransformUsageFlags.Dynamic),
                awp = GetEntity(authoring.AWPPrefab, TransformUsageFlags.Dynamic),
                PKM = GetEntity(authoring.PKMPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}