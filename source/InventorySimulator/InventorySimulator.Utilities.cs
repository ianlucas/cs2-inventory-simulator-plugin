/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using NativeVector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace InventorySimulator;

public partial class InventorySimulator
{
    static CCSGameRulesProxy? GameRulesProxy;

    public static string GetAgentModelPath(string model) => $"characters/models/{model}.vmdl";

    public static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static CsTeam ToggleTeam(CsTeam team) => team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;

    public static float ViewAsFloat<T>(T value)
        where T : struct
    {
        byte[] bytes = value switch
        {
            int intValue => BitConverter.GetBytes(intValue),
            uint uintValue => BitConverter.GetBytes(uintValue),
            _ => throw new ArgumentException("Unsupported type"),
        };
        return BitConverter.ToSingle(bytes, 0);
    }

    public static NativeVector Vector3toVector(Vector3 vec) => new(vec.X, vec.Y, vec.Z);

    public static CCSGameRules GetGameRules() =>
        (
            GameRulesProxy?.IsValid == true ? GameRulesProxy.GameRules
            : (GameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First())?.IsValid == true ? GameRulesProxy?.GameRules
            : null
        ) ?? throw new Exception("Game rules not found.");
}
