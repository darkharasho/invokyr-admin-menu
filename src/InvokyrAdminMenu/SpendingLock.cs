using System;
using System.Collections.Generic;
using System.Reflection;
using _200_Dev.Entities;
using _200_Dev.EnvironmentElements.Shop;
using _200_Dev.UI.Networking;
using Entities.Player;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace InvokyrAdminMenu
{
    /// <summary>
    /// Stops blocked players spending the team's money, on the host only (purchases are decided on the server, so
    /// nobody else needs the mod). Shop items: each slot type's <c>SrvOnBought</c> is skipped and reports "not bought".
    /// Room skins: the server side of <c>CmdTryPayForSkin</c> is skipped. If the price was already taken out of the
    /// bank by the time the purchase is stopped, it is put back at the end of the frame.
    /// </summary>
    internal static class SpendingLock
    {
        private static readonly Dictionary<ulong, float> LastNotice = new Dictionary<ulong, float>();
        private static int _lastScore = int.MinValue;
        private static bool _blockedThisFrame;
        private static string _blockedWhat;

        public static void Apply(Harmony harmony)
        {
            var bought = AccessTools.Method(typeof(SpendingLock), nameof(BoughtPrefix));
            foreach (var type in new[] { typeof(ShopUpgradeItemSlot), typeof(ShopGadgetItemSlot), typeof(ShopFurnitureItemSlot), typeof(ShopConsumableItemSlot) })
                Patch(harmony, AccessTools.Method(type, "SrvOnBought", new[] { typeof(PlayerEntity) }), bought);
            Patch(harmony, AccessTools.Method(typeof(SkinSelector), "UserCode_CmdTryPayForSkin__PlayerEntity__Int32"), AccessTools.Method(typeof(SpendingLock), nameof(SkinPrefix)));
        }

        private static void Patch(Harmony harmony, MethodInfo target, MethodInfo prefix)
        {
            if (target == null)
            {
                Plugin.Log.LogWarning($"Spending lock: a purchase method is missing ({prefix.Name}); that kind of purchase isn't blocked");
                return;
            }
            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        }

        private static bool BoughtPrefix(ShopItemSlot __instance, PlayerEntity __0, ref bool __result)
        {
            if (!Blocks(__0, __instance?.name ?? "a shop item"))
                return true;
            __result = false;
            return false;
        }

        private static bool SkinPrefix(PlayerEntity __0) => !Blocks(__0, "a room skin");

        /// <summary>True if <paramref name="player"/> may not spend; tells them why.</summary>
        private static bool Blocks(PlayerEntity player, string what)
        {
            try
            {
                if (!NetworkServer.active || player == null)
                    return false;
                var id = Players.IdOf(player, out var isHost);
                if (id == 0)
                    return false;
                if (Plugin.Rules.Remember(id, isHost))
                    Plugin.SaveRules();
                if (Plugin.Rules.IsAllowed(id))
                    return false;

                _blockedThisFrame = true;
                _blockedWhat = what;
                Plugin.Log.LogInfo($"Blocked {player.playerNick} ({id}) from buying {what}");
                Notify(player, id);
                return true;
            }
            catch (Exception e)
            {
                // Never break a purchase because of the mod.
                Plugin.Log.LogError($"Spending lock check failed, allowing the purchase: {e}");
                return false;
            }
        }

        private static void Notify(PlayerEntity player, ulong id)
        {
            var now = Time.unscaledTime;
            if (LastNotice.TryGetValue(id, out var last) && now - last < 2f)
                return;
            LastNotice[id] = now;
            var ui = UIManager.Instance;
            var connection = player.connectionToClient;
            if (ui != null && connection != null)
                ui.TargetShowMessage(connection, Plugin.BlockedMessage.Value, 3f);
        }

        /// <summary>Once per frame, after the game's Update: refunds a blocked purchase the game had already paid for.</summary>
        public static void LateTick()
        {
            if (!NetworkServer.active)
            {
                _lastScore = int.MinValue;
                _blockedThisFrame = false;
                return;
            }
            var bank = Bank.instance;
            if (bank == null)
                return;
            var score = bank.GetCurrentTotalScore();
            if (_blockedThisFrame && _lastScore != int.MinValue && score < _lastScore)
            {
                var lost = _lastScore - score;
                Plugin.Log.LogInfo($"Refunding {lost} taken for blocked purchase of {_blockedWhat}");
                bank.SrvAddToTotalScore(lost);
                score = bank.GetCurrentTotalScore();
            }
            _blockedThisFrame = false;
            _lastScore = score;
        }
    }
}
