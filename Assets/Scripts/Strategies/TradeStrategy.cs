using NeonTerritories.Players;
using NeonTerritories.Territories;

namespace NeonTerritories.Strategies
{
    public class TradeStrategy : Strategy
    {
        public string ReceiverPlayerId { get; private set; }
        public PlayerResources OfferedResources { get; private set; }
        public PlayerResources RequestedResources { get; private set; }
        public bool IsAccepted { get; set; }

        public TradeStrategy(string playerId, string receiverPlayerId, PlayerResources offered, PlayerResources requested)
            : base(playerId, ActionType.Trade, 1)
        {
            ReceiverPlayerId = receiverPlayerId;
            OfferedResources = offered ?? new PlayerResources();
            RequestedResources = requested ?? new PlayerResources();
            IsAccepted = false;

            // The offered resources are the actual cost deducted upfront from the sender
            ResourceCost = OfferedResources;
        }

        public override bool Validate(Player player, TerritoryGraph map)
        {
            if (!base.Validate(player, map))
                return false;

            // Cannot trade with yourself
            if (PlayerId == ReceiverPlayerId)
                return false;

            // Check if player has enough action points and the offered resources
            if (!player.Resources.CanAfford(OfferedResources))
                return false;

            return true;
        }

        public override string ToString()
        {
            return $"{base.ToString()} | Trade Proposal to {ReceiverPlayerId}: Offer [{OfferedResources}] for [{RequestedResources}]";
        }
    }
}
