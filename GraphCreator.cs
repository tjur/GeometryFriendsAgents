using GeometryFriends.AI.Perceptions.Information;
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

        private Graph graph;
        public Graph Graph
        {
            get
            {
                if (graph == null)
                    CreateGraph();

                return graph;
            }
        }

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
        }

        private void CreateGraph()
        {
            graph = new Graph();

            CreateOnStartVertex();
            CreateObstacleStartEndVertices();
            CreateCollectibleVertices();
        }

        // tworzy wierzchołek (lub wierzchołki, gdy na planszy jest zarówno kółko i prostokąt) w miejscu startu
        private void CreateOnStartVertex()
        {
            float Width = 50;
            float Height = 40;

            List<ObstacleRepresentation> AllObstacles = obstaclesInfo.Concat(circlePlatformsInfo).Concat(rectanglePlatformsInfo).ToList();

            // gdy aktualna mapa zawiera kółko
            if (!(circleInfo.X == -1000 && circleInfo.Y == -1000))
            {
                // znajdujemy przeszkodę na której leży kółko
                var obstacleUnder = AllObstacles
                                            .Where(obst => obst.Y > circleInfo.Y && obst.X - obst.Width / 2 <= circleInfo.X && obst.X + obst.Width / 2 >= circleInfo.X)
                                            .Aggregate((obst1, obst2) => (obst1.Y - circleInfo.Y) < (obst2.Y - circleInfo.Y) ? obst1 : obst2);

                Vertex vertex = new Vertex(circleInfo.X, obstacleUnder.Y - (obstacleUnder.Height / 2) - (Height / 2), Width, Height, VertexType.OnCircleStart, obstacleUnder);
                graph.Vertices.Add(vertex);
            }

            // gdy aktualna mapa zawiera prostokąt
            if (!(rectangleInfo.X == -1000 && rectangleInfo.Y == -1000))
            {
                // znajdujemy przeszkodę na której leży prostokąt
                var obstacleUnder = AllObstacles
                                            .Where(obst => obst.Y > rectangleInfo.Y && obst.X - obst.Width / 2 <= rectangleInfo.X && obst.X + obst.Width / 2 >= rectangleInfo.X)
                                            .Aggregate((obst1, obst2) => (obst1.Y - rectangleInfo.Y) < (obst2.Y - rectangleInfo.Y) ? obst1 : obst2);

                Vertex vertex = new Vertex(rectangleInfo.X, obstacleUnder.Y - (obstacleUnder.Height / 2) - (Height / 2), Width, Height, VertexType.OnRectangleStart, obstacleUnder);
                graph.Vertices.Add(vertex);
            }
        }

        // tworzy wierzchołki na początku i końcu każdej platformy (jeśli nie ma tam innej przeszkody lub kąta)
        private void CreateObstacleStartEndVertices()
        {
            foreach (var obstacle in obstaclesInfo)
                CreateStartEndVertex(obstacle);

            foreach (var obstacle in circlePlatformsInfo)
                CreateStartEndVertex(obstacle);

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
            if (obstacle.Width <= Width)
            {
                vertex = new Vertex(obstacle.X, upCornerY - (Height / 2), obstacle.Width, Height, VertexType.OnWholeObstacle, obstacle);
                if (VertexNotCollide(vertex))
                    graph.Vertices.Add(vertex);

                return;
            }

            vertex = new Vertex(leftTopCornerX + (Width / 2), upCornerY - (Height / 2), Width, Height, VertexType.OnObstacleLeft, obstacle);
            if (VertexNotCollide(vertex))
                graph.Vertices.Add(vertex);
            vertex = new Vertex(rightTopCornerX - (Width / 2), upCornerY - (Height / 2), Width, Height, VertexType.OnObstacleRight, obstacle);
            if (VertexNotCollide(vertex))
                graph.Vertices.Add(vertex);
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

            List<ObstacleRepresentation> AllObstacles = obstaclesInfo.Concat(circlePlatformsInfo).Concat(rectanglePlatformsInfo).ToList();

            foreach (var coll in collectiblesInfo)
            {
                Vertex vertex;

                // na diamencie
                vertex = new Vertex(coll.X, coll.Y, R, R, VertexType.OnCollectible, null);
                graph.Vertices.Add(vertex);

                // pod diamentem
                // znajdujemy najbliższą przeszkodę pod diamentem i tworzymy tam wierzchołek
                var obstacleUnder = AllObstacles
                                            .Where(obst => obst.Y > coll.Y && obst.X - obst.Width / 2 <= coll.X && obst.X + obst.Width / 2 >= coll.X)
                                            .Aggregate((obst1, obst2) => (obst1.Y - coll.Y) < (obst2.Y - coll.Y) ? obst1 : obst2);

                // czy sprawdzać tutaj czy ta znaleziona przeszkoda jest za nisko (tzn. kulka nie doskoczy z niej) i wtedy nie dodawać wierzchołka?
                vertex = new Vertex(coll.X, obstacleUnder.Y - (obstacleUnder.Height / 2) - (Height / 2), Width, Height, VertexType.UnderCollectible, obstacleUnder);
                graph.Vertices.Add(vertex);
            }        
        }
    }
}
