using System;
using System.Collections.Generic;
using System.Linq;

namespace InvokyrAdminMenu
{
    /// <summary>
    /// Who may spend the team's money. Saved as "steamid:allow,steamid:block". A player is recorded the first time
    /// the host sees them, with the "new players" default of that moment, so changing the default later doesn't
    /// flip anyone already known.
    /// </summary>
    internal sealed class SpendingRules
    {
        private readonly Dictionary<ulong, bool> _allowed = new Dictionary<ulong, bool>();

        public bool BlockNewPlayers;

        public static SpendingRules Parse(string text, bool blockNewPlayers)
        {
            var rules = new SpendingRules { BlockNewPlayers = blockNewPlayers };
            foreach (var entry in (text ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = entry.Split(':');
                if (parts.Length != 2 || !ulong.TryParse(parts[0], out var id) || id == 0)
                    continue;
                var state = parts[1].Trim().ToLowerInvariant();
                if (state == "allow")
                    rules._allowed[id] = true;
                else if (state == "block")
                    rules._allowed[id] = false;
            }
            return rules;
        }

        public bool Knows(ulong id) => _allowed.ContainsKey(id);

        /// <summary>Unknown players get the "new players" default until <see cref="Remember"/> records them.</summary>
        public bool IsAllowed(ulong id) => _allowed.TryGetValue(id, out var allowed) ? allowed : !BlockNewPlayers;

        /// <summary>Records a player seen for the first time. The host is always allowed. True if they were new.</summary>
        public bool Remember(ulong id, bool isHost)
        {
            if (id == 0 || _allowed.ContainsKey(id))
                return false;
            _allowed[id] = isHost || !BlockNewPlayers;
            return true;
        }

        public void Set(ulong id, bool allowed)
        {
            if (id != 0)
                _allowed[id] = allowed;
        }

        public string Serialize() =>
            string.Join(",", _allowed.OrderBy(p => p.Key).Select(p => $"{p.Key}:{(p.Value ? "allow" : "block")}"));
    }
}
