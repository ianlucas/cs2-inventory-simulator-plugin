/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public readonly FakeConVar<string> InvSimProtocolCvar = new("css_inventory_simulator_protocol", "Inventory Simulator API's protocol.", "https");
    public readonly FakeConVar<string> InvSimCvar = new("css_inventory_simulator", "Inventory Simulator API's domain.", "inventory.cstrike.app");
    public readonly FakeConVar<string> InvSimApiKeyCvar = new("css_inventory_simulator_apikey", "Inventory Simulator API's key.", "");

    public readonly HashSet<ulong> FetchingInventory = new();

    public string GetApiUrl(string uri)
    {
        return $"{InvSimProtocolCvar.Value}://{InvSimCvar.Value}{uri}";
    }

    public async Task<T?> Fetch<T>(string uri, bool shouldThrow = false)
    {
        try
        {
            using HttpClient client = new();
            var response = await client.GetAsync(GetApiUrl(uri));
            response.EnsureSuccessStatusCode();

            string jsonContent = response.Content.ReadAsStringAsync().Result;
            T? data = JsonConvert.DeserializeObject<T>(jsonContent);
            return data;
        }
        catch (Exception error)
        {
            Logger.LogError($"GET {uri} failed: {error.Message}");
            if (shouldThrow) throw;
            return default;
        }
    }

    public async Task Send(string uri, object data)
    {
        try
        {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpClient client = new();
            var response = await client.PostAsync(GetApiUrl(uri), content);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                Logger.LogError($"POST {uri} failed, check your css_inventory_simulator_apikey's value.");
        }
        catch (Exception error)
        {
            Logger.LogError($"POST {uri} failed: {error.Message}");
        }
    }

    public async void FetchPlayerInventory(ulong steamId, bool force = false)
    {
        if (!force && InventoryManager.ContainsKey(steamId))
            return;

        if (FetchingInventory.Contains(steamId))
            return;

        FetchingInventory.Add(steamId);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var playerInventory = await Fetch<PlayerInventory>(
                    $"/api/equipped/v3/{steamId}.json", true
                );

                if (playerInventory != null)
                {
                    AddPlayerInventory(steamId, playerInventory);
                }

                FetchingInventory.Remove(steamId);
                return;
            }
            catch
            {
                // Try again to fetch data (up to 3 times).
            }
        }
    }

    public async void SendStatTrakIncrease(ulong userId, int targetUid)
    {
        if (InvSimApiKeyCvar.Value == "")
            return;

        await Send($"/api/increment-item-stattrak", new
        {
            apiKey = InvSimApiKeyCvar.Value,
            targetUid,
            userId = userId.ToString()
        });
    }
}
