using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct ReloadUISystem : ISystem
{
    private float _localReloadStartTime;
    private bool _wasReloadingLastFrame;
    private Entity _lastWeaponEntity;

    public void OnUpdate(ref SystemState state)
    {
        if (ReloadUIController.Instance == null) return;

        if (!SystemAPI.HasSingleton<NetworkId>()) return;
        int localNetId = SystemAPI.GetSingleton<NetworkId>().Value;

        float currentTime = (float)SystemAPI.Time.ElapsedTime;
        bool shouldShowUI = false;
        float progress = 0f;

        foreach (var (inventory, ghostOwner) in
                 SystemAPI.Query<RefRO<PlayerInventory>, RefRO<GhostOwner>>())
        {
            if (ghostOwner.ValueRO.NetworkId != localNetId) continue;

            Entity weaponEntity = inventory.ValueRO.CurrentWeaponEntity;
            if (weaponEntity == Entity.Null || !SystemAPI.HasComponent<WeaponData>(weaponEntity))
            {
                _wasReloadingLastFrame = false;
                continue;
            }

            var weaponData = SystemAPI.GetComponent<WeaponData>(weaponEntity);

            // G£ÓWNA LOGIKA PROGRESU
            if (weaponData.isReloading)
            {
                // 1. Jeœli to pierwsza klatka prze³adowania (lub zmieniliœmy broñ), zapisz czas startu
                if (!_wasReloadingLastFrame || _lastWeaponEntity != weaponEntity)
                {
                    _localReloadStartTime = currentTime;
                    _wasReloadingLastFrame = true;
                    _lastWeaponEntity = weaponEntity;
                }

                // 2. Oblicz up³yw czasu lokalnie
                float elapsed = currentTime - _localReloadStartTime;

                // 3. Wylicz progres (0.0 do 1.0) na podstawie czasu zapisanego w WeaponData
                progress = math.saturate(elapsed / weaponData.reloadTime);

                shouldShowUI = true;
            }
            else
            {
                // Resetuj stany, gdy serwer wy³¹czy flagê isReloading
                _wasReloadingLastFrame = false;
                _lastWeaponEntity = Entity.Null;
                progress = 0f;
                shouldShowUI = false;
            }

            // Aktualizujemy UI i wychodzimy z pêtli (mamy dane lokalnego gracza)
            ReloadUIController.Instance.UpdateProgressFromData(progress, shouldShowUI);
            break;
        }
    }
}