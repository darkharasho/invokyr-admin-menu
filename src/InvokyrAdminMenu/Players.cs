using System.Collections.Generic;
using Entities.Player;
using Il2CppInterop.Runtime;
using Mirror;
using UnityEngine;
using User;

namespace InvokyrAdminMenu
{
    internal sealed class PlayerRow
    {
        public ulong Id;
        public string Name;
        public bool IsHost;
    }

    /// <summary>Every lookup of players in the game. Each connected player has a <see cref="UserInstance"/> with their Steam id.</summary>
    internal static class Players
    {
        public static bool Hosting => NetworkServer.active;

        public static List<UserInstance> Users()
        {
            var users = new List<UserInstance>();
            var found = Object.FindObjectsByType(Il2CppType.Of<UserInstance>(), FindObjectsSortMode.None);
            for (var i = 0; found != null && i < found.Length; i++)
            {
                var user = found[i]?.TryCast<UserInstance>();
                if (user != null)
                    users.Add(user);
            }
            return users;
        }

        public static List<PlayerRow> Rows()
        {
            var me = UserInstance.Me;
            var rows = new List<PlayerRow>();
            foreach (var user in Users())
            {
                var id = user.SteamUserId;
                if (id == 0)
                    continue;
                var entity = user.playerEntity;
                var nick = entity != null ? entity.playerNick : null;
                rows.Add(new PlayerRow
                {
                    Id = id,
                    Name = string.IsNullOrWhiteSpace(nick) ? $"Player {id}" : nick,
                    IsHost = me != null && me.Pointer == user.Pointer,
                });
            }
            rows.Sort((a, b) => a.IsHost != b.IsHost ? (a.IsHost ? -1 : 1) : string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));
            return rows;
        }

        /// <summary>The Steam id of whoever controls <paramref name="player"/>, or 0 if unknown.</summary>
        public static ulong IdOf(PlayerEntity player, out bool isHost)
        {
            isHost = false;
            if (player == null)
                return 0;
            var me = UserInstance.Me;
            foreach (var user in Users())
            {
                var entity = user.playerEntity;
                if (entity != null && entity.Pointer == player.Pointer)
                {
                    isHost = me != null && me.Pointer == user.Pointer;
                    return user.SteamUserId;
                }
            }
            return 0;
        }
    }
}
