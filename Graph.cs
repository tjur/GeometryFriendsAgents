using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
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

        public void AddEdge(Vertex VertexFrom, Vertex VertexTo)
        {
            if (!Edges.ContainsKey(VertexFrom)) Edges.Add(VertexFrom, new Dictionary<Vertex, Edge>());
            Edges[VertexFrom][VertexTo] = new Edge(VertexFrom, VertexTo);
        }

        // dla danej przeszkody zwraca wszystkie wierzchołki, które się na niej znajdują (posortowane rosnąco po X)
        public List<Vertex> GetAllVerticesOnObstacle(ObstacleRepresentation? obstacle)
        {
            return Vertices.Where(vertex => vertex.Obstacle.Equals(obstacle)).OrderBy(vertex => vertex.X).ToList();
        }

        public Tuple<float, List<Vertex>> A_star(Vertex start, Vertex target)
        {
            //to be continued

            return new Tuple<float, List<Vertex>>(0f, null);
        }

        static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1)
                return list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1)
                                    .SelectMany(t => list.Where(o => !t.Contains(o)), (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        public List<Vertex> FindBestPath()
        {
            IEnumerable<Vertex> CollectiblesVertices = Vertices.Where(vertex => vertex.Type == VertexType.OnCollectible);
            List<Vertex> BestPath = new List<Vertex>();
            float BestPathCost = 100000f;
            Vertex StartVertex = Vertices.Where(vertex => vertex.Type == VertexType.OnCircleStart).First();

            foreach(List<Vertex> Permutation in GetPermutations(CollectiblesVertices, CollectiblesVertices.Count()))
            {
                Permutation.Insert(0, StartVertex);
                List<Vertex> Path = new List<Vertex>() { StartVertex };
                float PathCost = 0f;

                int i;
                for (i = 0; i < Permutation.Count - 1; i++)
                {
                    Vertex Start = Permutation[i];
                    Vertex Target = Permutation[i + 1];
                    var Result = A_star(Start, Target);

                    // nie istnieje ścieżka pomiędzy wierzchołkami lub przekroczyliśmy BestPathCost
                    if (Result == null || Result.Item1 + PathCost > BestPathCost)
                        break;

                    Path = (List<Vertex>)Path.Concat(Result.Item2.Skip(1));
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

        float Suggested_time { get; set; }

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
