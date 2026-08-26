using System;
using System.Collections.Generic;
using NeonTerritories.Players;

namespace NeonTerritories.Alliance
{
    public class ReputationManager
    {
        // Tracks a player's general public reputation (0 to 100)
        private Dictionary<string, int> _playerReputations;

        // Tracks dyadic trust history (Player A towards Player B -> List of past behaviors: true = cooperative, false = defected)
        private Dictionary<string, Dictionary<string, List<bool>>> _trustHistories;

        public ReputationManager()
        {
            _playerReputations = new Dictionary<string, int>();
            _trustHistories = new Dictionary<string, Dictionary<string, List<bool>>>();
        }

        public void RegisterPlayer(string playerId, int startingReputation = 50)
        {
            if (!_playerReputations.ContainsKey(playerId))
            {
                _playerReputations[playerId] = startingReputation;
                _trustHistories[playerId] = new Dictionary<string, List<bool>>();
            }
        }

        public int GetReputation(string playerId)
        {
            return _playerReputations.ContainsKey(playerId) ? _playerReputations[playerId] : 50;
        }

        public void SetReputation(string playerId, int score)
        {
            _playerReputations[playerId] = Math.Clamp(score, 0, 100);
        }

        /// <summary>
        /// Records an interaction outcome between two players.
        /// </summary>
        public void RecordInteraction(string actorId, string targetId, bool cooperative)
        {
            if (!_trustHistories.ContainsKey(actorId))
                _trustHistories[actorId] = new Dictionary<string, List<bool>>();

            if (!_trustHistories[actorId].ContainsKey(targetId))
                _trustHistories[actorId][targetId] = new List<bool>();

            _trustHistories[actorId][targetId].Add(cooperative);

            // Adjust general reputation
            int currentRep = GetReputation(actorId);
            if (cooperative)
            {
                SetReputation(actorId, currentRep + 5);
            }
            else
            {
                SetReputation(actorId, currentRep - 15);
            }
        }

        /// <summary>
        /// Applies a heavy penalty for direct betrayal (e.g. attacking an ally).
        /// </summary>
        public void RecordBetrayal(string traitorId, string victimId)
        {
            RecordInteraction(traitorId, victimId, false);
            
            // Apply double penalty to general reputation
            int currentRep = GetReputation(traitorId);
            SetReputation(traitorId, currentRep - 25);
        }

        /// <summary>
        /// Returns the cooperation rate (0.0 to 1.0) of a target player from the perspective of an actor.
        /// </summary>
        public float GetCooperationRate(string actorId, string targetId)
        {
            if (!_trustHistories.ContainsKey(actorId) || !_trustHistories[actorId].ContainsKey(targetId))
                return 0.5f; // Neutral default

            var history = _trustHistories[actorId][targetId];
            if (history.Count == 0) return 0.5f;

            int cooperativeCounts = 0;
            foreach (bool b in history)
            {
                if (b) cooperativeCounts++;
            }

            return (float)cooperativeCounts / history.Count;
        }

        /// <summary>
        /// Slowly decays reputations back towards the neutral baseline of 50.
        /// Prevents a player from being permanently hated or loved indefinitely.
        /// </summary>
        public void DecayReputations()
        {
            var keys = new List<string>(_playerReputations.Keys);
            foreach (var key in keys)
            {
                int current = _playerReputations[key];
                // Shift 5% closer to 50
                float next = current * 0.95f + 50 * 0.05f;
                _playerReputations[key] = (int)next;
            }
        }
    }
}
