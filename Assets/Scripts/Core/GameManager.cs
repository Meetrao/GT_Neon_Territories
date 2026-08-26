using System;
using System.Collections.Generic;
using System.Linq;
using NeonTerritories.Players;
using NeonTerritories.Territories;
using NeonTerritories.Strategies;
using NeonTerritories.GameTheory;
using NeonTerritories.Alliance;
using NeonTerritories.AI;

namespace NeonTerritories.Core
{
    public class GameManager
    {
        public TerritoryGraph Map { get; private set; }
        public Dictionary<string, Player> Players { get; private set; }
        public ReputationManager Reputation { get; private set; }
        public AllianceManager Alliances { get; private set; }
        public ConflictResolver ConflictResolver { get; private set; }
        public PayoffCalculator PayoffCalc { get; private set; }
        public StrategyPredictor Predictor { get; private set; }
        public AIDecisionEngine AIEngine { get; private set; }

        public int CurrentRound { get; private set; }
        public const int MaxRounds = 15;
        public List<string> ActionLogs { get; private set; }

        public GameManager()
        {
            Map = new TerritoryGraph();
            Players = new Dictionary<string, Player>();
            Reputation = new ReputationManager();
            Alliances = new AllianceManager(Reputation);
            ConflictResolver = new ConflictResolver(Map, Players);
            PayoffCalc = new PayoffCalculator(Map);
            Predictor = new StrategyPredictor();
            AIEngine = new AIDecisionEngine();
            CurrentRound = 1;
            ActionLogs = new List<string>();

            InitializePlayers();
            DistributeStartingTerritories();
        }

        private void InitializePlayers()
        {
            // Create the 4 players/factions
            AddPlayer(new Player("P1", "Nova Conglomerate", FactionType.NOVA, true));
            AddPlayer(new Player("P2", "Vortex Syndicate", FactionType.VORTEX, true));
            AddPlayer(new Player("P3", "Pulse Network", FactionType.PULSE, true));
            AddPlayer(new Player("P4", "Synth Nexus", FactionType.SYNTH, true));
        }

        private void AddPlayer(Player player)
        {
            Players.Add(player.Id, player);
            Reputation.RegisterPlayer(player.Id, player.Reputation);
            Predictor.RegisterPlayer(player.Id);
        }

        private void DistributeStartingTerritories()
        {
            // Distribute 2 matching territories to each faction, leaving others neutral
            AssignTerritory("T01", "P1"); // Energy
            AssignTerritory("T03", "P1"); // Credit

            AssignTerritory("T04", "P2"); // Defense
            AssignTerritory("T11", "P2"); // Energy

            AssignTerritory("T02", "P3"); // Tech
            AssignTerritory("T10", "P3"); // Tech

            AssignTerritory("T09", "P4"); // Credit
            AssignTerritory("T12", "P4"); // Defense
        }

        private void AssignTerritory(string territoryId, string playerId)
        {
            var territory = Map.GetTerritory(territoryId);
            if (territory != null)
            {
                territory.OwnerId = playerId;
                Players[playerId].AddTerritory(territoryId);
            }
        }

        /// <summary>
        /// Executes a single full round in the game.
        /// </summary>
        public bool PlayRound()
        {
            if (CurrentRound > MaxRounds)
            {
                LogEvent("Game has already concluded.");
                return false;
            }

            LogEvent($"\n=== STARTING ROUND {CurrentRound} / {MaxRounds} ===");

            // 1. Resource Generation Phase
            GenerateResourcesForRound();

            // 2. Information/Diplomacy Phase (Alliances)
            NegotiateAlliancesPhase();

            // 3. Strategy Selection Phase
            var lockedStrategies = GatherPlayerStrategies();

            // 4. Record behaviours for prediction
            foreach (var strat in lockedStrategies)
            {
                Predictor.RecordPlayerAction(strat.PlayerId, strat.Type);
            }

            // Deduct costs upfront
            foreach (var strat in lockedStrategies)
            {
                var player = Players[strat.PlayerId];
                strat.ExecuteCosts(player);
            }

            // 5. Strategy Resolution & Conflict Phase
            var allianceDict = Alliances.GetAllianceDictionary(Players.Keys.ToList());
            ConflictResolver.ResolveConflicts(lockedStrategies, allianceDict);

            // 6. Post-Round Housekeeping
            Alliances.TickAlliances();
            Reputation.DecayReputations();
            UpdateStrategicScores();

            LogEvent($"=== ROUND {CurrentRound} RESOLVED ===");
            foreach (var player in Players.Values)
            {
                LogEvent(player.ToString());
            }

            CurrentRound++;
            return CurrentRound <= MaxRounds;
        }

