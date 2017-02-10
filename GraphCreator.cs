﻿using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Debug;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class GraphCreator
    {
        private CountInformation numbersInfo;
        private RectangleRepresentation rectangleInfo;
        private CircleRepresentation circleInfo;
        private ObstacleRepresentation[] obstaclesInfo;
        private ObstacleRepresentation[] rectanglePlatformsInfo;
        private ObstacleRepresentation[] circlePlatformsInfo;
        private CollectibleRepresentation[] collectiblesInfo;
        private Rectangle area;

        private List<ObstacleRepresentation> AllObstacles;

        public Graph Graph { get; private set; }

        public GraphCreator(
                        CountInformation nI,
                        RectangleRepresentation rI,
                        CircleRepresentation cI,
                        ObstacleRepresentation[] oI,
                        ObstacleRepresentation[] rPI,
                        ObstacleRepresentation[] cPI,
                        CollectibleRepresentation[] colI,
                        Rectangle area)
        {
            numbersInfo = nI;
            rectangleInfo = rI;
            circleInfo = cI;
            obstaclesInfo = oI;
            rectanglePlatformsInfo = rPI;
            circlePlatformsInfo = cPI;
            collectiblesInfo = colI;
            this.area = area;
            AllObstacles = obstaclesInfo.Concat(circlePlatformsInfo).Concat(rectanglePlatformsInfo).ToList();

            CreateGraph();
        }

        public List<DebugInformation> AddFallingVertices(ActionSimulator simulator)
        {
            const float MAX_VELOCITY = 200;
            const float MAX_ANGULAR_VELOCITY = 4;
            const float VELOCITIES = 3;
            const float VELOCITY_STEP = MAX_VELOCITY / VELOCITIES;
            const float ANGULAR_VELOCITY_STEP = MAX_ANGULAR_VELOCITY / VELOCITIES;

            List<Vertex> fallenVertices = new List<Vertex>();
            List<DebugInformation> debugInformations = new List<DebugInformation>();

            foreach (Vertex vertex in Graph.Vertices)
            {
                if (vertex.Type != VertexType.OnObstacleLeft && vertex.Type != VertexType.OnObstacleRight) continue;

                for (int i = 1; i <= VELOCITIES; i++)
                {
                    PointF linearVelocity = new PointF(i * VELOCITY_STEP, 0);
                    float angularVelocity = i * ANGULAR_VELOCITY_STEP;

                    PointF position;
                    const float X_POSITION_OFFSET = 10;

                    if (vertex.Type == VertexType.OnObstacleLeft)
                    {
                        linearVelocity.X *= -1.0f;
                        angularVelocity *= -1.0f;
                        position = new PointF(vertex.X - vertex.Width / 2 + X_POSITION_OFFSET, vertex.Y + vertex.Height / 2 - simulator.CircleVelocityRadius);
                    }

                    else
                        position = new PointF(vertex.X + vertex.Width / 2 - X_POSITION_OFFSET, vertex.Y + vertex.Height / 2 - simulator.CircleVelocityRadius);

                    foreach (bool jump in new []{ true, false })
                    {
                        var tuple = _CreateFallenVertex(simulator, position, linearVelocity, angularVelocity, jump);
                        var fallenVertex = tuple.Item1;

                        var obstacleUnder = GetClosestObstacleUnder(fallenVertex);

                        // dopuszczalna odleglosc przeszkody od vertexa
                        const float IS_OKAY = 5;
                        if (obstacleUnder.Y - obstacleUnder.Height / 2 - (fallenVertex.Y + fallenVertex.Height / 2) <= IS_OKAY)
                        {
                            Graph.AddEdge(vertex, fallenVertex);
                            var edge = Graph.Edges[vertex][fallenVertex];
                            edge.SuggestedMove = jump ? Moves.JUMP : Moves.NO_ACTION;
                            edge.SuggestedXVelocity = linearVelocity.X;

                            fallenVertex.Obstacle = obstacleUnder;
                            fallenVertices.Add(fallenVertex);
                            debugInformations.AddRange(tuple.Item2);
                        }
                    }
                }
            }

            Graph.Vertices.AddRange(fallenVertices);
            return debugInformations;
        }

        public List<DebugInformation> AddJumpingVertices(ActionSimulator simulator)
        {
            const float MAX_VELOCITY = 200;
            const float MAX_ANGULAR_VELOCITY = 4;
            const float VELOCITIES = 3;
            const float VELOCITY_STEP = MAX_VELOCITY / VELOCITIES;
            const float ANGULAR_VELOCITY_STEP = MAX_ANGULAR_VELOCITY / VELOCITIES;

            List<Vertex> jumpingVertices = new List<Vertex>();
            List<DebugInformation> debugInformations = new List<DebugInformation>();

            foreach (Vertex vertex in Graph.Vertices)
            {
                if (vertex.Type != VertexType.FallenFromRight && vertex.Type != VertexType.FallenFromLeft) continue;

                for (int i = 1; i <= VELOCITIES; i++)
                {
                    PointF linearVelocity = new PointF(i * VELOCITY_STEP, 0);
                    float angularVelocity = i * ANGULAR_VELOCITY_STEP;

                    PointF position;
                    const float X_POSITION_OFFSET = 10;

                    if (vertex.Type == VertexType.FallenFromLeft)
                    {
                        linearVelocity.X *= -1.0f;
                        angularVelocity *= -1.0f;
                        position = new PointF(vertex.X - vertex.Width / 2 + X_POSITION_OFFSET, vertex.Y + vertex.Height / 2 - simulator.CircleVelocityRadius);
                    }

                    else
                        position = new PointF(vertex.X + vertex.Width / 2 - X_POSITION_OFFSET, vertex.Y + vertex.Height / 2 - simulator.CircleVelocityRadius);

                    var tuple = _CreateFallenVertex(simulator, position, linearVelocity, angularVelocity, true);
                    var jumpingVertex = tuple.Item1;
                    jumpingVertex.Type = VertexType.Jumping;

                    var obstacleUnder = GetClosestObstacleUnder(jumpingVertex);
                    jumpingVertex.Obstacle = obstacleUnder;

                    // nie chcemy wyladowac na tej samej przeszkodzie +
                    // dopuszczalna odleglosc przeszkody od vertexa
                    const float IS_OKAY = 5;
                    if (!vertex.Obstacle.Equals(obstacleUnder) &&
                        obstacleUnder.Y - obstacleUnder.Height / 2 - (jumpingVertex.Y + jumpingVertex.Height / 2) <= IS_OKAY)
                    {
                        var closestOnObstacle = Graph.GetAllVerticesOnObstacle(obstacleUnder)
                                                     .Aggregate((minVertex, v) =>
                                                        Math.Abs(jumpingVertex.X - minVertex.X) < Math.Abs(jumpingVertex.X - v.X) ?
                                                            minVertex : v
                                                     );

                        const float IS_CLOSE_ENOUGH = 100;
                        if (Math.Abs(jumpingVertex.X - closestOnObstacle.X) <= IS_CLOSE_ENOUGH)
                            jumpingVertex = closestOnObstacle;
                        else
                            jumpingVertices.Add(jumpingVertex);

                        Graph.AddEdge(vertex, jumpingVertex);
                        var edge = Graph.Edges[vertex][jumpingVertex];
                        edge.SuggestedMove = Moves.JUMP;
                        edge.SuggestedXVelocity = linearVelocity.X;

                        debugInformations.AddRange(tuple.Item2);
                    }
                }
            }

            Graph.Vertices.AddRange(jumpingVertices);
            return debugInformations;
        }

        private Tuple<Vertex, List<DebugInformation>> _CreateFallenVertex(ActionSimulator simulator, PointF position, PointF linearVelocity, float angularVelocity, bool jump)
        {
            const int vertexWidth = 50;
            const int vertexHeight = 40;

            simulator.ResetSimulator();
            simulator.DebugInfo = true;
            // simulator.SimulatorStep = 0.01f;

            ReflectionUtils.SetSimulator(simulator, position, linearVelocity, angularVelocity);

            if (jump) simulator.AddInstruction(Moves.JUMP, 100);
            simulator.Update(0.1f);
            simulator.Actions.Clear();
            simulator.Update(0.4f);

            while (simulator.CircleVelocityY <= 0)
                simulator.Update(0.01f);
            while (simulator.CircleVelocityY > 0)
                simulator.Update(0.01f);

            VertexType vertexType = simulator.CircleVelocityX > 0 ? VertexType.FallenFromLeft : VertexType.FallenFromRight;
            PointF vertexPosition = new PointF(simulator.CirclePositionX, simulator.CirclePositionY + simulator.CircleVelocityRadius - vertexHeight / 2);
            return Tuple.Create(new Vertex(vertexPosition.X, vertexPosition.Y, vertexWidth, vertexHeight, vertexType), simulator.SimulationHistoryDebugInformation);
        }

        private ObstacleRepresentation GetClosestObstacleUnder(Vertex vertex)
        {
            return AllObstacles
                                .Where(obst => obst.Y > vertex.Y && obst.X - obst.Width / 2 <= vertex.X && obst.X + obst.Width / 2 >= vertex.X)
                                .Aggregate((obst1, obst2) => (obst1.Y - vertex.Y) < (obst2.Y - vertex.Y) ? obst1 : obst2);
        }

        private void CreateGraph()
        {
            Graph = new Graph();

            CreateOnStartVertex();
            CreateObstacleStartEndVertices();
            CreateCollectibleVertices();

            // CreateEdges();
        }

        // tworzy wierzchołek (lub wierzchołki, gdy na planszy jest zarówno kółko i prostokąt) w miejscu startu
        private void CreateOnStartVertex()
        {
            float Width = 50;
            float Height = 40;

            // gdy aktualna mapa zawiera kółko
            if (!(circleInfo.X == -1000 && circleInfo.Y == -1000))
            {
                // znajdujemy przeszkodę na której leży kółko
                var obstacleUnder = AllObstacles
                                            .Where(obst => obst.Y > circleInfo.Y && obst.X - obst.Width / 2 <= circleInfo.X && obst.X + obst.Width / 2 >= circleInfo.X)
                                            .Aggregate((obst1, obst2) => (obst1.Y - circleInfo.Y) < (obst2.Y - circleInfo.Y) ? obst1 : obst2);

                Vertex vertex = new Vertex(circleInfo.X, obstacleUnder.Y - (obstacleUnder.Height / 2) - (Height / 2), Width, Height, VertexType.OnCircleStart, obstacleUnder);
                Graph.Vertices.Add(vertex);
            }

            // gdy aktualna mapa zawiera prostokąt
            if (!(rectangleInfo.X == -1000 && rectangleInfo.Y == -1000))
            {
                // znajdujemy przeszkodę na której leży prostokąt
                var obstacleUnder = AllObstacles
                                            .Where(obst => obst.Y > rectangleInfo.Y && obst.X - obst.Width / 2 <= rectangleInfo.X && obst.X + obst.Width / 2 >= rectangleInfo.X)
                                            .Aggregate((obst1, obst2) => (obst1.Y - rectangleInfo.Y) < (obst2.Y - rectangleInfo.Y) ? obst1 : obst2);

                Vertex vertex = new Vertex(rectangleInfo.X, obstacleUnder.Y - (obstacleUnder.Height / 2) - (Height / 2), Width, Height, VertexType.OnRectangleStart, obstacleUnder);
                Graph.Vertices.Add(vertex);
            }
        }

        // tworzy wierzchołki na początku i końcu każdej platformy (jeśli nie ma tam innej przeszkody lub kąta)
        private void CreateObstacleStartEndVertices()
        {
            foreach (var obstacle in obstaclesInfo)
                CreateStartEndVertex(obstacle);

            // foreach (var obstacle in circlePlatformsInfo)
                // CreateStartEndVertex(obstacle);

            foreach (var obstacle in rectanglePlatformsInfo)
                CreateStartEndVertex(obstacle);
        }

        private void CreateStartEndVertex(ObstacleRepresentation obstacle)
        {
            float leftTopCornerX = obstacle.X - (obstacle.Width / 2);
            float rightTopCornerX = obstacle.X + (obstacle.Width / 2);
            float upCornerY = obstacle.Y - (obstacle.Height / 2);

            float Width = 50;
            float Height = 40;

            Vertex vertex;

            // wąska przeszkoda - zamiat dwóch robimy jeden wierzchołek na całej przeszkodzie
            /*if (obstacle.Width <= Width)
            {
                vertex = new Vertex(obstacle.X, upCornerY - (Height / 2), obstacle.Width, Height, VertexType.OnWholeObstacle, obstacle);
                if (VertexNotCollide(vertex))
                    Graph.Vertices.Add(vertex);

                return;
            }*/

            vertex = new Vertex(leftTopCornerX + (Width / 2), upCornerY - (Height / 2), Width, Height, VertexType.OnObstacleLeft, obstacle);
            if (VertexNotCollide(vertex))
                Graph.Vertices.Add(vertex);
            vertex = new Vertex(rightTopCornerX - (Width / 2), upCornerY - (Height / 2), Width, Height, VertexType.OnObstacleRight, obstacle);
            if (VertexNotCollide(vertex))
                Graph.Vertices.Add(vertex);
        }

        // sprawdza czy podany wierzchołek nie nachodzi na jakąś przeszkodę lub nie wychodzi poza planszę
        // wierzchołek nie zostanie postawiony również wtedy, gdy jego lewy lub prawy bok styka się z jakąś przeszkodą
        // dzięki temu nie tworzą się wierzchołki w kątach
        private bool VertexNotCollide(Vertex vertex)
        {
            float top1 = vertex.Y - (vertex.Height / 2); float right1 = vertex.X + (vertex.Width / 2);
            float bottom1 = vertex.Y + (vertex.Height / 2); float left1 = vertex.X - (vertex.Width / 2);

            foreach (var obstacle in obstaclesInfo)
            {
                float top2 = obstacle.Y - (obstacle.Height / 2); float right2 = obstacle.X + (obstacle.Width / 2);
                float bottom2 = obstacle.Y + (obstacle.Height / 2); float left2 = obstacle.X - (obstacle.Width / 2);

                if (left1 <= right2 && right1 >= left2 && top1 < bottom2 && bottom1 > top2)
                    return false;
            }

            foreach (var obstacle in circlePlatformsInfo)
            {
                float top2 = obstacle.Y - (obstacle.Height / 2); float right2 = obstacle.X + (obstacle.Width / 2);
                float bottom2 = obstacle.Y + (obstacle.Height / 2); float left2 = obstacle.X - (obstacle.Width / 2);

                if (left1 <= right2 && right1 >= left2 && top1 < bottom2 && bottom1 > top2)
                    return false;
            }

            foreach (var obstacle in rectanglePlatformsInfo)
            {
                float top2 = obstacle.Y - (obstacle.Height / 2); float right2 = obstacle.X + (obstacle.Width / 2);
                float bottom2 = obstacle.Y + (obstacle.Height / 2); float left2 = obstacle.X - (obstacle.Width / 2);

                if (left1 <= right2 && right1 >= left2 && top1 < bottom2 && bottom1 > top2)
                    return false;
            }

            // to jest po to, aby wierzchołki nie powstały na obramowaniu planszy (po zewnętrznej stronie)
            if (vertex.Y < 0)
                return false;

            return true;
        }

        // tworzy wierzchołki w miejscu każdego diamencika oraz pod nim
        private void CreateCollectibleVertices()
        {
            int R = 40; // szer. i wys. wierzchołka na diamencie
            int Width = 50;
            int Height = 40;

            foreach (var coll in collectiblesInfo)
            {
                // na diamencie
                Vertex VertexOnCollectible = new Vertex(coll.X, coll.Y, R, R, VertexType.OnCollectible, null);
                Graph.Vertices.Add(VertexOnCollectible);

                // pod diamentem
                // znajdujemy najbliższą przeszkodę pod diamentem i tworzymy tam wierzchołek
                var obstacleUnder = AllObstacles
                                            .Where(obst => obst.Y > coll.Y && obst.X - obst.Width / 2 <= coll.X && obst.X + obst.Width / 2 >= coll.X)
                                            .Aggregate((obst1, obst2) => (obst1.Y - coll.Y) < (obst2.Y - coll.Y) ? obst1 : obst2);

                Vertex VertexUnderCollectible = new Vertex(coll.X, obstacleUnder.Y - (obstacleUnder.Height / 2) - (Height / 2), Width, Height, VertexType.UnderCollectible, obstacleUnder);
                Graph.Vertices.Add(VertexUnderCollectible);

                // krawędź w dół zawsze istnieje
                Graph.AddEdge(VertexOnCollectible, VertexUnderCollectible);

                // krawedź w górę istnieje, jeśli kulka doskoczy z przeszkody do diamentu
                int CollectibleDiagonal = 60; // długość przekątnej diamenta (tak na oko)
                int CircleMaxJumpHeight = 323; // max wysokość na jaką podskoczy kulka

                if ((obstacleUnder.Y - obstacleUnder.Height / 2) - 2 * circleInfo.Radius - CircleMaxJumpHeight <= coll.Y + CollectibleDiagonal / 2)
                {
                    Graph.AddEdge(VertexUnderCollectible, VertexOnCollectible);
                    Graph.Edges[VertexUnderCollectible][VertexOnCollectible].SuggestedMove = Moves.JUMP;
                }
            }        
        }

        public void CreateEdges()
        {
            foreach (var vertex in Graph.Vertices)
                if (!Graph.Edges.ContainsKey(vertex))
                    Graph.Edges.Add(vertex, new Dictionary<Vertex, Edge>());

            CreateSameObstacleEdges();
        }

        // łączy wierzchołki na tej samej przeszkodzie (jeśli nie ma pomiędzy nimi żadnej przeszkody)
        private void CreateSameObstacleEdges()
        {
            foreach (var obstacle in AllObstacles)
            {
                List<Vertex> VerticesOnObstacle = Graph.GetAllVerticesOnObstacle(obstacle);
                for (int i = 0; i < VerticesOnObstacle.Count - 1; i++)
                {
                    Vertex vertexLeft = VerticesOnObstacle[i];
                    Vertex vertexRight = VerticesOnObstacle[i + 1];
                    bool obstacleBetween = false;

                    foreach (var obstacle2 in AllObstacles)
                    {
                        float top = obstacle2.Y - (obstacle2.Height / 2);
                        float bottom = obstacle2.Y + (obstacle2.Height / 2);
                        float left = obstacle2.X - (obstacle2.Width / 2);
                        float right = obstacle2.X + (obstacle2.Width / 2);

                        if (left > vertexLeft.X && right < vertexRight.X && 
                            bottom <= obstacle.Y - (obstacle.Height / 2) &&
                            (bottom >= vertexLeft.Y - (vertexLeft.Height / 2) || bottom >= vertexRight.Y - (vertexRight.Height / 2)))
                        {
                            obstacleBetween = true;
                            break;
                        }
                    }

                    if (!obstacleBetween)
                    {
                        Graph.AddEdge(vertexLeft, vertexRight);
                        Graph.Edges[vertexLeft][vertexRight].SuggestedMove = Moves.ROLL_RIGHT;

                        Graph.AddEdge(vertexRight, vertexLeft);
                        Graph.Edges[vertexRight][vertexLeft].SuggestedMove = Moves.MOVE_LEFT;
                    }
                }
            }
        }
    }
}
