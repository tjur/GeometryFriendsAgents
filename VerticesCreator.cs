using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class VerticesCreator
    {
        private CountInformation numbersInfo;
        private RectangleRepresentation rectangleInfo;
        private CircleRepresentation circleInfo;
        private ObstacleRepresentation[] obstaclesInfo;
        private ObstacleRepresentation[] rectanglePlatformsInfo;
        private ObstacleRepresentation[] circlePlatformsInfo;
        private CollectibleRepresentation[] collectiblesInfo;
        private Rectangle area;

        public List<Vertex> Vertices { get; private set; }

        public VerticesCreator(
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

        public void CreateVertices()
        {
            if (Vertices == null)
            {
                Vertices = new List<Vertex>();

                CreateObstacleStartEndVertices();
                CreateCollectibleVertices();
            }
        }

        // tworzy wierzchołki na początku i końcu każdej platformy (jeśli nie ma tam innej przeszkody)
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

            // na razie bardzo proste tworzenie wierzchołków, sprawdza tylko kolizje z przeszkodami (wierzchołki mają jednakową szer. i wys.)

            // wąska przeszkoda - zamiat dwóch robimy jeden wierzchołek
            if (obstacle.Width <= Width)
            {
                vertex = new Vertex(obstacle.X, upCornerY - (Height / 2), obstacle.Width, Height);
                if (VertexNotCollide(vertex))
                    Vertices.Add(vertex);

                return;
            }

            vertex = new Vertex(leftTopCornerX + (Width / 2), upCornerY - (Height / 2), Width, Height);
            if (VertexNotCollide(vertex))
                Vertices.Add(vertex);
            vertex = new Vertex(rightTopCornerX - (Width / 2), upCornerY - (Height / 2), Width, Height);
            if (VertexNotCollide(vertex))
                Vertices.Add(vertex);
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
                // na diamencie
                Vertices.Add(new Vertex(coll.X, coll.Y, R, R));

                // pod diamentem
                // znajdujemy najbliższą przeszkodę pod diamentem i tworzymy tam wierzchołek
                var obstacleUnder = AllObstacles
                                            .Where(obst => obst.Y > coll.Y && obst.X - obst.Width / 2 <= coll.X && obst.X + obst.Width / 2 >= coll.X)
                                            .Aggregate((obst1, obst2) => (obst1.Y - coll.Y) < (obst2.Y - coll.Y) ? obst1 : obst2);
                
                // czy sprawdzać tutaj czy ta znaleziona przeszkoda jest za nisko (tzn. kulka nie doskoczy z niej) i wtedy nie dodawać wierzchołka?
                Vertices.Add(new Vertex(coll.X, obstacleUnder.Y - (obstacleUnder.Height / 2) - (Height / 2), Width, Height));
            }
                
        }
    }


    // wierzchołek grafu, będący tak naprawdę prostokątem na planszy
    class Vertex
    {
        // przechowywana pozycja, to pozycja środka
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }

        public Vertex(float X, float Y, float Width, float Height)
        {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
        }
    }
}
