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
        private int[,] map;
        private int N, M;

        private int size_of_box = 3;
        const int max_size = 10000;

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

            bfs_heura_init();
        }


        //skromne funkcje aby uniknac krotek
        int c2to1(int x, int y)
        {
            return max_size * x + y;
        }


        int f1to2(int x)
        {
            return x / max_size;
        }

        int s1to2(int x)
        {
            return x % max_size;
        }

        void bfs_heura_init()
        {
            float Sx = area.Width;
            float Sy = area.Height;

            N = (int)(area.Width - area.X) / size_of_box + 1;

            M = (int)(area.Height - area.Y) / size_of_box + 1;

            map = new int[N, M]; // 0 - empty , 1- only circle can move , 2 - only rectangle can move, 3 nobody can move

            for (int i = 0; i < N; i++)
                for (int j = 0; j < M; j++)
                    map[i, j] = 0;


            for (int i = 0; i < N; i++)
                for (int j = 0; j < M; j++)
                {

                    Rectangle xij = new Rectangle(size_of_box * i + area.X, size_of_box * j + area.Y, size_of_box, size_of_box);
                    foreach (ObstacleRepresentation x in rectanglePlatformsInfo)
                    {
                        Rectangle recx = new Rectangle((int)x.X - (int)x.Width / 2 - area.X, (int)x.Y - (int)x.Height / 2 - area.Y, (int)x.Width, (int)x.Height);
                        if (!Rectangle.Intersect(xij, recx).IsEmpty)
                            map[i, j] = 2;
                    }

                    foreach (ObstacleRepresentation x in circlePlatformsInfo)
                    {
                        Rectangle recx = new Rectangle((int)x.X - (int)x.Width / 2 - area.X, (int)x.Y - (int)x.Height / 2 - area.Y, (int)x.Width, (int)x.Height);
                        if (!Rectangle.Intersect(xij, recx).IsEmpty)
                            map[i, j] = 1;
                    }

                    foreach (ObstacleRepresentation x in obstaclesInfo)
                    {
                        Rectangle recx = new Rectangle((int)x.X - (int)x.Width / 2 - area.X, (int)x.Y - (int)x.Height / 2 - area.Y, (int)x.Width, (int)x.Height);
                        if (!Rectangle.Intersect(xij, recx).IsEmpty)
                            map[i, j] = 3;


                    }

                }

        }

        void debug_draw_map()
        {
            for (int i = 0; i < M; i++)
            {
                string str = "";
                for (int j = 0; j < N; j++)
                {
                    str += map[j, i].ToString();


                }

                Debug.WriteLine(str);
            }

            Debug.WriteLine("______________________________________");

        }


        public float bfs_heura(float x1, float y1, float x2, float y2, bool iscircle)
        {
            if (x1 >= area.X + area.Width || y1 >= area.Y + area.Height || x2 >= area.X + area.Width || y2 >= area.Y + area.Height || x1 <= area.X || x2 <= area.X || y1 <= area.Y || y2 <= area.Y)
            {
                return max_size * 2;
            }

            int i1 = (int)(x1 - area.X) / size_of_box;
            int j1 = (int)(y1 - area.Y) / size_of_box;
            int i2 = (int)(x2 - area.X) / size_of_box;
            int j2 = (int)(y2 - area.Y) / size_of_box;

            Queue<int> q = new Queue<int>();

            int[,] dist = new int[N, M];

            for (int i = 0; i < N; i++)
                for (int j = 0; j < M; j++)
                    dist[i, j] = 0;

            q.Enqueue(c2to1(i1, j1));
            dist[i1, j1] = 1;
            while (q.Count != 0)
            {
                int z = q.Dequeue();
                int x = f1to2(z);
                int y = s1to2(z);
                int d = dist[x, y];
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        if (Math.Abs(i) + Math.Abs(j) == 1)
                            if (i + x >= 0 && i + x < N && j + y >= 0 && j + y < M)
                                if (map[i + x, j + y] == 0 || (iscircle == true && map[i + x, j + y] == 1) || (iscircle == false && map[i + x, j + y] == 2))
                                    if (dist[i + x, j + y] == 0)
                                    {

                                        if (i + x == i2 && j + y == j2)
                                            return d;

                                        dist[i + x, j + y] = d + 1;
                                        q.Enqueue(c2to1(i + x, j + y));

                                    }

            }



            //brak sciezki
            return max_size * 2;
        }
    }
}
