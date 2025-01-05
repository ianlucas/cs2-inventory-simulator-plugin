/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public void LoadPlayerInventories()
    {
        try
        {
            var path = Path.Combine(Server.GameDirectory, InventoryFileDir, invsim_file.Value);
            if (!File.Exists(path))
                return;

            string json = File.ReadAllText(path);
            var inventories = JsonSerializer.Deserialize<Dictionary<ulong, PlayerInventory>>(json);
            if (inventories != null)
            {
                LoadedPlayerInventory.Clear();
                foreach (var pair in inventories)
                {
                    LoadedPlayerInventory.Add(pair.Key);
                    AddPlayerInventory(pair.Key, pair.Value);
                }
            }
        }
        catch
        {
            Logger.LogError($"Error when processing \"{invsim_file.Value}\".");
        }
    }

    public void AddPlayerInventory(ulong steamId, PlayerInventory inventory)
    {
        PlayerInventoryManager[steamId] = inventory;
        Server.NextFrame(() =>
        {
            var player = Utilities.GetPlayerFromSteamId(steamId);
            if (inventory.MusicKit != null)
                PlayerOnTickInventoryManager[steamId] = (player, inventory);
            else
                PlayerOnTickInventoryManager.Remove(steamId, out _);
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
            PlayerInventoryManager.Remove(steamId, out _);
            PlayerCooldownManager.Remove(steamId, out _);
            PlayerSprayCooldownManager.Remove(steamId, out _);
            PlayerOnTickInventoryManager.Remove(steamId, out _);
        }
        if (PlayerOnTickInventoryManager.TryGetValue(steamId, out var tuple))
        {
            PlayerOnTickInventoryManager[steamId] = (null, tuple.Item2);
        }
    }

    public void ClearPlayerUseCmd(ulong steamId)
    {
        PlayerUseCmdManager.Remove(steamId, out var _);
        PlayerUseCmdBlockManager.Remove(steamId, out var _);
    }

    public void ClearPlayerServerSideClient(int? userid)
    {
        var key = ServerSideClientUserid.FirstOrDefault(other => other.Value == userid).Key;
        if (key != default)
            ServerSideClientUserid.TryRemove(key, out _);
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
