using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    static class LevelDrawer
    {
        // rysuje dany level (wraz z wyznaczonymi przez VerticesCreator wierzchołkami) i zapisuje obraz do pliku .png
        public static void SaveImage(
                             RectangleRepresentation rI,
                             CircleRepresentation cI,
                             ObstacleRepresentation[] oI,
                             ObstacleRepresentation[] rPI,
                             ObstacleRepresentation[] cPI,
                             CollectibleRepresentation[] colI,
                             List<Vertex> Vertices,
                             Rectangle area, 
                             string fileName = "levelView")
        {
            const int borderWidth = 40; // szerokość czarnej ramki otaczającej każdą planszę

            Bitmap bitmap = new Bitmap(area.Width + 2 * borderWidth, area.Height + 2 * borderWidth, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bitmap);

            g.Clear(Color.LightBlue);

            // wierzchołki utworzone przez VerticesCreator
            foreach (var vertex in Vertices)
                g.FillRectangle(Brushes.Pink, CreateRectangle(vertex));

            // przeszkody ogólne
            foreach (var obstacle in oI)
                g.FillRectangle(Brushes.Black, CreateRectangle(obstacle));

            // przeszkody tylko dla kółka
            foreach (var obstacle in cPI)
                g.FillRectangle(Brushes.Yellow, CreateRectangle(obstacle));

            // przeszkody tylko dla prostokąta
            foreach (var obstacle in rPI)
                g.FillRectangle(Brushes.Green, CreateRectangle(obstacle));

            // diamenty
            foreach (var collectible in colI)
                g.FillPolygon(Brushes.Purple, CreatePoints(collectible));

            bitmap.Save(fileName + ".png", ImageFormat.Png);
        }

        private static Rectangle CreateRectangle(ObstacleRepresentation obstacle)
        {
            float leftUpCornerX = obstacle.X - (obstacle.Width / 2);
            float leftUpCornerY = obstacle.Y - (obstacle.Height / 2);

            return new Rectangle((int)leftUpCornerX, (int)leftUpCornerY, (int)obstacle.Width, (int)obstacle.Height);
        }

        private static Rectangle CreateRectangle(Vertex vertex)
        {
            float leftUpCornerX = vertex.X - (vertex.Width / 2);
            float leftUpCornerY = vertex.Y - (vertex.Height / 2);

            return new Rectangle((int)leftUpCornerX, (int)leftUpCornerY, (int)vertex.Width, (int)vertex.Height);
        }

        private static System.Drawing.Point[] CreatePoints(CollectibleRepresentation collectible)
        {
            var Points = new System.Drawing.Point[4];
            float X = collectible.X;
            float Y = collectible.Y;
            int R = 30;

            Points[0] = new System.Drawing.Point((int)X, (int)(Y - R));
            Points[1] = new System.Drawing.Point((int)(X + R), (int)Y);
            Points[2] = new System.Drawing.Point((int)X, (int)(Y + R));
            Points[3] = new System.Drawing.Point((int)(X - R), (int)Y);
            return Points;
        }
    }
}
