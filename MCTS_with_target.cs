using GeometryFriends;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class MCTS_with_target
    {

        private List<Moves> possibleMoves;
        private Random rnd;
        private double CP;
        private Graph Graph;
        private bool DidCollide;
        private float CollisionStep;
        private BFSHeura BFSHeura;

        //p0-p1 vs p2-p3
       public static bool get_line_intersection(float p0_x, float p0_y, float p1_x, float p1_y,
                            float p2_x, float p2_y, float p3_x, float p3_y)
        {
                float s02_x, s02_y, s10_x, s10_y, s32_x, s32_y, s_numer, t_numer, denom, t;
                s10_x = p1_x - p0_x;
                s10_y = p1_y - p0_y;
                s32_x = p3_x - p2_x;
                s32_y = p3_y - p2_y;

                denom = s10_x * s32_y - s32_x * s10_y;
                if (denom == 0)
                    return false; // Collinear
                bool denomPositive = denom > 0;

                s02_x = p0_x - p2_x;
                s02_y = p0_y - p2_y;
                s_numer = s10_x * s02_y - s10_y * s02_x;
                if ((s_numer < 0) == denomPositive)
                    return false; // No collision

                t_numer = s32_x * s02_y - s32_y * s02_x;
                if ((t_numer < 0) == denomPositive)
                    return false; // No collision

                if (((s_numer > denom) == denomPositive) || ((t_numer > denom) == denomPositive))
                    return false; 

                return true;
            }


        public static float dist(float x1,float y1,float x2,float y2)
        {
            return (float) Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        public static bool check_intersect(Vertex ver,float x,float y,float r)
        {
            if (dist(ver.X - ver.Width / 2, ver.Y - ver.Height / 2, x, y) <= r)
                return true;
            if (dist(ver.X + ver.Width / 2, ver.Y - ver.Height / 2, x, y) <= r)
                return true;
            if (dist(ver.X - ver.Width / 2, ver.Y + ver.Height / 2, x, y) <= r)
                return true;
            if (dist(ver.X + ver.Width / 2, ver.Y + ver.Height / 2, x, y) <= r)
                return true;

            if (get_line_intersection(ver.X - ver.Width / 2, ver.Y - ver.Height / 2, ver.X + ver.Width / 2, ver.Y - ver.Height / 2, x, y + r / 2, x, y - r / 2) == true)
                return true;
            if (get_line_intersection(ver.X - ver.Width / 2, ver.Y + ver.Height / 2, ver.X + ver.Width / 2, ver.Y + ver.Height / 2, x, y + r / 2, x, y - r / 2) == true)
                return true;
            if (get_line_intersection(ver.X - ver.Width / 2, ver.Y - ver.Height / 2, ver.X - ver.Width / 2, ver.Y + ver.Height / 2, x+r/2, y , x-r/2, y) == true)
                return true;
            if (get_line_intersection(ver.X + ver.Width / 2, ver.Y - ver.Height / 2, ver.X + ver.Width / 2, ver.Y + ver.Height / 2, x + r / 2, y, x - r / 2, y) == true)
                return true;
            
            return false;
        }

        public MCTS_with_target(List<Moves> possibleMoves, double CP, Graph Graph, BFSHeura BFSHeura)
        {
            this.possibleMoves = possibleMoves;
            this.rnd = new Random();
            this.CP = CP;
            this.Graph = Graph;
            this.DidCollide = false;
            CollisionStep = 0.1f;
            this.BFSHeura = BFSHeura;
        }

        private MCTSTreeNode Expand_with_target(MCTSTreeNode node, Vertex source, Vertex target)
        {
            Moves newMove;
            Moves suggestedMove = Graph.Edges[source][target].SuggestedMove;
            List<Moves> notUsedActions = possibleMoves.Except(node.Children.Select(c => c.Move)).ToList();
            List<Moves> movesWithoutJump = notUsedActions.Where(move => move != Moves.JUMP).ToList();

            if (node.Children.Count == 0)
                newMove = suggestedMove;

            else if (movesWithoutJump.Count > 0)
                newMove = movesWithoutJump[rnd.Next(movesWithoutJump.Count)];

            else
                newMove = Moves.JUMP;

            return node.AddNewMove(newMove);
        }

        private void BackUp_with_target(MCTSTreeNode node, double value)
        {
            node.Backpropagation(value);
        }

        private MCTSTreeNode BestChild_with_target(MCTSTreeNode node, double c, Moves suggestedMove)
        {
            double maxValue = -1;
            MCTSTreeNode bestNode = null;

            foreach (MCTSTreeNode child in node.Children)
            {
                double value = child.Value / child.Simulations + c * Math.Sqrt(2 * Math.Log(node.Simulations) / child.Simulations);

                if (child.Move == suggestedMove)
                    value += 0.5f;

                if (value > maxValue)
                {
                    maxValue = value;
                    bestNode = child;
                }
            }

            return bestNode;
        }

        private MCTSTreeNode TreePolicy_with_target(ActionSimulator simulator, MCTSTreeNode node, float move_time, Vertex source, Vertex target)
        {
            while (true) // todo: może nie warto schodzić zbyt głęboko
            {
                const float ExpandOtherChance = 0.2f;

                if (node.Children.Count == 0 || rnd.NextDouble() < ExpandOtherChance && node.Children.Count < possibleMoves.Count)
                    return Expand_with_target(node, source, target);
                else
                {
                    node = BestChild_with_target(node, CP, Graph[source][target].SuggestedMove);
                    RunSimulatorAndCheckCollision(target, simulator, node.Move, move_time * 1000);
                }
            }
        }

        private double DefaultPolicy_with_target(ActionSimulator simulator, MCTSTreeNode node, Vertex source, Vertex target, float move_time)
        {
            RunSimulatorAndCheckCollision(target, simulator, node.Move, move_time * 1000);

            if (DidCollide)
                return 1;

            int MaxCircleSpeed = 200;
            float SECONDS_OF_SIMULATION = dist(target.X, target.Y, simulator.CirclePositionX, simulator.CirclePositionY) / MaxCircleSpeed + 0.5f;
            float SuggestedMoveChance = 0.0f;
            float movesInMoveTime = SECONDS_OF_SIMULATION / move_time;

            for (int i = 0; i < movesInMoveTime; i++)
            {
                Moves move = possibleMoves[rnd.Next(possibleMoves.Count)];

                if (rnd.NextDouble() < SuggestedMoveChance)
                    move = Graph[source][target].SuggestedMove;

                RunSimulatorAndCheckCollision(target, simulator, move, move_time * 1000);
            }

            if (DidCollide)
                return 1;

            float d = dist(simulator.CirclePositionX, simulator.CirclePositionY, target.X, target.Y);//BFSHeura.BfsHeura(simulator.CirclePositionX, simulator.CirclePositionY, target.X, target.Y, true);

            double CONST2 = 1000;
            double g = Math.Max(-d / CONST2 + 1, 0);
            
            return g;
        }

        public Moves UCTSearch_with_target(ActionSimulator simulator, Vertex source, Vertex target, float move_time, Moves currentAction)
        {
            if (simulator == null) { return Moves.NO_ACTION; }

            MCTSTreeNode root = new MCTSTreeNode(Moves.NO_ACTION, null);
            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds < move_time)
            {
                DidCollide = false;
                simulator.SimulatorStep = 0.06f;
                RunSimulatorAndCheckCollision(target, simulator, currentAction, move_time * 1000);

                MCTSTreeNode node = TreePolicy_with_target(simulator, root, move_time, source, target);
                double value = DefaultPolicy_with_target(simulator, node, source, target, move_time);
                BackUp_with_target(node, value);

                simulator.ResetSimulator();
            }

            MCTSTreeNode bestNode = BestChild_with_target(root, 0, Moves.MORPH_UP);

            Log.LogInformation("Root simulations: " + root.Simulations + ", value: " + root.Value);
            Log.LogInformation("    Best node (" + bestNode.Move + ") simulations: " + bestNode.Simulations + ", value: " + bestNode.Value);

            return bestNode.Move;
        }

        private void RunSimulatorAndCheckCollision(Vertex target, ActionSimulator simulator, Moves move, float timeMs)
        {
            float numberOfSteps = timeMs / 1000 / CollisionStep;

            for (int j = 0; j < numberOfSteps; j++)
            {
                CircleAgent.RunSimulator(simulator, move, CollisionStep * 1000);

                if (check_intersect(target, simulator.CirclePositionX, simulator.CirclePositionY, simulator.CircleVelocityRadius))
                    DidCollide = true;
            }
        }

    }
}
