using GeometryFriends;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Communication;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace GeometryFriendsAgents
{
    /// <summary>
    /// A circle agent implementation for the GeometryFriends game that demonstrates prediction and history keeping capabilities.
    /// </summary>
    public class CircleAgent : AbstractCircleAgent
    {
        //agent implementation specificiation
        private bool implementedAgent;
        private string agentName = "MCTSCircle";

        //auxiliary variables for agent action
        private MCTS_with_target MCTS_with_target;
        private List<Vertex> BestPath;
        private int CurrentTargetIndex;
        private Moves currentAction;
        private List<Moves> possibleMoves;
        private long LastMoveTime;
        private Random rnd;

        //predictor of actions for the circle
        private ActionSimulator predictor = null;
        private DebugInformation[] debugInfo = null;
        private List<DebugInformation> staticDebugInfo = null;
        private int debugCircleSize = 20;

        //debug agent predictions and history keeping
        private List<CollectibleRepresentation> caughtCollectibles;
        private List<CollectibleRepresentation> uncaughtCollectibles;
        private object remainingInfoLock = new Object();
        private List<CollectibleRepresentation> remaining;

        //Sensors Information and level state
        private CountInformation numbersInfo;
        private RectangleRepresentation rectangleInfo;
        private CircleRepresentation circleInfo;
        private ObstacleRepresentation[] obstaclesInfo;
        private ObstacleRepresentation[] rectanglePlatformsInfo;
        private ObstacleRepresentation[] circlePlatformsInfo;
        private CollectibleRepresentation[] collectiblesInfo;

        private ObstacleRepresentation[] borderObstacles; // 4 przeszkody stanowiące obramowanie planszy (nie ma ich w obstaclesInfo)

        private int nCollectiblesLeft;

        private List<AgentMessage> messages;

        //Area of the game screen
        private Rectangle area;

        // Graf utworzony przez GraphCreator
        private GraphCreator GraphCreator;
        private Graph Graph;
        private bool CreatedOtherVertices = false;
        private DateTime LastTimeOnPath;

        float time_step = 0.35f;
        DateTime lastaction;

        private double CP;
        private int NumberOfCollectibles;

        public CircleAgent()
        {

            lastaction = DateTime.Now;

            //Change flag if agent is not to be used
            implementedAgent = true;

            //setup for action updates
            // LastMoveTime = DateTime.Now.Second;
            currentAction = Moves.NO_ACTION;
            rnd = new Random();

            //prepare the possible moves  
            possibleMoves = new List<Moves>();
            possibleMoves.Add(Moves.ROLL_LEFT);
            possibleMoves.Add(Moves.ROLL_RIGHT);
            possibleMoves.Add(Moves.JUMP);

            //possibleMoves.Add(Moves.NO_ACTION);

            //history keeping
            uncaughtCollectibles = new List<CollectibleRepresentation>();
            caughtCollectibles = new List<CollectibleRepresentation>();
            remaining = new List<CollectibleRepresentation>();

            //messages exchange
            messages = new List<AgentMessage>();
        }

        //implements abstract circle interface: used to setup the initial information so that the agent has basic knowledge about the level
        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            numbersInfo = nI;
            nCollectiblesLeft = nI.CollectiblesCount;
            rectangleInfo = rI;
            circleInfo = cI;
            obstaclesInfo = oI;
            rectanglePlatformsInfo = rPI;
            circlePlatformsInfo = cPI;
            collectiblesInfo = colI;
            uncaughtCollectibles = new List<CollectibleRepresentation>(collectiblesInfo);
            this.area = area;

            //send a message to the rectangle informing that the circle setup is complete and show how to pass an attachment: a pen object
            messages.Add(new AgentMessage("Setup complete, testing to send an object as an attachment.", new Pen(Color.AliceBlue)));

            NumberOfCollectibles = nCollectiblesLeft;
            CP = (double)NumberOfCollectibles / Math.Sqrt(2);

            // dodaję obramowanie planszy jako przeszkody (aby można było ją narysować i postawić na niej wierzchołki)
            const int borderWidth = 40; // szerokość czarnej ramki otaczającej każdą planszę

            borderObstacles = new ObstacleRepresentation[4];
            borderObstacles[0] = new ObstacleRepresentation(borderWidth / 2, (area.Height + 2 * borderWidth) / 2, borderWidth, area.Height + 2 * borderWidth);
            borderObstacles[1] = new ObstacleRepresentation(area.Width + borderWidth + borderWidth / 2, (area.Height + 2 * borderWidth) / 2, borderWidth, area.Height + 2 * borderWidth);
            borderObstacles[2] = new ObstacleRepresentation((area.Width + 2 * borderWidth) / 2, borderWidth / 2, area.Width, borderWidth);
            borderObstacles[3] = new ObstacleRepresentation((area.Width + 2 * borderWidth) / 2, area.Height + borderWidth + borderWidth / 2, area.Width, borderWidth);
            ObstacleRepresentation[] newOIArray = obstaclesInfo.Concat(borderObstacles).ToArray();

            GraphCreator = new GraphCreator(nI, rI, cI, newOIArray, rPI, cPI, colI, area);
            Graph = GraphCreator.Graph;

            //List<DebugInformation> newDebugInfo = new List<DebugInformation>();
            //foreach (var vertex in Graph.Vertices)
            //{
            //    // rysowanie wierzchołków
            //    newDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(vertex.X - vertex.Width / 2, vertex.Y - vertex.Height / 2), new Size((int)vertex.Width, (int)vertex.Height), GeometryFriends.XNAStub.Color.Orange));

            //    // rysowanie krawędzi
            //    //foreach (Vertex neighbour in Graph.Edges[vertex].Keys)
            //        //newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(vertex.X, vertex.Y), new PointF(neighbour.X, neighbour.Y), GeometryFriends.XNAStub.Color.Black));
            //}
            //debugInfo = newDebugInfo.ToArray();

            //LevelDrawer.SaveImage(rI, cI, newOIArray, rPI, cPI, colI, Graph.Vertices, area);
        }

        //implements abstract circle interface: registers updates from the agent's sensors that it is up to date with the latest environment information
        /*WARNING: this method is called independently from the agent update - Update(TimeSpan elapsedGameTime) - so care should be taken when using complex 
         * structures that are modified in both (e.g. see operation on the "remaining" collection)      
         */
        public override void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
            nCollectiblesLeft = nC;

            rectangleInfo = rI;
            circleInfo = cI;
            collectiblesInfo = colI;
            lock (remaining)
            {
                remaining = new List<CollectibleRepresentation>(collectiblesInfo);
            }
        }

        //implements abstract circle interface: provides the circle agent with a simulator to make predictions about the future level state
        public override void ActionSimulatorUpdated(ActionSimulator updatedSimulator)
        {
            predictor = updatedSimulator;
        }

        //implements abstract circle interface: signals if the agent is actually implemented or not
        public override bool ImplementedAgent()
        {
            return implementedAgent;
        }

        //implements abstract circle interface: provides the name of the agent to the agents manager in GeometryFriends
        public override string AgentName()
        {
            return agentName;
        }

        //simple algorithm for choosing a random action for the circle agent
        private void RandomAction()
        {

            currentAction = possibleMoves[rnd.Next(possibleMoves.Count)];

            //send a message to the rectangle agent telling what action it chose
            messages.Add(new AgentMessage("Going to :" + currentAction));
        }

        //implements abstract circle interface: GeometryFriends agents manager gets the current action intended to be actuated in the enviroment for this agent
        public override Moves GetAction()
        {
            return currentAction;
        }

        //implements abstract circle interface: updates the agent state logic and predictions
        /*
        public override void Update(TimeSpan elapsedGameTime)
        {
        
            //Every second one new action is choosen
            if (lastMoveTime == 60)
                lastMoveTime = 0;

            if ((lastMoveTime) <= (DateTime.Now.Second) && (lastMoveTime < 60))
            {
                if (!(DateTime.Now.Second == 59))
                {
                    // currentAction = UCTSearch(predictor);
                    _CreateOtherVertices(predictor);
                    lastMoveTime = lastMoveTime + 1;
                }
                else
                    lastMoveTime = 60;
            }
        }
        */

        public override void Update(TimeSpan elapsedGameTime)
        {
            _CreateOtherVertices(predictor);

            lock (remaining)
            {
                if (remaining.Count > 0)
                {
                    foreach (var vertex in Graph.Vertices.Where(v => v.Type == VertexType.OnCollectible))
                    {
                        if (!remaining.Exists(collectible => collectible.X == vertex.X && collectible.Y == vertex.Y))
                            vertex.Type = VertexType.CaughtCollectible;
                    }
                }
            }

            MostImportantFunction();
        }

        public void test()
        {
            Graph g = new Graph();
            Vertex a = new Vertex(0, 0, 0, 0, 0);
            Vertex b = new Vertex(200, 100, 0, 0, 0);
            Vertex c = new Vertex(100, 200, 0, 0, 0);
            Vertex d = new Vertex(200, 400, 0, 0, 0);
            Vertex e = new Vertex(300, 100, 0, 0, 0);

            g.Vertices.Add(a);
            g.Vertices.Add(b);
            g.Vertices.Add(c);
            g.Vertices.Add(d);
            g.Vertices.Add(e);

            g.AddEdge(a, b);
            g.Edges[a][b].SuggestedTime=1;
            g.AddEdge(a, c);
            g.Edges[a][c].SuggestedTime = 1;
            g.AddEdge(c, b);
            g.Edges[c][b].SuggestedTime = 1;
            g.AddEdge(c, d);
            g.Edges[c][d].SuggestedTime = 1;
            g.AddEdge(d, e);
            g.Edges[d][e].SuggestedTime = 1;
            g.AddEdge(c, e);
            g.Edges[c][e].SuggestedTime = 2.5f;
            g.AddEdge(e, b);
            g.Edges[e][b].SuggestedTime = 1;

            try
            {
               var x= g.A_star(a, e);
                Debug.WriteLine(x.Item1);
            }
            catch(KeyNotFoundException cc)
            {
                Debug.WriteLine(cc.ToString());
                Debug.WriteLine("");
            }

        }

        public void test_fun()
        {

            foreach (var obs1 in this.GraphCreator.obstaclesInfo)
                Debug.WriteLine(Graph.GetAllVerticesOnObstacle(obs1).Count());

            int i = 0;
            foreach (var obs1 in this.GraphCreator.obstaclesInfo)
            {
                int j = 0;
                foreach (var obs2 in this.GraphCreator.obstaclesInfo)
                {
                    if (Graph.can_go_toobstacle(obs1, obs2) && obs1.GetHashCode() != obs2.GetHashCode())
                    {
                        //Debug.WriteLine("____________________________");
                        //Debug.WriteLine(obs1.X.ToString() + " " + obs1.Y.ToString());
                        //Debug.WriteLine(obs2.X.ToString() + " " + obs2.Y.ToString());
                        Debug.WriteLine(i.ToString() + " " + j.ToString());
                    }
                    //Debug.WriteLine(  Graph.can_go_toobstacle(obs1, obs2));
                    j++;
                }
                i++;
            }


        }

        //implements abstract circle interface: signals the agent the end of the current level
        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
            Log.LogInformation("CIRCLE - Collectibles caught = " + collectiblesCaught + ", Time elapsed - " + timeElapsed);
        }

        //implements abstract circle interface: gets the debug information that is to be visually represented by the agents manager
        public override DebugInformation[] GetDebugInformation()
        {
            return debugInfo;
        }

        //implememts abstract agent interface: send messages to the rectangle agent
        public override List<GeometryFriends.AI.Communication.AgentMessage> GetAgentMessages()
        {
            List<AgentMessage> toSent = new List<AgentMessage>(messages);
            messages.Clear();
            return toSent;
        }

        //implememts abstract agent interface: receives messages from the rectangle agent
        public override void HandleAgentMessages(List<GeometryFriends.AI.Communication.AgentMessage> newMessages)
        {
            foreach (AgentMessage item in newMessages)
            {
                Log.LogInformation("Circle: received message from rectangle: " + item.Message);
                if (item.Attachment != null)
                {
                    Log.LogInformation("Received message has attachment: " + item.Attachment.ToString());
                    if (item.Attachment.GetType() == typeof(Pen))
                    {
                        Log.LogInformation("The attachment is a pen, let's see its color: " + ((Pen)item.Attachment).Color.ToString());
                    }
                }
            }
        }


        // prawdopodobnie trzeba będzie te funkcje wrzucić do osobnej klasy (na razie są tutaj,
        // bo jeszcze nie wiem co nam będzie potrzebne z tego CircleAgenta :D)

        private MCTSTreeNode Expand(MCTSTreeNode node)
        {
            List<Moves> notUsedActions = possibleMoves.Except(node.Children.Select(c => c.Move)).ToList();
            Moves newMove = notUsedActions[rnd.Next(notUsedActions.Count)];

            return node.AddNewMove(newMove);
        }

        private void BackUp(MCTSTreeNode node, double value)
        {
            node.Backpropagation(value);
        }

        private MCTSTreeNode BestChild(MCTSTreeNode node, double c)
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

        private MCTSTreeNode TreePolicy(ActionSimulator simulator, MCTSTreeNode node)
        {
            while (true) // todo: może nie warto schodzić zbyt głęboko
            {
                if (node.Children.Count < possibleMoves.Count)
                    return Expand(node);
                else
                {
                    node = BestChild(node, CP);
                    simulator.AddInstruction(node.Move, 1);
                }
            }
        }

        private double DefaultPolicy(ActionSimulator simulator, MCTSTreeNode node)
        {
            const float SECONDS_OF_SIMULATION = 20;

            simulator.AddInstruction(node.Move, 1);

            for (int i = 0; i < SECONDS_OF_SIMULATION; i++)
                simulator.AddInstruction(possibleMoves[rnd.Next(possibleMoves.Count)], 1);

            simulator.Update(SECONDS_OF_SIMULATION);

            float min_d = area.Width * area.Height;
            // todo: uwzględnić jeszcze odległość do najbliższego niezebranego
            foreach (CollectibleRepresentation item in simulator.CollectiblesUncaught)
            {
                float d = 0; // bfs_heura(simulator.CirclePositionX, simulator.CirclePositionY, item.X, item.Y, true);
                if (d < min_d)
                    min_d = d;
            }

            //f(x)= 1/e^(x/CONST)^2    
            double CONST = 20;
            double f = 1 / Math.Exp((min_d / CONST) * (min_d / CONST));

            //g(x)=max(x/CONST2+1,0)
            double CONST2 = 600;
            double g = Math.Max(-min_d / CONST2 + 1, 0);

            int CollectiblesCaught = NumberOfCollectibles - simulator.CollectiblesUncaughtCount;
            return CollectiblesCaught + g;
        }

        public Moves UCTSearch(ActionSimulator simulator)
        {
            if (simulator == null) { return Moves.NO_ACTION; }

            MCTSTreeNode root = new MCTSTreeNode(Moves.NO_ACTION, null);
            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).Seconds < 1)
            {
                simulator.AddInstruction(Moves.NO_ACTION, 1);
                // simulator.SimulatorStep = 0.1f;

                MCTSTreeNode node = TreePolicy(simulator, root);
                double value = DefaultPolicy(simulator, node);
                BackUp(node, value);

                simulator.ResetSimulator();
            }

            MCTSTreeNode bestNode = BestChild(root, 0);

            Log.LogInformation("Root simulations: " + root.Simulations + ", value: " + root.Value);
            Log.LogInformation("Best node (" + bestNode.Move + ") simulations: " + bestNode.Simulations + ", value: " + bestNode.Value);

            return bestNode.Move;
        }

        private void _CreateOtherVertices(ActionSimulator simulator)
        {
            if (simulator == null || CreatedOtherVertices) return;

            List<DebugInformation> fallingDebugInfo = GraphCreator.AddFallingVertices(simulator);
            List<DebugInformation> jumpingDebugInfo = GraphCreator.AddJumpingVertices(simulator);
            List<DebugInformation> verticesDebugInfo = new List<DebugInformation>();
            List<DebugInformation> edgesDebugInfo = new List<DebugInformation>();
            List<DebugInformation> numbers = new List<DebugInformation>();

            GraphCreator.CreateEdges();

            foreach (var vertex in Graph.Vertices)
            {
                const float alpha = 0.33f;
                GeometryFriends.XNAStub.Color color = new GeometryFriends.XNAStub.Color(GeometryFriends.XNAStub.Color.Orange, alpha);
                if (vertex.Type == VertexType.FallenFromLeft || vertex.Type == VertexType.FallenFromRight) color = new GeometryFriends.XNAStub.Color(GeometryFriends.XNAStub.Color.CornflowerBlue, alpha);
                if (vertex.Type == VertexType.Jumping) color = new GeometryFriends.XNAStub.Color(GeometryFriends.XNAStub.Color.Brown, alpha);
                if (vertex.Type == VertexType.Rolling) color = new GeometryFriends.XNAStub.Color(GeometryFriends.XNAStub.Color.BlanchedAlmond, alpha);

                foreach (Vertex neighbour in Graph.Edges[vertex].Keys)
                    edgesDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(vertex.X, vertex.Y), new PointF(neighbour.X, neighbour.Y), GeometryFriends.XNAStub.Color.Black));

                verticesDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(vertex.X - vertex.Width / 2, vertex.Y - vertex.Height / 2), new Size((int)vertex.Width, (int)vertex.Height), color));
            }
            int nr = 0;
            foreach (var obs in this.GraphCreator.obstaclesInfo)
            {

                numbers.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(obs.X, obs.Y), nr.ToString(), GeometryFriends.XNAStub.Color.White));
                nr++;
            }

            List<Vertex> bestPath = Graph.FindBestPath();
            List<DebugInformation> bestPathDebugInfo = new List<DebugInformation>();
            Log.LogInformation("vertices in bestPath: " + bestPath.Count);

            foreach (var vertex in bestPath)
            {
            //    bestPathDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(vertex.X - vertex.Width / 2, vertex.Y - vertex.Height / 2), new Size((int)vertex.Width, (int)vertex.Height), GeometryFriends.XNAStub.Color.Red));
            }

            for (int i = 0; i < bestPath.Count - 1; i++)
            {
                bestPathDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(bestPath[i].X, bestPath[i].Y), new PointF(bestPath[i + 1].X, bestPath[i + 1].Y), GeometryFriends.XNAStub.Color.Yellow));
            }

            List<DebugInformation> newDebugInfo = new List<DebugInformation>();
            //newDebugInfo.AddRange(verticesDebugInfo);
            //newDebugInfo.AddRange(fallingDebugInfo);
            //newDebugInfo.AddRange(jumpingDebugInfo);
            //newDebugInfo.AddRange(edgesDebugInfo);
            newDebugInfo.AddRange(bestPathDebugInfo);
            //newDebugInfo.AddRange(numbers);
            staticDebugInfo = newDebugInfo;

            ObstacleRepresentation[] newOIArray = obstaclesInfo.Concat(borderObstacles).ToArray();
            MCTS_with_target = new MCTS_with_target(possibleMoves, 1.0 / Math.Sqrt(2), Graph, new BFSHeura(area, rectanglePlatformsInfo, circlePlatformsInfo, newOIArray));
            CreatedOtherVertices = true;
        }

        public static void RunSimulator(ActionSimulator simulator, Moves move, float timeMs)
        {
            simulator.AddInstruction(move, timeMs);
            simulator.Update(timeMs / 1000);
            simulator.Actions.Clear();
        }

        private void MostImportantFunction()
        {
            if (!CreatedOtherVertices) return;

            if (BestPath == null)
            {
                BestPath = Graph.FindBestPath();
                CurrentTargetIndex = 1;
                LastTimeOnPath = DateTime.Now;
            }

            Func<float, float> maxAllowedTime = suggestedTime => suggestedTime * 3.0f + 2;
            var sourceTargetEdge = Graph[BestPath[CurrentTargetIndex - 1]][BestPath[CurrentTargetIndex]];

            if ((DateTime.Now - LastTimeOnPath).TotalSeconds > maxAllowedTime(sourceTargetEdge.SuggestedTime))
            {
                var oldStartVertex = Graph.Vertices.Where(v => v.Type == VertexType.OnCircleStart).First();
                oldStartVertex.Type = VertexType.OnCircleStartOld;

                var newStartVertex = GraphCreator.CreateVertexUnderPosition(circleInfo.X, circleInfo.Y, VertexType.OnCircleStart);
                GraphCreator.CreateSameObstacleEdges(newStartVertex.Obstacle ?? obstaclesInfo[0]); // XD

                this.BestPath = Graph.FindBestPath();
                CurrentTargetIndex = 1;
                LastTimeOnPath = DateTime.Now;

                staticDebugInfo.Clear();
                for (int i = 0; i < BestPath.Count - 1; i++)
                {
                    staticDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(BestPath[i].X, BestPath[i].Y), new PointF(BestPath[i + 1].X, BestPath[i + 1].Y), GeometryFriends.XNAStub.Color.Yellow));
                }

                List<DebugInformation> newDebugInfo = new List<DebugInformation>();
                newDebugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());
                newDebugInfo.AddRange(staticDebugInfo);

                Vertex vertex = BestPath[CurrentTargetIndex];
                newDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(vertex.X - vertex.Width / 2, vertex.Y - vertex.Height / 2), new Size((int)vertex.Width, (int)vertex.Height), GeometryFriends.XNAStub.Color.BlanchedAlmond));

                debugInfo = newDebugInfo.ToArray();
            }

            if (MCTS_with_target.check_intersect(BestPath[CurrentTargetIndex], circleInfo.X, circleInfo.Y, circleInfo.Radius))
            {
                CurrentTargetIndex++;
                LastTimeOnPath = DateTime.Now;
                List<DebugInformation> newDebugInfo = new List<DebugInformation>();
                newDebugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());
                newDebugInfo.AddRange(staticDebugInfo);

                Vertex vertex = BestPath[CurrentTargetIndex];
                newDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(vertex.X - vertex.Width / 2, vertex.Y - vertex.Height / 2), new Size((int)vertex.Width, (int)vertex.Height), GeometryFriends.XNAStub.Color.BlanchedAlmond));

                debugInfo = newDebugInfo.ToArray();
            }

            if ((DateTime.Now - lastaction).TotalSeconds >= time_step)
            {
                currentAction = MCTS_with_target.UCTSearch_with_target(predictor, BestPath[CurrentTargetIndex - 1], BestPath[CurrentTargetIndex], time_step, currentAction);
                lastaction = DateTime.Now;
            }
        }
    }
}

