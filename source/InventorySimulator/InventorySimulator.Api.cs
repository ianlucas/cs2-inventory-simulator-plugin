/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public string GetApiUrl(string pathname = "")
    {
        return $"{Config.Invsim_protocol}://{Config.Invsim_hostname}{pathname}";
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
        if (!force && PlayerInventoryManager.ContainsKey(steamId))
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

    public async void SendStatTrakIncrement(ulong userId, int targetUid)
    {
        if (Config.Invsim_apikey == "")
            return;

        await Send($"/api/increment-item-stattrak", new
        {
            apiKey = Config.Invsim_apikey,
            targetUid,
            userId = userId.ToString()
        });
    }
}
