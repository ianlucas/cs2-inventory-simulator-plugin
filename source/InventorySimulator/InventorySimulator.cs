/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;

namespace InventorySimulator;

public partial class InventorySimulator : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "Inventory Simulator (inventory.cstrike.app)";
    public override string ModuleName => "InventorySimulator";
    public override string ModuleVersion => "1.0.0";

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
        RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventRoundPrestart>(OnRoundPrestart);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        VirtualFunctions.GiveNamedItemFunc.Hook(OnGiveNamedItemPost, HookMode.Post);
        Extensions.UpdateSelectTeamPreview.Hook(OnUpdateSelectTeamPreview, HookMode.Post);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeathPre, HookMode.Pre);
        RegisterEventHandler<EventRoundMvp>(OnRoundMvpPre, HookMode.Pre);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

        invsim_file.ValueChanged += OnInvsimFileChanged;
        OnInvsimFileChanged(null, invsim_file.Value);

        invsim_require_inventory.ValueChanged += OnInvSimRequireInventoryChange;
        OnInvSimRequireInventoryChange(null, invsim_require_inventory.Value);

        invsim_compatibility_mode.ValueChanged += OnInvSimCompatibilityModeChange;
        OnInvSimCompatibilityModeChange(null, invsim_compatibility_mode.Value);

        invsim_spray_on_use.ValueChanged += OnInvSimSprayOnUseChange;
        OnInvSimSprayOnUseChange(null, invsim_spray_on_use.Value);
    }
}
