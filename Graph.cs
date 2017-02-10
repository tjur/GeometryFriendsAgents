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
                //Vertex current = f_score.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
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
                    float tentotive_g_score = g_score[current] + Edges[current][neighbor].Suggested_time;

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
            return Math.Max(Math.Abs(start.X - goal.X) / 210 , Math.Abs(start.Y - goal.Y) / 310);
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

        public float Suggested_time { get; set; }

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
