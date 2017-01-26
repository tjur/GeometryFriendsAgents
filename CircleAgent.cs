using GeometryFriends;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Communication;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

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
        private Moves currentAction;
        private List<Moves> possibleMoves;
        private long lastMoveTime;
        private Random rnd;

        //predictor of actions for the circle
        private ActionSimulator predictor = null;
        private DebugInformation[] debugInfo = null;
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

        private int nCollectiblesLeft;

        private List<AgentMessage> messages;

        //Area of the game screen
        private Rectangle area;

        private MCTSTree MCTSTree;
        private double CP = 1 / Math.Sqrt(2);
        private const float SECONDS_OF_SIMULATION = 35;

        public CircleAgent()
        {
            //Change flag if agent is not to be used
            implementedAgent = true;

            //setup for action updates
            lastMoveTime = DateTime.Now.Second;
            currentAction = Moves.NO_ACTION;
            rnd = new Random();

            //prepare the possible moves  
            possibleMoves = new List<Moves>();
            possibleMoves.Add(Moves.ROLL_LEFT);
            possibleMoves.Add(Moves.ROLL_RIGHT);
            possibleMoves.Add(Moves.JUMP);
            possibleMoves.Add(Moves.NO_ACTION);         
      
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
        public override void Update(TimeSpan elapsedGameTime)
        {
            //Every second one new action is choosen
            if (lastMoveTime == 60)
                lastMoveTime = 0;

            if ((lastMoveTime) <= (DateTime.Now.Second) && (lastMoveTime < 60))
            {
                if (!(DateTime.Now.Second == 59))
                {
                    currentAction = UCTSearch(predictor);
                    lastMoveTime = lastMoveTime + 1;
                }
                else
                    lastMoveTime = 60;
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
            List<CollectibleRepresentation> caughtCollectibles = new List<CollectibleRepresentation>();

            simulator.AddInstruction(node.Move, 1);

            for (int i = 0; i < SECONDS_OF_SIMULATION; i++)
                simulator.AddInstruction(possibleMoves[rnd.Next(possibleMoves.Count)], 1);

            simulator.SimulatorCollectedEvent += (Object sender, CollectibleRepresentation collectibleCaught) => { caughtCollectibles.Add(collectibleCaught); };
            simulator.Update(SECONDS_OF_SIMULATION);

            // todo: uwzględnić jeszcze odległość do najbliższego niezebranego
            return caughtCollectibles.Count;
        }

        public Moves UCTSearch(ActionSimulator simulator)
        {
            MCTSTreeNode root = new MCTSTreeNode(Moves.NO_ACTION, null);
            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).Seconds < 1)
            {
                MCTSTreeNode node = TreePolicy(simulator, root);
                double value = DefaultPolicy(simulator, node);
                BackUp(node, value);
            }

            MCTSTreeNode bestNode = BestChild(root, 0);

            Log.LogInformation("Root simulations: " + root.Simulations + ", value: " + root.Value);
            Log.LogInformation("Best node simulations: " + bestNode.Simulations + ", value: " + bestNode.Value);
            return bestNode.Move;
        }
    }
}

