/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public T? Fetch<T>(string url)
    {
        try
        {
            using HttpClient client = new();
            HttpResponseMessage response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            string jsonContent = response.Content.ReadAsStringAsync().Result;
            T? data = JsonConvert.DeserializeObject<T>(jsonContent);
            return data;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error fetching data from {url}: {ex.Message}");
            return default;
        }
    }

    public void FetchPlayerInventory(ulong steamId, bool force = false)
    {
        if (!force && g_PlayerInventory.ContainsKey(steamId))
        {
            return;
        }

        // Reserves the inventory for the player in the dictionary.
        g_PlayerInventory[steamId] = new PlayerInventory();

        var playerInventory = Fetch<Dictionary<string, object>>(
            $"{InvSimCvar.Value}/api/equipped/{steamId}.json"
        );
        if (playerInventory != null)
        {
            g_PlayerInventory[steamId] = new PlayerInventory(playerInventory);
        }
    }
}
