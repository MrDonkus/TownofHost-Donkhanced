﻿using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Glow
{
    private const int Id = 22000;
    public static bool IsEnable = false;

    public static OptionItem ImpCanBeGlow;
    public static OptionItem CrewCanBeGlow;
    public static OptionItem NeutralCanBeGlow;
    private static OptionItem GlowRadius;
    private static OptionItem GlowVisionOthers;
    private static OptionItem GlowVisionSelf;

    private static Dictionary<byte, HashSet<byte>> InRadius = [];
    private static Dictionary<byte, bool> MarkedOnce = [];

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Glow, canSetNum: true, tab: TabGroup.OtherRoles);
        ImpCanBeGlow = BooleanOptionItem.Create(Id + 10, "ImpCanBeGlow", true, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Glow]);
        CrewCanBeGlow = BooleanOptionItem.Create(Id + 11, "CrewCanBeGlow", true, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Glow]);
        NeutralCanBeGlow = BooleanOptionItem.Create(Id + 12, "NeutralCanBeGlow", true, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Glow]);
        GlowRadius = FloatOptionItem.Create(Id + 13, "GlowRadius", new(0.1f, 5f, 0.05f), 0.5f, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Glow])
            .SetValueFormat(OptionFormat.Multiplier);
        GlowVisionOthers = FloatOptionItem.Create(Id + 14, "GlowVisionOthers", new(0.1f, 5f, 0.05f), 0.15f, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Glow])
            .SetValueFormat(OptionFormat.Multiplier);
        GlowVisionSelf = FloatOptionItem.Create(Id + 15, "GlowVisionSelf", new(0.1f, 5f, 0.05f), 0.15f, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Glow])
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public static void Init()
    {
        InRadius.Clear();
        IsEnable = false;
        MarkedOnce.Clear();
    }
    public static void Add(byte playerId)
    {
        MarkedOnce[playerId] = false;
        InRadius[playerId] = [];
        IsEnable = true;
    }
    public static void Remove(byte playerId)
    {
        MarkedOnce.Remove(playerId);
        InRadius.Remove(playerId);
    }
    public static void ApplyGameOptions(IGameOptions opt, PlayerControl player)
    {
        if (!InRadius.Any() || player == null) return;
        if (!Utils.IsActive(SystemTypes.Electrical)) return;

        if (!player.Is(CustomRoles.Glow))
        {    HashSet<byte> affectedPlaters = [];
            foreach (var allSets in InRadius.Values)
                affectedPlaters.UnionWith(allSets);

            if (!affectedPlaters.Contains(player.PlayerId)) return;
        }

        opt.SetVision(false);
        float setCrewVision = opt.GetFloat(FloatOptionNames.CrewLightMod);
        float setImpVision = opt.GetFloat(FloatOptionNames.ImpostorLightMod);
        setCrewVision += player.Is(CustomRoles.Glow) ? GlowVisionSelf.GetFloat() : GlowVisionOthers.GetFloat();
        setImpVision += player.Is(CustomRoles.Glow) ? GlowVisionSelf.GetFloat() : GlowVisionOthers.GetFloat();
        //opt.SetFloat(FloatOptionNames.CrewLightMod, setCrewVision);
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, setImpVision);
        opt.SetFloat(FloatOptionNames.CrewLightMod, setCrewVision);
    }

    public static void OnFixedUpdate(PlayerControl player)
    {
        if (player == null || !player.Is(CustomRoles.Glow)) return;
        if (!Utils.IsActive(SystemTypes.Electrical)) 
        { 
            InRadius[player.PlayerId].Clear();
            MarkedOnce[player.PlayerId] = false;
            return;
        }
        if (!InRadius.ContainsKey(player.PlayerId)) InRadius[player.PlayerId] = [];
        var prevList = InRadius[player.PlayerId];
        if (!MarkedOnce.ContainsKey(player.PlayerId)) MarkedOnce[player.PlayerId] = false;
        InRadius[player.PlayerId] = Main.AllAlivePlayerControls
            .Where(target => target != null 
                && !target.Is(CustomRoles.Glow) 
                && Vector2.Distance(player.GetCustomPosition(), target.GetCustomPosition()) <= GlowRadius.GetFloat())
            .Select(target => target.PlayerId)
            .ToHashSet();

        if (!MarkedOnce[player.PlayerId] || (!prevList.SetEquals(InRadius[player.PlayerId]))) 
        {
            MarkedOnce[player.PlayerId] = true;
            Utils.MarkEveryoneDirtySettings(); 
        }
    }
}

