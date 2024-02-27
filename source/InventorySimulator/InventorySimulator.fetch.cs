/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public async Task<T?> Fetch<T>(string url)
    {
        try
        {
            using HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonContent = await response.Content.ReadAsStringAsync();
            T? data = JsonConvert.DeserializeObject<T>(jsonContent);
            return data;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error fetching data from {url}: {ex.Message}");
            return default;
        }
    }

    public async Task FetchLookupWeaponLegacy()
    {
        var lookupWeaponLegacy = await Fetch<Dictionary<ushort, List<int>>>(
            "https://raw.githubusercontent.com/ianlucas/cslib/main/assets/data/lookup-weapon-legacy.json"
        );
        if (lookupWeaponLegacy != null)
        {
            g_LookupWeaponLegacy = lookupWeaponLegacy;
        }
    }

    public async Task FetchLookupWeaponModel()
    {
        var lookupWeaponModel = await Fetch<Dictionary<ushort, string>>(
            "https://raw.githubusercontent.com/ianlucas/cslib/main/assets/data/lookup-weapon-model.json"
        );
        if (lookupWeaponModel != null)
        {
            g_LookupWeaponModel = lookupWeaponModel;
        }
    }

    public async Task FetchLookupAgentModel()
    {
        var lookupAgentModel = await Fetch<Dictionary<ushort, string>>(
            "https://raw.githubusercontent.com/ianlucas/cslib/main/assets/data/lookup-agent-model.json"
        );
        if (lookupAgentModel != null)
        {
            foreach (var entry in lookupAgentModel)
            {
                var model = $"characters/models/{entry.Value}.vmdl";
                g_LookupAgentModel.Add(entry.Key, model);
            }
        }
    }

    public async Task FetchPlayerInventory(ulong steamId)
    {
        var playerInventory = await Fetch<Dictionary<string, object>>(
            $"https://inventory.cstrike.app/api/equipped/{steamId}.json"
        );
        if (playerInventory != null)
        {
            g_PlayerInventory[steamId] = new PlayerInventory(playerInventory);
        }
    }
}
