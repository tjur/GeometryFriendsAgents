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

        public Graph()
        {
            Vertices = new List<Vertex>();
        }

        // dla danej przeszkody zwraca wszystkie wierzchołki, które się na niej znajdują (posortowane rosnąco po X)
        public List<Vertex> GetAllVerticesOnObstacle(ObstacleRepresentation? obstacle)
        {
            return Vertices.Where(vertex => vertex.Obstacle.Equals(obstacle)).OrderBy(vertex => vertex.X).ToList();
        }
    }

    // typy wierzchołków
   public enum VertexType { OnCircleStart, OnRectangleStart, OnObstacleLeft, OnObstacleRight, OnWholeObstacle, OnCollectible, UnderCollectible, Fallen };

    // wierzchołek grafu, będący tak naprawdę prostokątem na planszy
   public class Vertex
    {
        // przechowywana pozycja to pozycja środka
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }

        // przeszkoda na której znajduje się wierzchołek lub null (dla typu OnCollectible)
        public ObstacleRepresentation? Obstacle { get; }

        public VertexType Type { get; }

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
}
