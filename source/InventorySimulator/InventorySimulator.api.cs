/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace InventorySimulator;

public partial class InventorySimulator
{
    private readonly HashSet<ulong> g_FetchInProgress = new();

    public string GetApiUrl(string uri)
    {
        return $"{InvSimProtocolCvar.Value}://{InvSimCvar.Value}{uri}";
    }

    public async Task<T?> Fetch<T>(string uri)
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
            return default;
        }
    }

    public async Task Send(string uri, object data)
    {
        var json = JsonConvert.SerializeObject(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        try
        {
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
        if (!force && g_PlayerInventory.ContainsKey(steamId))
            return;

        if (g_FetchInProgress.Contains(steamId))
            return;

        g_FetchInProgress.Add(steamId);

        var playerInventory = await Fetch<PlayerInventory>(
            $"/api/equipped/v2/{steamId}.json"
        );

        if (playerInventory != null)
        {
            g_PlayerInventory.Add(steamId, playerInventory);
        }

        g_FetchInProgress.Remove(steamId);
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
