/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json;
using System.Text.Json;

namespace InventorySimulator;

public class StickerItem
{
    [JsonProperty("def")]
    public uint Def { get; set; }

    [JsonProperty("slot")]
    public ushort Slot { get; set; }

    [JsonProperty("wear")]
    public float Wear { get; set; }
}

public class BaseEconItem
{
    [JsonProperty("def")]
    public ushort Def { get; set; }

    [JsonProperty("paint")]
    public int Paint { get; set; }

    [JsonProperty("seed")]
    public int Seed { get; set; }

    [JsonProperty("wear")]
    public float Wear { get; set; }
}

public class WeaponEconItem : BaseEconItem
{
    [JsonProperty("legacy")]
    public bool Legacy { get; set; }

    [JsonProperty("nametag")]
    public required string Nametag { get; set; }

    [JsonProperty("stattrak")]
    public required int Stattrak { get; set; }

    [JsonProperty("stickers")]
    public required List<StickerItem> Stickers { get; set; }

    [JsonProperty("uid")]
    public required int Uid { get; set; }
}

public class AgentItem
{
    [JsonProperty("model")]
    public required string Model { get; set; }

    [JsonProperty("patches")]
    public required List<uint> Patches { get; set; }

    [JsonProperty("vofallback")]
    public required bool VoFallback { get; set; }

    [JsonProperty("vofemale")]
    public required bool VoFemale { get; set; }

    [JsonProperty("voprefix")]
    public required string VoPrefix { get; set; }
}

public class MusicKitItem
{
    [JsonProperty("def")]
    public int Def { get; set; }

    [JsonProperty("stattrak")]
    public required int Stattrak { get; set; }

    [JsonProperty("uid")]
    public required int Uid { get; set; }
}

public class PlayerInventory
{
    [JsonProperty("knives")]
    public Dictionary<byte, WeaponEconItem> Knives { get; set; }

    [JsonProperty("gloves")]
    public Dictionary<byte, BaseEconItem> Gloves { get; set; }

    [JsonProperty("tWeapons")]
    public Dictionary<ushort, WeaponEconItem> TWeapons { get; set; }

    [JsonProperty("ctWeapons")]
    public Dictionary<ushort, WeaponEconItem> CTWeapons { get; set; }

    [JsonProperty("agents")]
    public Dictionary<byte, AgentItem> Agents { get; set; }

    [JsonProperty("pin")]
    public uint? Pin { get; set; }

    [JsonProperty("musicKit")]
    public MusicKitItem? MusicKit { get; set; }

    [JsonConstructor]
    public PlayerInventory(
        Dictionary<byte, WeaponEconItem>? knives = null,
        Dictionary<byte, BaseEconItem>? gloves = null,
        Dictionary<ushort, WeaponEconItem>? tWeapons = null,
        Dictionary<ushort, WeaponEconItem>? ctWeapons = null,
        Dictionary<byte, AgentItem>? agents = null,
        uint? pin = null,
        MusicKitItem? musicKit = null)
    {
        Knives = knives ?? [];
        Gloves = gloves ?? [];
        TWeapons = tWeapons ?? [];
        CTWeapons = ctWeapons ?? [];
        Agents = agents ?? [];
        Pin = pin;
        MusicKit = musicKit;
    }

    public WeaponEconItem? GetKnife(byte team)
    {
        if (Knives.TryGetValue(team, out var knife))
        {
            return knife;
        }
        return null;
    }

    public WeaponEconItem? GetWeapon(CsTeam team, ushort def)
    {
        if ((team == CsTeam.Terrorist ? TWeapons : CTWeapons).TryGetValue(def, out var weapon))
        {
            return weapon;
        }
        return null;
    }
}
