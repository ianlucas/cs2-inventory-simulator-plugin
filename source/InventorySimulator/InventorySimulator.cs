/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace InventorySimulator;

[MinimumApiVersion(175)]
public partial class InventorySimulator : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "Inventory Simulator (inventory.cstrike.app)";
    public override string ModuleName => "InventorySimulator";
    public override string ModuleVersion => "0.0.5";

    private readonly Dictionary<ulong, PlayerInventory> g_PlayerInventory = new();
    private ulong g_ItemId = UInt64.MaxValue - 32768;

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(OnTick);
        VirtualFunctions.GiveNamedItemFunc.Hook(OnGiveNamedItemPost, HookMode.Post);
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (!IsPlayerHumanAndValid(player))
            return HookResult.Continue;

        var steamId = player.SteamID;

        Task.Run(async () =>
        {
            await FetchPlayerInventory(steamId);
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (!IsPlayerHumanAndValid(player) || !IsPlayerPawnValid(player))
            return HookResult.Continue;

        GivePlayerMusicKit(player);
        GivePlayerAgent(player);
        GivePlayerGloves(player);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (IsPlayerHumanAndValid(player))
        {
            g_PlayerInventory.Remove(player.SteamID);
        }

        return HookResult.Continue;
    }

    public void OnTick()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            try
            {
                if (!IsPlayerHumanAndValid(player)) continue;

                var viewModel = GetPlayerViewModel(player);
                if (viewModel == null || viewModel.Weapon.Value == null) continue;
                if (viewModel.VMName.Contains("knife")) continue;

                CBasePlayerWeapon weapon = viewModel.Weapon.Value;
                if (weapon == null || !weapon.IsValid) continue;

                var inventory = GetPlayerInventory(player);
                var itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
                var team = player.TeamNum;

                if (!inventory.HasProperty("pa", team, itemDef)) continue;

                // Looks like changing the weapon's MeshGroupMask during creation is not enough, so we
                // update the view model's skeleton as well.
                if (UpdateWeaponModel(viewModel, inventory.HasProperty("pal", team, itemDef)))
                {
                    Utilities.SetStateChanged(viewModel, "CBaseEntity", "m_CBodyComponent");
                }
            }
            catch (Exception)
            { }
        }
    }

    public HookResult OnGiveNamedItemPost(DynamicHook hook)
    {
        var itemServices = hook.GetParam<CCSPlayer_ItemServices>(0);
        var weapon = hook.GetReturn<CBasePlayerWeapon>(0);
        if (!weapon.DesignerName.Contains("weapon"))
            return HookResult.Continue;

        var player = GetPlayerFromItemServices(itemServices);
        if (player != null)
            GivePlayerWeaponSkin(player, weapon);

        return HookResult.Continue;
    }
}
