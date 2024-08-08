/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public void LoadPlayerInventories()
    {
        try
        {
            var path = Path.Combine(Server.GameDirectory, InventoryFilePath);
            if (!File.Exists(path))
                return;

            string json = File.ReadAllText(path);
            var inventories = JsonSerializer.Deserialize<Dictionary<ulong, PlayerInventory>>(json);
            if (inventories != null)
            {
                foreach (var pair in inventories)
                {
                    LoadedPlayerInventory.Add(pair.Key);
                    AddPlayerInventory(pair.Key, pair.Value);
                }
            }
        }
        catch
        {
            Logger.LogError($"Error when processing \"inventories.json\".");
        }
    }

    public void AddPlayerInventory(ulong steamId, PlayerInventory inventory)
    {
        PlayerInventoryManager[steamId] = inventory;
        Server.NextFrame(() =>
        {
            if (inventory.MusicKit != null)
                PlayerOnTickInventoryManager[steamId] = (Utilities.GetPlayerFromSteamId(steamId), inventory);
            else PlayerOnTickInventoryManager.Remove(steamId);
        });
    }

    public void ClearInventoryManager()
    {
        var connected = Utilities.GetPlayers().Select(player => player.SteamID).ToHashSet();
        var disconnected = PlayerInventoryManager.Keys.Except(connected).ToList();
        foreach (var steamId in disconnected)
        {
            RemovePlayerInventory(steamId);
        }
    }

    public void RemovePlayerInventory(ulong steamId)
    {
        if (!LoadedPlayerInventory.Contains(steamId))
        {
            PlayerInventoryManager.Remove(steamId);
            PlayerOnTickInventoryManager.Remove(steamId);
        }
        if (PlayerOnTickInventoryManager.TryGetValue(steamId, out var tuple))
        {
            PlayerOnTickInventoryManager[steamId] = (null, tuple.Item2);
        }
    }

    public PlayerInventory GetPlayerInventory(CCSPlayerController player)
    {
        if (PlayerInventoryManager.TryGetValue(player.SteamID, out var inventory))
        {
            return inventory;
        }
        return EmptyInventory;
    }
}
