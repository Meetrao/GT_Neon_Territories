using System;
using System.Collections.Generic;

namespace NeonTerritories.Alliance
{
    public class AllianceAgreement
    {
        public string PlayerAId { get; private set; }
        public string PlayerBId { get; private set; }
        public int RemainingRounds { get; set; }

        public AllianceAgreement(string playerAId, string playerBId, int duration)
        {
            PlayerAId = playerAId;
            PlayerBId = playerBId;
            RemainingRounds = duration;
        }

        public bool Contains(string playerId)
        {
            return PlayerAId == playerId || PlayerBId == playerId;
        }

        public string GetAllyOf(string playerId)
        {
            if (PlayerAId == playerId) return PlayerBId;
            if (PlayerBId == playerId) return PlayerAId;
            return null;
        }
    }

    public class AllianceManager
    {
        private List<AllianceAgreement> _activeAlliances;
        private ReputationManager _reputationManager;

        public AllianceManager(ReputationManager reputationManager)
        {
            _activeAlliances = new List<AllianceAgreement>();
            _reputationManager = reputationManager;
        }

        /// <summary>
        /// Proposes and establishes an alliance (symmetric relationship).
        /// </summary>
        public bool ProposeAlliance(string proposerId, string receiverId, int duration)
        {
            if (proposerId == receiverId) return false;
            if (AreAllied(proposerId, receiverId)) return false;

            // Simple checks: AI will refuse alliances with low reputation players
            int proposerRep = _reputationManager.GetReputation(proposerId);
            if (proposerRep < 30)
            {
                // Alliance rejected due to low trust
                return false;
            }

            _activeAlliances.Add(new AllianceAgreement(proposerId, receiverId, duration));
            _reputationManager.RecordInteraction(proposerId, receiverId, true);
            _reputationManager.RecordInteraction(receiverId, proposerId, true);
            return true;
        }

        /// <summary>
        /// Checks if two players are currently allied.
        /// </summary>
        public bool AreAllied(string playerAId, string playerBId)
        {
            foreach (var alliance in _activeAlliances)
            {
                if (alliance.Contains(playerAId) && alliance.Contains(playerBId))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns all current allies of a player.
        /// </summary>
        public List<string> GetAllies(string playerId)
        {
            List<string> allies = new List<string>();
            foreach (var alliance in _activeAlliances)
            {
                if (alliance.Contains(playerId))
                {
                    allies.Add(alliance.GetAllyOf(playerId));
                }
            }
            return allies;
        }

        /// <summary>
        /// Directly breaks an alliance due to aggression or choice, triggering penalties.
        /// </summary>
        public void BreakAlliance(string breakerId, string allyId)
        {
            var allianceToRemove = _activeAlliances.Find(a => a.Contains(breakerId) && a.Contains(allyId));
            if (allianceToRemove != null)
            {
                _activeAlliances.Remove(allianceToRemove);
                _reputationManager.RecordBetrayal(breakerId, allyId);
            }
        }

        /// <summary>
        /// Ticks down remaining rounds for all alliances, removing expired ones.
        /// </summary>
        public void TickAlliances()
        {
            for (int i = _activeAlliances.Count - 1; i >= 0; i--)
            {
                _activeAlliances[i].RemainingRounds--;
                if (_activeAlliances[i].RemainingRounds <= 0)
                {
                    _activeAlliances.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Generates a dictionary representation of active alliances for helper lookups.
        /// </summary>
        public Dictionary<string, List<string>> GetAllianceDictionary(List<string> allPlayerIds)
        {
            var dict = new Dictionary<string, List<string>>();
            foreach (var pid in allPlayerIds)
            {
                dict[pid] = GetAllies(pid);
            }
            return dict;
        }
    }
}
