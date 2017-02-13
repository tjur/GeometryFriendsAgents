using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class BFSHeura
    {
        private FieldType[,] map;
        private int N, M;

        private int boxSize = 7;
        const int maxSize = 10000;

        private enum FieldType { Free, FreeForCircle, FreeForRectangle, NotFree }

        private int[,,,] Distances;

        Rectangle area;
        ObstacleRepresentation[] rectanglePlatformsInfo;
        ObstacleRepresentation[] circlePlatformsInfo;
        ObstacleRepresentation[] obstaclesInfo;

        public BFSHeura(Rectangle area, ObstacleRepresentation[] rectanglePlatformsInfo, ObstacleRepresentation[] circlePlatformsInfo, ObstacleRepresentation[] obstaclesInfo)
        {
            this.area = area;
            this.rectanglePlatformsInfo = rectanglePlatformsInfo;
            this.circlePlatformsInfo = circlePlatformsInfo;
            this.obstaclesInfo = obstaclesInfo;

            Init();
            //FloydWarshallInit();
        }

        void Init()
        {
            float Sx = area.Width;
            float Sy = area.Height;

            N = (int)(area.Width + 2 * area.X) / boxSize + 1;
            M = (int)(area.Height + 2 * area.Y) / boxSize + 1;

            map = new FieldType[N, M]; // 0 - empty , 1 - only circle can move , 2 - only rectangle can move, 3 - nobody can move

            for (int i = 0; i < N; i++)
                for (int j = 0; j < M; j++)
                    map[i, j] = FieldType.Free;


            for (int i = 0; i < N; i++)
                for (int j = 0; j < M; j++)
                {

                    Rectangle xij = new Rectangle(boxSize * i, boxSize * j, boxSize, boxSize);
                    foreach (ObstacleRepresentation obst in rectanglePlatformsInfo)
                    {
                        Rectangle recx = new Rectangle((int)obst.X - (int)obst.Width / 2, (int)obst.Y - (int)obst.Height / 2, (int)obst.Width, (int)obst.Height);
                        if (!Rectangle.Intersect(xij, recx).IsEmpty)
                            map[i, j] = FieldType.FreeForRectangle;
                    }

                    foreach (ObstacleRepresentation obst in circlePlatformsInfo)
                    {
                        Rectangle recx = new Rectangle((int)obst.X - (int)obst.Width / 2, (int)obst.Y - (int)obst.Height / 2, (int)obst.Width, (int)obst.Height);
                        if (!Rectangle.Intersect(xij, recx).IsEmpty)
                            map[i, j] = FieldType.FreeForCircle;
                    }

                    foreach (ObstacleRepresentation obst in obstaclesInfo)
                    {
                        Rectangle recx = new Rectangle((int)obst.X - (int)obst.Width / 2, (int)obst.Y - (int)obst.Height / 2, (int)obst.Width, (int)obst.Height);
                        if (!Rectangle.Intersect(xij, recx).IsEmpty)
                            map[i, j] = FieldType.NotFree;
                    }
                }
        }

        void debug_draw_map()
        {
            for (int i = 0; i < M; i++)
            {
                string str = "";
                for (int j = 0; j < N; j++)
                    str += map[j, i].ToString();

                Debug.WriteLine(str);
            }

            Debug.WriteLine("______________________________________");
        }

        public float BfsHeura(float x1, float y1, float x2, float y2, bool isCircle)
        {
            if (x1 >= area.X + area.Width || y1 >= area.Y + area.Height || x2 >= area.X + area.Width || y2 >= area.Y + area.Height || x1 <= area.X || x2 <= area.X || y1 <= area.Y || y2 <= area.Y)
            {
                return 2 * maxSize;
            }

            int i1 = (int)(x1) / boxSize;
            int j1 = (int)(y1) / boxSize;
            int i2 = (int)(x2) / boxSize;
            int j2 = (int)(y2) / boxSize;

            Queue<Tuple<int, int>> q = new Queue<Tuple<int, int>>();

            int[,] dist = new int[N, M];
            bool[,] visited = new bool[N, M];

            for (int i = 0; i < N; i++)
                for (int j = 0; j < M; j++)
                {
                    dist[i, j] = 0;
                    visited[i, j] = false;
                }

            visited[i1, j1] = true;
            q.Enqueue(new Tuple<int, int>(i1, j1));

            while (q.Count != 0)
            {
                Tuple<int, int> elem = q.Dequeue();
                int x = elem.Item1;
                int y = elem.Item2;

                if (x == i2 && y == j2)
                    return dist[i2, j2];

                List<Tuple<int, int>> neighbours = new List<Tuple<int, int>>();

                if (x > 0)
                    neighbours.Add(new Tuple<int, int>(x - 1, y));
                if (x < N - 1)
                    neighbours.Add(new Tuple<int, int>(x + 1, y));
                if (y > 0)
                    neighbours.Add(new Tuple<int, int>(x, y - 1));
                if (y < M - 1)
                    neighbours.Add(new Tuple<int, int>(x, y + 1));

                neighbours = neighbours
                                .Where(field => !visited[field.Item1, field.Item2] &&
                                       (map[field.Item1, field.Item2] == FieldType.Free ||
                                       (map[field.Item1, field.Item2] == FieldType.FreeForCircle && isCircle) ||
                                       (map[field.Item1, field.Item2] == FieldType.FreeForRectangle && !isCircle))).ToList();

                foreach (var field in neighbours)
                {
                    dist[field.Item1, field.Item2] = dist[x, y] + 1;
                    visited[field.Item1, field.Item2] = true;
                    q.Enqueue(field);
                }
            }

            return 2 * maxSize;

            /*dist[i1, j1] = 1;
            while (q.Count != 0)
            {
                Tuple<int, int> elem = q.Dequeue();
                int x = elem.Item1;
                int y = elem.Item2;
                int d = dist[x, y];
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        if (Math.Abs(i) + Math.Abs(j) == 1)
                            if (i + x >= 0 && i + x < N && j + y >= 0 && j + y < M)
                                if (map[i + x, j + y] == 0 || (isCircle == true && map[i + x, j + y] == 1) || (isCircle == false && map[i + x, j + y] == 2))
                                    if (dist[i + x, j + y] == 0)
                                    {
                                        if (i + x == i2 && j + y == j2)
                                            return d;

                                        dist[i + x, j + y] = d + 1;
                                        q.Enqueue(new Tuple<int, int>(i + x, j + y));
                                    }
            }

            //brak sciezki
            return max_size * 2;*/

        }

        public void FloydWarshallInit()
        {
            bool isCircle = true;

            Distances = new int[N, M, N, M];
            Tuple<int, int>[,,,] pred = new Tuple<int, int>[N, M, N, M];

            for (int x1 = 0; x1 < N; x1++)
            {
                for (int y1 = 0; y1 < M; y1++)
                {
                    for (int x2 = 0; x2 < N; x2++)
                    {
                        for (int y2 = 0; y2 < M; y2++)
                        {
                            Distances[x1, y1, x2, y2] = 2 * maxSize;
                            pred[x1, y1, x2, y2] = null;
                        }
                    }

                    Distances[x1, y1, x1, y1] = 0;
                }
            }

            for (int x = 0; x < N; x++)
            {
                for (int y = 0; y < M; y++)
                {
                    if (map[x, y] == FieldType.FreeForRectangle || map[x, y] == FieldType.NotFree) continue;

                    List<Tuple<int, int>> neighbours = new List<Tuple<int, int>>();

                    if (x > 0)
                        neighbours.Add(new Tuple<int, int>(x - 1, y));
                    if (x < N - 1)
                        neighbours.Add(new Tuple<int, int>(x + 1, y));
                    if (y > 0)
                        neighbours.Add(new Tuple<int, int>(x, y - 1));
                    if (y < M - 1)
                        neighbours.Add(new Tuple<int, int>(x, y + 1));

                    neighbours = neighbours
                                    .Where(field =>
                                           (map[field.Item1, field.Item2] == FieldType.Free ||
                                           (map[field.Item1, field.Item2] == FieldType.FreeForCircle && isCircle) ||
                                           (map[field.Item1, field.Item2] == FieldType.FreeForRectangle && !isCircle))).ToList();

                    foreach (var neighbour in neighbours)
                    {
                        Distances[x, y, neighbour.Item1, neighbour.Item2] = 1;
                        pred[x, y, neighbour.Item1, neighbour.Item2] = new Tuple<int, int>(x, y);
                    }
                }
            }

            for (int x1 = 0; x1 < N; x1++)
                for (int y1 = 0; y1 < M; y1++)
                    for (int x2 = 0; x2 < N; x2++)
                        for (int y2 = 0; y2 < M; y2++)
                            for (int x3 = 0; x3 < N; x3++)
                                for (int y3 = 0; y3 < M; y3++)
                                {
                                    int d = Distances[x2, y2, x1, y1] + Distances[x1, y1, x3, y3];

                                    if (Distances[x2, y2, x3, y3] > d)
                                    {
                                        Distances[x2, y2, x3, y3] = d;
                                        pred[x2, y2, x3, y3] = pred[x1, y1, x3, y3];
                                    }
                                }
        }

        public int GetDistance(float x1, float y1, float x2, float y2)
        {
            if (x1 >= area.X + area.Width || y1 >= area.Y + area.Height || x2 >= area.X + area.Width || y2 >= area.Y + area.Height || x1 <= area.X || x2 <= area.X || y1 <= area.Y || y2 <= area.Y)
            {
                return 2 * maxSize;
            }

            int i1 = (int)(x1 - area.X) / boxSize;
            int j1 = (int)(y1 - area.Y) / boxSize;
            int i2 = (int)(x2 - area.X) / boxSize;
            int j2 = (int)(y2 - area.Y) / boxSize;

            return Distances[i1, j1, i2, j2];
        }
    }
}
