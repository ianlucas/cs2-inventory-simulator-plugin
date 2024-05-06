/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public readonly FakeConVar<string> invsim_protocol = new("invsim_protocol", "Inventory Simulator API's protocol.", "https");
    public readonly FakeConVar<string> invsim_hostname = new("invsim_hostname", "Inventory Simulator API's hostname.", "inventory.cstrike.app");
    public readonly FakeConVar<string> invsim_apikey = new("invsim_apikey", "Inventory Simulator API's key.", "");

    public readonly HashSet<ulong> FetchingPlayerInventory = new();

    public string GetApiUrl(string pathname = "")
    {
        return $"{invsim_protocol.Value}://{invsim_hostname.Value}{pathname}";
    }

    public async Task<T?> Fetch<T>(string pathname, bool rethrow = false)
    {
        try
        {
            using HttpClient client = new();
            var response = await client.GetAsync(GetApiUrl(pathname));
            response.EnsureSuccessStatusCode();

            string jsonContent = response.Content.ReadAsStringAsync().Result;
            T? data = JsonConvert.DeserializeObject<T>(jsonContent);
            return data;
        }
        catch (Exception error)
        {
            Logger.LogError($"GET {pathname} failed: {error.Message}");
            if (rethrow) throw;
            return default;
        }
    }

    public async Task Send(string pathname, object data)
    {
        try
        {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpClient client = new();
            var response = await client.PostAsync(GetApiUrl(pathname), content);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                Logger.LogError($"POST {pathname} failed, check your invsim_apikey's value.");
        }
        catch (Exception error)
        {
            Logger.LogError($"POST {pathname} failed: {error.Message}");
        }
    }

    public async Task FetchPlayerInventory(ulong steamId, bool force = false)
    {
        if (!force && InventoryManager.ContainsKey(steamId))
            return;

        if (FetchingPlayerInventory.Contains(steamId))
            return;

        FetchingPlayerInventory.Add(steamId);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var playerInventory = await Fetch<PlayerInventory>(
                    $"/api/equipped/v3/{steamId}.json", true
                );

                if (playerInventory != null)
                {
                    PlayerCooldownManager[steamId] = Now();
                    AddPlayerInventory(steamId, playerInventory);
                }

                break;
            }
            catch
            {
                // Try again to fetch data (up to 3 times).
            }
        }
        
        FetchingPlayerInventory.Remove(steamId);
    }

    public async void RefreshPlayerInventory(CCSPlayerController player, bool force = false)
    {
        if (!force)
        {
            await FetchPlayerInventory(player.SteamID);
            return;
        }

        await FetchPlayerInventory(player.SteamID, true);
        Server.NextFrame(() =>
        {
            player.PrintToChat(Localizer["invsim.ws_completed"]);
            GiveOnRefreshPlayerInventory(player);
        });
    }

    public async void SendStatTrakIncrease(ulong userId, int targetUid)
    {
        if (invsim_apikey.Value == "")
            return;

        await Send($"/api/increment-item-stattrak", new
        {
            apiKey = invsim_apikey.Value,
            targetUid,
            userId = userId.ToString()
        });
    }
}
