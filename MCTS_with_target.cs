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
        private Moves currentAction;
        private Random rnd;
        private double CP;
        private int NumberOfCollectibles;

        //p0-p1 vs p2-p3
       public bool get_line_intersection(float p0_x, float p0_y, float p1_x, float p1_y,
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


        public float dist(float x1,float y1,float x2,float y2)
        {
            return (float) Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        public bool check_intersect(Vertex ver,float x,float y,float r)
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
            float SECONDS_OF_SIMULATION =(float) Math.Sqrt(((target.X-simulator.CirclePositionX)* (target.X - simulator.CirclePositionX)+ (target.Y - simulator.CirclePositionY)* (target.Y - simulator.CirclePositionY)))/200+3;
            float step = 0.1f;
            float CircleSize = 40;
            simulator.AddInstruction(node.Move, move_time_ms);

            for (int i = 0; i < SECONDS_OF_SIMULATION; i++)
                simulator.AddInstruction(possibleMoves[rnd.Next(possibleMoves.Count)], move_time_ms);


           // Rectangle circle = new Rectangle((int)(simulator.CirclePositionX-CircleSize/2), (int)(simulator.CirclePositionY-CircleSize/2), (int)(CircleSize), (int)(CircleSize));


            for (float i=0; i <SECONDS_OF_SIMULATION;i+=step )
            {
                if (check_intersect(target, simulator.CirclePositionX, simulator.CirclePositionY,CircleSize/2))
                    return 1;
              
                simulator.Update(step);
            }
            
            return 0;
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
