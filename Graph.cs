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
        }
    }
}