        private void GenerateResourcesForRound()
        {
            LogEvent("Resource Generation Phase:");
            foreach (var player in Players.Values)
            {
                player.ResetRoundActionPoints();

                // Sum up resources from all controlled territories
                PlayerResources roundGains = new PlayerResources();
                foreach (var tId in player.ControlledTerritoryIds)
                {
                    var territory = Map.GetTerritory(tId);
                    if (territory != null)
                    {
                        roundGains.Add(territory.GenerationRate);
                    }
                }

                // Apply faction modifiers
                roundGains.Credits = (int)(roundGains.Credits * player.CreditGainModifier);
                roundGains.Technology = (int)(roundGains.Technology * player.TechGainModifier);

                player.Resources.Add(roundGains);
                LogEvent($"- {player.Name} generated: {roundGains}");
            }
        }

        private void NegotiateAlliancesPhase()
        {
            // Simple automated AI negotiations based on reputation
            var playerList = Players.Values.ToList();
            for (int i = 0; i < playerList.Count; i++)
            {
                var proposer = playerList[i];
                if (!proposer.IsAI) continue;

                for (int j = i + 1; j < playerList.Count; j++)
                {
                    var receiver = playerList[j];
                    if (!receiver.IsAI) continue;

                    // If they are not already allied, and both have decent reputations
                    if (!Alliances.AreAllied(proposer.Id, receiver.Id))
                    {
                        int proposerRep = Reputation.GetReputation(proposer.Id);
                        int receiverRep = Reputation.GetReputation(receiver.Id);

                        // If both players have positive trust, establish alliance
                        if (proposerRep >= 45 && receiverRep >= 45)
                        {
                            bool success = Alliances.ProposeAlliance(proposer.Id, receiver.Id, 3); // 3 round agreement
                            if (success)
                            {
                                LogEvent($"- Alliance formed between [{proposer.Name}] and [{receiver.Name}] for 3 rounds.");
                            }
                        }
                    }
                }
            }
        }

        private List<Strategy> GatherPlayerStrategies()
        {
            List<Strategy> allStrategies = new List<Strategy>();
            var alliesDict = Alliances.GetAllianceDictionary(Players.Keys.ToList());

            foreach (var player in Players.Values)
            {
                if (player.IsAI)
                {
                    var aiActions = AIEngine.DetermineRoundActions(player, Map, Players.Values.ToList(), Predictor, alliesDict[player.Id]);
                    allStrategies.AddRange(aiActions);
                }
                // (For human players, they would lock actions through UI inputs)
            }

            return allStrategies;
        }

        private void UpdateStrategicScores()
        {
            foreach (var player in Players.Values)
            {
                // Dynamic score recalculation based on territories and resources
                int score = 0;

                // 1. Territory strategic value
                foreach (var tId in player.ControlledTerritoryIds)
                {
                    var territory = Map.GetTerritory(tId);
                    if (territory != null)
                    {
                        score += territory.StrategicValue;
                    }
                }

                // 2. Resource scores (converting reserves to points)
                score += (int)(player.Resources.Credits * 0.1f);
                score += (int)(player.Resources.Energy * 0.1f);
                score += (int)(player.Resources.Technology * 0.2f);
                score += (int)(player.Resources.Influence * 0.2f);

                // 3. Alliance bonuses
                int activeAllies = Alliances.GetAllies(player.Id).Count;
                score += activeAllies * 30; // 30 points per active alliance

                player.StrategicScore = score;
            }
        }

        private void LogEvent(string msg)
        {
            ActionLogs.Add(msg);
            Console.WriteLine(msg);
        }
    }
}
