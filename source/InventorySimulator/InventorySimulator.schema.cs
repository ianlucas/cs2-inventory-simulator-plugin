/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;

namespace InventorySimulator;

class InventorySimulator_CCSPlayerController : CBasePlayerController
{
    public InventorySimulator_CCSPlayerController(IntPtr pointer) : base(pointer) { }

    // m_iMusicKitID
    [SchemaMember("CCSPlayerController", "m_iMusicKitID")]
    public ref Int32 MusicKitID => ref Schema.GetRef<Int32>(this.Handle, "CCSPlayerController", "m_iMusicKitID");

    // m_iMusicKitMVPs
    [SchemaMember("CCSPlayerController", "m_iMusicKitMVPs")]
    public ref Int32 MusicKitMVPs => ref Schema.GetRef<Int32>(this.Handle, "CCSPlayerController", "m_iMusicKitMVPs");

    // m_iMVPs
    [SchemaMember("CCSPlayerController", "m_iMVPs")]
    public ref Int32 MVPs => ref Schema.GetRef<Int32>(this.Handle, "CCSPlayerController", "m_iMVPs");
}
