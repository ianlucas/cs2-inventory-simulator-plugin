using CounterStrikeSharp.API.Core;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public required InventorySimulatorConfig Config { get; set; }

    public void OnConfigParsed(InventorySimulatorConfig config)
    {
        if (config.Version != ConfigVersion) throw new Exception($"You have a wrong config version. Delete it and restart the server to get the right version ({ConfigVersion})!");

        if (config.Invsim_minmodels < 0 || config.Invsim_minmodels > 2)
            throw new Exception($"Invsim_minmodels must be 0,1 or 2");

        Config = config;
    }

}
public class InventorySimulatorConfig : BasePluginConfig
{
    public override int Version { get; set; } = 1;
    public bool Invsim_stattrak_ignore_bots { get; set; } = true;
    public bool Invsim_ws_enabled { get; set; } = false;
    public int Invsim_minmodels { get; set; } = 0;
    public int Invsim_ws_cooldown { get; set; } = 30;
    public string Invsim_apikey { get; set; } = "";
    public string Invsim_hostname { get; set; } = "inventory.cstrike.app";
    public string Invsim_protocol { get; set; } = "https";
}
