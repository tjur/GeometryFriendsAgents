using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class Graph
    {
        public List<Vertex> Vertices { get; }
        public Dictionary<Vertex, Dictionary<Vertex, Edge>> Edges;

        public Graph()
        {
            Vertices = new List<Vertex>();
            Edges = new Dictionary<Vertex, Dictionary<Vertex, Edge>>();
        }

        public Edge AddEdge(Vertex VertexFrom, Vertex VertexTo)
        {
            if (!Edges.ContainsKey(VertexFrom)) Edges.Add(VertexFrom, new Dictionary<Vertex, Edge>());
            return Edges[VertexFrom][VertexTo] = new Edge(VertexFrom, VertexTo);
        }

        // dla danej przeszkody zwraca wszystkie wierzchołki, które się na niej znajdują (posortowane rosnąco po X)
        public List<Vertex> GetAllVerticesOnObstacle(ObstacleRepresentation? obstacle)
        {
            return Vertices.Where(vertex => vertex.Obstacle.Equals(obstacle)).OrderBy(vertex => vertex.X).ToList();
        }


        public int dfs_init(Vertex start, Vertex goal)
        {
            Dictionary<Vertex, bool> vis = new Dictionary<Vertex, bool>();
            foreach (var v in this.Vertices)
                vis[v] = false;



            return dfs(start,goal,vis);
        }
        public int dfs(Vertex start, Vertex goal,Dictionary<Vertex,bool> vis)
        {
            if (start == goal)
                return 1;

            vis[start] = true;
            int is_all = 0;
            if (Edges.Keys.Contains(start))
                foreach (var neighbor in Edges[start].Keys)
                {
                    if (vis[neighbor]==false)
                    {
                        if (dfs(neighbor, goal, vis) == 1)
                            is_all = 1;
                    }
                }


                    return is_all;
        }

        public Tuple<float, List<Vertex>> A_star(Vertex start, Vertex goal)
        {
            HashSet<Vertex> closedset=new HashSet<Vertex>();
            HashSet<Vertex> openset=new HashSet<Vertex>();
            Dictionary<Vertex, float> g_score = new Dictionary<Vertex, float>();
            Dictionary<Vertex, float> f_score = new Dictionary<Vertex, float>();
            Dictionary<Vertex, Vertex> cameFrom = new Dictionary<Vertex, Vertex>();

            openset.Add(start);

            foreach (var ver in this.Vertices)
            {
                g_score[ver] = float.PositiveInfinity;
                f_score[ver] = float.PositiveInfinity;
            }

            
            g_score[start] = 0;
            f_score[start] = heuristic_cost_estimate(start, goal);

            while (openset.Count!=0)
            {
                Vertex current = openset.Aggregate((l, r) => f_score[l] < f_score[r] ? l : r);

                if (current == goal)
                    return new Tuple<float, List<Vertex>>(g_score[goal], reconstruct_path(cameFrom,current));

                openset.Remove(current);
                closedset.Add(current);

                if (Edges.Keys.Contains(current))
                foreach (var neighbor in Edges[current].Keys)
                {
                    if (closedset.Contains(neighbor))
                        continue;
                    float tentotive_g_score = g_score[current] + Edges[current][neighbor].SuggestedTime;

                    if (!openset.Contains(neighbor))
                        openset.Add(neighbor);
                    else if (tentotive_g_score >= g_score[neighbor])
                        continue;


                    cameFrom[neighbor] = current;
                    g_score[neighbor] = tentotive_g_score;
                    f_score[neighbor] = g_score[neighbor] + heuristic_cost_estimate(neighbor, goal);
                    
                }

            }
            

            return null;
        }
        private List<Vertex> reconstruct_path(Dictionary<Vertex, Vertex> cameFrom, Vertex current)
        {
            List < Vertex > total_path = new List<Vertex>();
            total_path.Add(current);

            while (cameFrom.Keys.Contains(current))
            {
                current = cameFrom[current];
                total_path.Add(current);
            }

            total_path.Reverse();

            return total_path;
        }

        private float heuristic_cost_estimate(Vertex start, Vertex goal)
        {
            return Math.Max(Math.Abs(start.X - goal.X) / 210, Math.Abs(start.Y - goal.Y) / 310);
        }

        private static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1)
                return list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1)
                                    .SelectMany(t => list.Where(o => !t.Contains(o)), (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        public void Fun()
        {
            List<Vertex> CollectiblesVertices = Vertices.Where(vertex => vertex.Type == VertexType.OnCollectible).ToList();
            for (int i = 0; i < CollectiblesVertices.Count; i++)
                for (int j = 0; j < CollectiblesVertices.Count; j++)
                {
                    Debug.WriteLine("__________________________");
                    Debug.WriteLine(i.ToString() + " " + j);

                    Debug.WriteLine(CollectiblesVertices[i].X.ToString() + " " + CollectiblesVertices[i].Y.ToString());

                    Debug.WriteLine(CollectiblesVertices[j].X.ToString() + " " + CollectiblesVertices[j].Y.ToString());

                    Debug.WriteLine(A_star(CollectiblesVertices[i], CollectiblesVertices[j]));
                    Debug.WriteLine(dfs_init(CollectiblesVertices[i], CollectiblesVertices[j]));

                }


        }


       
       public bool can_go_toobstacle(ObstacleRepresentation start,ObstacleRepresentation? goal)
        {
            foreach (Vertex ver in GetAllVerticesOnObstacle(start))
            {
                if (Edges.Keys.Contains(ver))
                    foreach (var neighbor in Edges[ver].Keys)
                    {
                        if (neighbor.Obstacle.GetHashCode() == goal.GetHashCode())
                            return true;
                    }

            }


            return false;
        }



        public List<Vertex> FindBestPath()
        {
            IEnumerable<Vertex> CollectiblesVertices = Vertices.Where(vertex => vertex.Type == VertexType.OnCollectible);
            List<Vertex> BestPath = new List<Vertex>();
            float BestPathCost = 100000f;
            Vertex StartVertex = Vertices.Where(vertex => vertex.Type == VertexType.OnCircleStart).First();

            foreach(IEnumerable<Vertex> PermutationEnumerable in GetPermutations(CollectiblesVertices, CollectiblesVertices.Count()))
            {
                List<Vertex> Permutation = PermutationEnumerable.ToList();
                Permutation.Insert(0, StartVertex);
                List<Vertex> Path = new List<Vertex>() { StartVertex };
                float PathCost = 0f;

                int i;
                for (i = 0; i < Permutation.Count - 1; i++)
                {
                    Vertex Start = Permutation[i];
                    Vertex Target = Permutation[i + 1];
                    var Result = A_star(Start, Target);

                    if (Result == null || Result.Item1 + PathCost > BestPathCost)
                        break;

                    Path = Path.Concat(Result.Item2.Skip(1)).ToList();
                    PathCost += Result.Item1;
                }

                if (i == Permutation.Count - 1 && PathCost < BestPathCost)
                {
                    BestPath = Path;
                    BestPathCost = PathCost;
                }
            }

            return BestPath;
        }
    }

    // typy wierzchołków
   public enum VertexType { OnCircleStart, OnRectangleStart, OnObstacleLeft, OnObstacleRight, OnWholeObstacle, OnCollectible, UnderCollectible, FallenFromLeft, FallenFromRight, Jumping };

    // wierzchołek grafu, będący tak naprawdę prostokątem na planszy
   public class Vertex
    {
        // przechowywana pozycja to pozycja środka
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }

        // przeszkoda na której znajduje się wierzchołek lub null (dla typu OnCollectible)
        public ObstacleRepresentation? Obstacle { get; set; }

        public VertexType Type { get; set; }

        public Vertex(float X, float Y, float Width, float Height, VertexType Type, ObstacleRepresentation? Obstacle = null)
        {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Type = Type;
            this.Obstacle = Obstacle;
        }
    }

    public class Edge
    {
        Vertex VertexFrom { get; }
        Vertex VertexTo { get; }

        public float SuggestedTime { get; set; }
        public Moves SuggestedMove { get; set; }
        public float SuggestedXVelocity { get; set; }

        public Edge(Vertex VertexFrom, Vertex VertexTo)
        {
            this.VertexFrom = VertexFrom;
            this.VertexTo = VertexTo;
            this.SuggestedMove = Moves.NO_ACTION;
            this.SuggestedXVelocity = 0f;
        }
    }
}
