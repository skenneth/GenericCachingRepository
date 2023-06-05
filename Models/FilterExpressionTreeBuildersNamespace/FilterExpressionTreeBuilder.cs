﻿using Models.DictionaryNamespace;
using Models.FilterExpressionNamespace;
using Models.FilterExpressionTreesNamespace;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace Models.FilterExpressionTreeBuildersNamespace
{
    public partial class FilterExpressionTreeBuilder
    {
        private List<FilterExpressionTree> _roots = new List<FilterExpressionTree>();
        public IEnumerable<FilterExpressionTree> Roots { get { return _roots; } }
        private AliasDictionary _aliases { get; set; } = new AliasDictionary();
        public IEnumerable<KeyValuePair<string, string>> Aliases { get => _aliases.AsEnumerable(); }
        private FilterExpressionTree CreateNode([Required] string name, bool hasNot)
            =>
            new FilterExpressionTree()
            {
                Expression = new FilterExpressionMetadata()
                {
                    IsNot = hasNot,
                    Name = name,
                    Value = null
                }
            };
        public void RemoveAlias(string alias) => _aliases.RemoveAlias(alias);

        public void Add(FilterExpression Expression)
        {
            if (Expression == null)
                return;
            FilterExpressionMetadata expression = new FilterExpressionMetadata(Expression);

            var (leftName, rightName) = Expression.GetExpressionChildrenNames();
            var (leftNot, rightNot) = (HasNot(leftName), HasNot(rightName));
            (leftName, rightName) = NormalizeAndRegisterAliases(leftName, rightName);
            var name  = expression.Name;

            if (FilterExpression.HasNoValue(name))
                throw new ArgumentException("Expression Name expected but was null or empty", nameof(expression.Name));

            //if node was found, then root has value
            FilterExpressionTree? node = null;
            var root = _roots.FirstOrDefault(root => TryFindNode(root, name, MapExpression(expression), out node));

            if (node != null && node.Expression?.Value != null)
            {
                if (expression.Value == node.Expression.Value)
                    throw new ArgumentException($"Expression '{name}' matches '{node.Expression.Name}' : '{expression.Value}'");
                else if (name == node.Expression.Name)
                    throw new ArgumentException($"Expression Name '{name}' matches '{node.Expression.Name}'");
                else if (MapExpression(expression) == node.Expression.Value && expression.Name != node.Expression.Name)
                {

                    var equivalentExpressionNames = $"{expression.Name} = {node.Expression.Name}";
                    var equivalentExpressions = MapAliasCorrelation(expression, node.Expression);
                    var equivalentMessage = $" {equivalentExpressions}";
                    throw new ArgumentException($"{equivalentExpressionNames} are equivalent.{equivalentMessage}");
                }
                throw new ArgumentException("unknown. Unexpected matching expressions");
            }

            //if root is null, then node is null. Assign node to root after creation
            if (root == null) //Disjoint Tree
            {
                root = CreateNode(NormalizeExpressionName(name), false);
                node = root;
                _roots.Add(root);
            }

            node.Expression = expression;
            node.Expression.Name = _aliases[expression.Name];
            //create placeholders to build a single disjoint tree
            node.Left = CreateNode(leftName, leftNot);
            node.Right = CreateNode(rightName, rightNot);
            _aliases.TryAddAlias(node.Expression.Name, node.Expression.Name);

            TryConnectTrees(node, node.Left);
            TryConnectTrees(node, node.Right);

            SetParent(node, node.Left);
            SetParent(node, node.Right);
        }

        private bool TryConnectTrees(FilterExpressionTree parent, FilterExpressionTree childNode)
        {
            if (FilterExpression.HasNoValue(childNode?.Expression?.Name))
                return false;
            //just compare _roots
            var root = _roots.FirstOrDefault(root => FilterExpression.NamesMatch(childNode.Expression.Name, root?.Expression?.Name));

            if (root != null)
            {
                //root is ChildNode
                var isLeftChild = FilterExpression.NamesMatch(parent.Left.Expression.Name, childNode.Expression.Name);
                //childNode is only name placeholder - update with Root of disjoint tree
                if (isLeftChild)
                    parent.Left = root;
                else
                    parent.Right = root;
                root.Parent = parent;
                _roots.Remove(root);
                return true;
            }
            return false;
        }

        private void SetParent(FilterExpressionTree parent, FilterExpressionTree? child)
        {
            if (child != null)
                child.Parent = parent;
        }
    }
}