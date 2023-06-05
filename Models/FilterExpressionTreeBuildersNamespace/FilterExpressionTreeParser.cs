﻿using Models.FilterExpressionNamespace;
using Models.FilterExpressionTreesNamespace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.FilterExpressionTreeBuildersNamespace
{
    public partial class FilterExpressionTreeBuilder
    {
        private bool parentSet;
        public bool TryFindNode(FilterExpressionTree? expression, string name, string value, out FilterExpressionTree? node)
        {
            parentSet = false;
            node = FindNode(expression, name, value);
            return node != null;
        }

        public FilterExpressionTree? FindNode(FilterExpressionTree? expressionTree, string? name, string? value = null)
        {
            if (FilterExpression.HasNoValue(expressionTree?.Expression?.Name, name))
                return null;
            else if (FilterExpression.NamesMatch(expressionTree.Expression.Name, _aliases[name]))
                return expressionTree;
            else if(FilterExpression.ValuesMatch(MapExpression(expressionTree.Expression.Value), value))
            {
                _aliases[name] = expressionTree.Expression.Name;
                return expressionTree;
            }
            //establish relationship between parent and found child node
            var leftChild = FindNode(expressionTree.Left, name, value);
            var rightChild = FindNode(expressionTree.Right, name, value);

            if (!parentSet && !FilterExpression.HasNoValue(leftChild, rightChild))
            {
                if (leftChild != null)
                {
                    leftChild.Parent = expressionTree;
                    expressionTree.Left = leftChild;
                }
                else
                {
                    rightChild.Parent = expressionTree;
                    expressionTree.Right = rightChild;
                }
                parentSet = true;
            }

            return leftChild ?? rightChild;
        }
    }
}
