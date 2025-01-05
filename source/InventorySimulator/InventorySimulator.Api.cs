/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Net;
using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace InventorySimulator;

public partial class InventorySimulator
{
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
            T? data = JsonSerializer.Deserialize<T>(jsonContent);
            return data;
        }
        catch (Exception error)
        {
            Logger.LogError("GET {Pathname} failed: {Message}", pathname, error.Message);
            if (rethrow)
                throw;
            return default;
        }
    }

    public async Task Send(string pathname, object data)
    {
        try
        {
            var url = GetApiUrl(pathname);
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpClient client = new();
            var response = await client.PostAsync(url, content);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                Logger.LogError("POST {Url} failed, check your invsim_apikey's value.", url);
        }
        catch (Exception error)
        {
            Logger.LogError("POST {Pathname} failed: {Message}", pathname, error.Message);
        }
    }

    public async Task FetchPlayerInventory(ulong steamId, bool force = false)
    {
        var existing = PlayerInventoryManager.TryGetValue(steamId, out var i) ? i : null;

        if (!force && existing != null)
            return;

        if (FetchingPlayerInventory.Contains(steamId))
            return;

        FetchingPlayerInventory.Add(steamId);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var playerInventory = await Fetch<PlayerInventory>(
                    $"/api/equipped/v3/{steamId}.json",
                    true
                );

                if (playerInventory != null)
                {
                    if (existing != null)
                        playerInventory.CachedWeaponEconItems = existing.CachedWeaponEconItems;
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
        if (invsim_apikey.Value == "")
            return;

        await Send(
            $"/api/increment-item-stattrak",
            new
            {
                apiKey = invsim_apikey.Value,
                targetUid,
                userId = userId.ToString(),
            }
        );
    }
}
