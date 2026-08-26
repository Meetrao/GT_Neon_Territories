using System;
using NeonTerritories.Players;
using NeonTerritories.Territories;

namespace NeonTerritories.Strategies
{
    public enum ActionType
    {
        Attack,
        Defend,
        Expand,
        Invest,
        Trade,
        Spy,
        Negotiate
    }

    public abstract class Strategy
    {
        public string PlayerId { get; private set; }
        public ActionType Type { get; private set; }
        public int APCost { get; protected set; }
        public PlayerResources ResourceCost { get; protected set; }

        protected Strategy(string playerId, ActionType type, int apCost)
        {
            PlayerId = playerId;
            Type = type;
            APCost = apCost;
            ResourceCost = new PlayerResources();
        }

        /// <summary>
        /// Validates if the action is legally playable given the player and map state.
        /// </summary>
        public virtual bool Validate(Player player, TerritoryGraph map)
        {
            if (player.ActionPoints < APCost)
                return false;

            if (!player.Resources.CanAfford(ResourceCost))
                return false;

            return true;
        }

        /// <summary>
        /// Deducts resources and AP from the player executing the strategy.
        /// </summary>
        public virtual void ExecuteCosts(Player player)
        {
            player.ActionPoints -= APCost;
            player.Resources.Deduct(ResourceCost);
        }

        public override string ToString()
        {
            return $"{PlayerId} chooses {Type} (AP: {APCost}, Cost: {ResourceCost})";
        }
    }
}
