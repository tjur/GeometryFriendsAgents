﻿using GeometryFriends.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class MCTSTree
    {
        MCTSTreeNode Root { get; }

        public MCTSTree()
        {
            Root = new MCTSTreeNode(Moves.NO_ACTION, null);
        }
    }

    class MCTSTreeNode
    {
        public Moves Move { get; }
        public double Value { get; set; }
        public int Simulations { get; set; }

        public MCTSTreeNode Parent { get; set; }

        public List<MCTSTreeNode> Children { get; }

        public MCTSTreeNode(Moves Move, MCTSTreeNode Parent)
        {
            this.Move = Move;
            this.Parent = Parent;

            Value = 0;
            Simulations = 0;
            Children = new List<MCTSTreeNode>();
        }

        public bool IsRoot()
        {
            return Parent == null;
        }

        public bool IsLeaf()
        {
            return Children.Count == 0;
        }

        public MCTSTreeNode AddNewMove(Moves Move)
        {
            MCTSTreeNode child = new MCTSTreeNode(Move, this);
            Children.Add(child);

            return child;
        }

        public MCTSTreeNode MakeMove(Moves Move)
        {
            MCTSTreeNode child = Children.Find(node => node.Move == Move);
            if (child == null)
                throw new KeyNotFoundException();
            else
                return child;
        }

        // dodaje NewValue do Value każdego węzła na ścieżce do korzenia oraz zwiększa o 1 Simulations
        public void Backpropagation(double NewValue)
        {
            Value += NewValue;
            Simulations++;

            if (!IsRoot())
                Parent.Backpropagation(NewValue);
        }
    }
}
