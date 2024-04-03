/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace InventorySimulator;

public partial class InventorySimulator
{
    private readonly HashSet<ulong> g_FetchInProgress = new();

    public async Task<T?> Fetch<T>(string url)
    {
        try
        {
            using HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonContent = response.Content.ReadAsStringAsync().Result;
            T? data = JsonConvert.DeserializeObject<T>(jsonContent);
            return data;
        }
        catch (Exception error)
        {
            Logger.LogError($"Error fetching data from {url}: {error.Message}");
            return default;
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
            $"{InvSimProtocolCvar.Value}://{InvSimCvar.Value}/api/equipped/v2/{steamId}.json"
        );

        if (playerInventory != null)
        {
            g_PlayerInventory.Add(steamId, playerInventory);
        }

        g_FetchInProgress.Remove(steamId);
    }
}
