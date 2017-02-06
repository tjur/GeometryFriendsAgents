using GeometryFriends;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class MCTS_with_target
    {

        private List<Moves> possibleMoves;
        private Moves currentAction;
        private Random rnd;
        private double CP;
        private int NumberOfCollectibles;

        public MCTS_with_target(List<Moves> possibleMoves, Moves currentAction, double CP, int NumberOfCollectibles)
        {
            this.possibleMoves = possibleMoves;
            this.currentAction = currentAction;
            this.rnd = new Random();
            this.CP = CP;
            this.NumberOfCollectibles = NumberOfCollectibles;
        }



        private MCTSTreeNode Expand_with_target(MCTSTreeNode node)
        {
            List<Moves> notUsedActions = possibleMoves.Except(node.Children.Select(c => c.Move)).ToList();
            Moves newMove = notUsedActions[rnd.Next(notUsedActions.Count)];

            return node.AddNewMove(newMove);
        }

        private void BackUp_with_target(MCTSTreeNode node, double value)
        {
            node.Backpropagation(value);
        }

        private MCTSTreeNode BestChild_with_target(MCTSTreeNode node, double c)
        {
            double maxValue = -1;
            MCTSTreeNode bestNode = null;

            foreach (MCTSTreeNode child in node.Children)
            {
                double value = child.Value / child.Simulations + c * Math.Sqrt(2 * Math.Log(node.Simulations) / child.Simulations);

                if (value > maxValue)
                {
                    maxValue = value;
                    bestNode = child;
                }
            }

            return bestNode;
        }

        private MCTSTreeNode TreePolicy_with_target(ActionSimulator simulator, MCTSTreeNode node, float move_time_ms)
        {
            while (true) // todo: może nie warto schodzić zbyt głęboko
            {
                if (node.Children.Count < possibleMoves.Count)
                    return Expand_with_target(node);
                else
                {
                    node = BestChild_with_target(node, CP);
                    simulator.AddInstruction(node.Move, move_time_ms);
                }
            }
        }




        private MCTSTreeNode BestChild_with_target(MCTSTreeNode node, double c, Vertex target)
        {
            double maxValue = -1;
            MCTSTreeNode bestNode = null;

            foreach (MCTSTreeNode child in node.Children)
            {
                double value = child.Value / child.Simulations + c * Math.Sqrt(2 * Math.Log(node.Simulations) / child.Simulations);

                if (value > maxValue)
                {
                    maxValue = value;
                    bestNode = child;
                }
            }

            return bestNode;
        }


        private double DefaultPolicy_with_target(ActionSimulator simulator, MCTSTreeNode node, Vertex target, float move_time_ms)
        {
            const float SECONDS_OF_SIMULATION = 20;

            simulator.AddInstruction(node.Move, move_time_ms);

            for (int i = 0; i < SECONDS_OF_SIMULATION; i++)
                simulator.AddInstruction(possibleMoves[rnd.Next(possibleMoves.Count)], 1);

            simulator.Update(SECONDS_OF_SIMULATION);

         
            int CollectiblesCaught = NumberOfCollectibles - simulator.CollectiblesUncaughtCount;
            return CollectiblesCaught ;
        }

        public Moves UCTSearch_with_target(ActionSimulator simulator, Vertex target, float move_time_ms)
        {
            if (simulator == null) { return Moves.NO_ACTION; }

            MCTSTreeNode root = new MCTSTreeNode(Moves.NO_ACTION, null);
            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).Seconds < move_time_ms * 1000)
            {
                simulator.AddInstruction(/*Moves.NO_ACTION*/this.currentAction, move_time_ms);
                // simulator.SimulatorStep = 0.1f;

                MCTSTreeNode node = TreePolicy_with_target(simulator, root, move_time_ms);
                double value = DefaultPolicy_with_target(simulator, node, target, move_time_ms);
                BackUp_with_target(node, value);

                simulator.ResetSimulator();
            }

            MCTSTreeNode bestNode = BestChild_with_target(root, 0);

            Log.LogInformation("Root simulations: " + root.Simulations + ", value: " + root.Value);
            Log.LogInformation("Best node (" + bestNode.Move + ") simulations: " + bestNode.Simulations + ", value: " + bestNode.Value);

            return bestNode.Move;
        }

    }
}
