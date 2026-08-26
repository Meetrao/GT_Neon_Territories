using NeonTerritories.Players;
using NeonTerritories.Territories;

namespace NeonTerritories.Strategies
{
    public class SpyStrategy : Strategy
    {
        public string TargetPlayerId { get; private set; }
        public string TargetTerritoryId { get; private set; }
        
        public int TechSpent => ResourceCost.Technology;

        public SpyStrategy(string playerId, string targetPlayerId, string targetTerritoryId, int techSpent, Player player)
            : base(playerId, ActionType.Spy, 1)
        {
            TargetPlayerId = targetPlayerId;
            TargetTerritoryId = targetTerritoryId;

            // Apply SYNTH faction spy cost discount
            float modifier = player != null ? player.SpyCostModifier : 1.0f;
            ResourceCost.Technology = (int)(techSpent * modifier);
        }

        public override bool Validate(Player player, TerritoryGraph map)
        {
            if (!base.Validate(player, map))
                return false;

            // Cannot spy on yourself
            if (PlayerId == TargetPlayerId)
                return false;

            if (TechSpent < 5) // Minimum intelligence required
                return false;

            return true;
        }

        public override string ToString()
        {
            string targetText = string.IsNullOrEmpty(TargetTerritoryId) ? $"Player {TargetPlayerId}" : $"Territory {TargetTerritoryId}";
            return $"{base.ToString()} | Spy on {targetText} spending {TechSpent} Tech";
        }
    }
}
