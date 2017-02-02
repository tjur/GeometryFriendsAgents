using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class PointsCreator
    {
        private CountInformation numbersInfo;
        private RectangleRepresentation rectangleInfo;
        private CircleRepresentation circleInfo;
        private ObstacleRepresentation[] obstaclesInfo;
        private ObstacleRepresentation[] rectanglePlatformsInfo;
        private ObstacleRepresentation[] circlePlatformsInfo;
        private CollectibleRepresentation[] collectiblesInfo;
        private Rectangle area;

        public List<Point> Points { get; }

        public PointsCreator(
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

            Points = new List<Point>();
        }

        public List<Point> CreatePoints()
        {
            CreateObstacleStartEndPoints();

            return Points;
        }

        // tworzy punkty na początku i końcu każdej platformy (jeśli nie ma tam innej przeszkody)
        private void CreateObstacleStartEndPoints()
        {
            foreach (var obstacle in obstaclesInfo)
                CreateStartEndPoint(obstacle);

            foreach (var obstacle in circlePlatformsInfo)
                CreateStartEndPoint(obstacle);

            foreach (var obstacle in rectanglePlatformsInfo)
                CreateStartEndPoint(obstacle);
        }

        private void CreateStartEndPoint(ObstacleRepresentation obstacle)
        {
            float leftTopCornerX = obstacle.X - (obstacle.Width / 2);
            float rightTopCornerX = obstacle.X + (obstacle.Width / 2);
            float upCornerY = obstacle.Y - (obstacle.Height / 2);

            float Width = 50;
            float Height = 40;

            Point point;

            // na razie bardzo proste tworzenie punktów, sprawdza tylko kolizje z przeszkodami (punkty mają jednakową szer. i wys.)

            // wąska przeszkoda - zamiat dwóch robimy jeden punkt
            if (obstacle.Width <= 2 * Width)
            {
                point = new Point(obstacle.X, upCornerY - (Height / 2), obstacle.Width, Height);
                if (PointNotCollide(point))
                    Points.Add(point);

                return;
            }

            point = new Point(leftTopCornerX + (Width / 2), upCornerY - (Height / 2), Width, Height);
            if (PointNotCollide(point))
                Points.Add(point);
            point = new Point(rightTopCornerX - (Width / 2), upCornerY - (Height / 2), Width, Height);
            if (PointNotCollide(point))
                Points.Add(point);
        }

        // sprawdza czy podany pounkt nie nachodzi na jakąś przeszkodę
        private bool PointNotCollide(Point point)
        {
            foreach(var obstacle in obstaclesInfo)
            {
                float top1 = point.Y - (point.Height / 2); float right1 = point.X + (point.Width / 2);
                float bottom1 = point.Y + (point.Height / 2); float left1 = point.X - (point.Width / 2);

                float top2 = obstacle.Y - (obstacle.Height / 2); float right2 = obstacle.X + (obstacle.Width / 2);
                float bottom2 = obstacle.Y + (obstacle.Height / 2); float left2 = obstacle.X - (obstacle.Width / 2);

                if (left1 < right2 && right1 > left2 && top1 < bottom2 && bottom1 > top2)
                    return false;
            }

            foreach (var obstacle in circlePlatformsInfo)
            {
                float top1 = point.Y - (point.Height / 2); float right1 = point.X + (point.Width / 2);
                float bottom1 = point.Y + (point.Height / 2); float left1 = point.X - (point.Width / 2);

                float top2 = obstacle.Y - (obstacle.Height / 2); float right2 = obstacle.X + (obstacle.Width / 2);
                float bottom2 = obstacle.Y + (obstacle.Height / 2); float left2 = obstacle.X - (obstacle.Width / 2);

                if (left1 < right2 && right1 > left2 && top1 < bottom2 && bottom1 > top2)
                    return false;
            }

            foreach (var obstacle in rectanglePlatformsInfo)
            {
                float top1 = point.Y - (point.Height / 2); float right1 = point.X + (point.Width / 2);
                float bottom1 = point.Y + (point.Height / 2); float left1 = point.X - (point.Width / 2);

                float top2 = obstacle.Y - (obstacle.Height / 2); float right2 = obstacle.X + (obstacle.Width / 2);
                float bottom2 = obstacle.Y + (obstacle.Height / 2); float left2 = obstacle.X - (obstacle.Width / 2);

                if (left1 < right2 && right1 > left2 && top1 < bottom2 && bottom1 > top2)
                    return false;
            }

            return true;
        }
    }


    // punkt (wierzchołek grafu), będący tak naprawdę prostokątem
    class Point
    {
        // przechowywana pozycja, to pozycja środka
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }

        public Point(float X, float Y, float Width, float Height)
        {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
        }
    }
}
