using WorkMapper.Mappers;

namespace WorkMapper.Collections
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Runtime.CompilerServices;

    internal sealed class ContextMapperHashArray
    {
        private const int Factor = 3;

        private static readonly Node EmptyNode = new(typeof(EmptyKey), typeof(EmptyKey), typeof(object), default!);

        private readonly object sync = new();

        private readonly int initialSize;

        private Node[] nodes;

        private int depth;

        private int count;

        //--------------------------------------------------------------------------------
        // Constructor
        //--------------------------------------------------------------------------------

        public ContextMapperHashArray(int initialSize)
        {
            this.initialSize = initialSize;
            nodes = CreateInitialTable(initialSize);
        }

        //--------------------------------------------------------------------------------
        // Private
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateHash(Type sourceType, Type targetType, Type contextType)
        {
            unchecked
            {
                return sourceType.GetHashCode() ^ (targetType.GetHashCode() * 397) ^ contextType.GetHashCode();
            }
        }

        private static int CalculateDepth(Node node)
        {
            var length = 1;
            var next = node.Next;
            while (next is not null)
            {
                length++;
                next = next.Next;
            }

            return length;
        }

        private static int CalculateDepth(Node[] targetNodes)
        {
            var depth = 0;

            for (var i = 0; i < targetNodes.Length; i++)
            {
                var node = targetNodes[i];
                if (node != EmptyNode)
                {
                    depth = Math.Max(CalculateDepth(node), depth);
                }
            }

            return depth;
        }

        private static int CalculateSize(int requestSize)
        {
            uint size = 0;

            for (var i = 1L; i < requestSize; i *= 2)
            {
                size = (size << 1) + 1;
            }

            return (int)(size + 1);
        }

        private static Node[] CreateInitialTable(int size)
        {
            var newNodes = new Node[size];

            for (var i = 0; i < newNodes.Length; i++)
            {
                newNodes[i] = EmptyNode;
            }

            return newNodes;
        }

        private static Node FindLastNode(Node node)
        {
            while (node.Next is not null)
            {
                node = node.Next;
            }

            return node;
        }

        private static void UpdateLink(ref Node node, Node addNode)
        {
            if (node == EmptyNode)
            {
                node = addNode;
            }
            else
            {
                var last = FindLastNode(node);
                last.Next = addNode;
            }
        }

        private static void RelocateNodes(Node[] nodes, Node[] oldNodes)
        {
            for (var i = 0; i < oldNodes.Length; i++)
            {
                var node = oldNodes[i];
                if (node == EmptyNode)
                {
                    continue;
                }

                do
                {
                    var next = node.Next;
                    node.Next = null;

                    UpdateLink(ref nodes[CalculateHash(node.SourceType, node.TargetType, node.ContextType) & (nodes.Length - 1)], node);

                    node = next;
                }
                while (node is not null);
            }
        }

        private void AddNode(Node node)
        {
            var requestSize = Math.Max(initialSize, (count + 1) * Factor);
            var size = CalculateSize(requestSize);
            if (size > nodes.Length)
            {
                var newNodes = new Node[size];
                for (var i = 0; i < newNodes.Length; i++)
                {
                    newNodes[i] = EmptyNode;
                }

                RelocateNodes(newNodes, nodes);

                UpdateLink(ref newNodes[CalculateHash(node.SourceType, node.TargetType, node.ContextType) & (newNodes.Length - 1)], node);

                Interlocked.MemoryBarrier();

                nodes = newNodes;
                depth = CalculateDepth(newNodes);
                count++;
            }
            else
            {
                Interlocked.MemoryBarrier();

                UpdateLink(ref nodes[CalculateHash(node.SourceType, node.TargetType, node.ContextType) & (nodes.Length - 1)], node);

                depth = Math.Max(CalculateDepth(nodes[CalculateHash(node.SourceType, node.TargetType, node.ContextType) & (nodes.Length - 1)]), depth);
                count++;
            }
        }

        //--------------------------------------------------------------------------------
        // Public
        //--------------------------------------------------------------------------------

        public void Clear()
        {
            lock (sync)
            {
                var newNodes = CreateInitialTable(initialSize);

                Interlocked.MemoryBarrier();

                nodes = newNodes;
                depth = 0;
                count = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(Type sourceType, Type targetType, Type contextType, [NotNullWhen(true)] out ContextObjectMapperInfo? item)
        {
            var temp = nodes;
            var node = temp[CalculateHash(sourceType, targetType, contextType) & (temp.Length - 1)];
            do
            {
                if ((node.SourceType == sourceType) && (node.TargetType == targetType))
                {
                    item = node.Item;
                    return true;
                }
                node = node.Next;
            }
            while (node is not null);

            item = default;
            return false;
        }

        public ContextObjectMapperInfo AddIfNotExist(Type sourceType, Type targetType, Type contextType, Func<Type, Type, Type, ContextObjectMapperInfo> valueFactory)
        {
            lock (sync)
            {
                // Double checked locking
                if (TryGetValue(sourceType, targetType, contextType, out var currentValue))
                {
                    return currentValue!;
                }

                var value = valueFactory(sourceType, targetType, contextType);

                // Check if added by recursive
                if (TryGetValue(sourceType, targetType, contextType, out currentValue))
                {
                    return currentValue!;
                }

                AddNode(new Node(sourceType, targetType, contextType, value));

                return value;
            }
        }

        //--------------------------------------------------------------------------------
        // Inner
        //--------------------------------------------------------------------------------

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Framework only")]
        private sealed class EmptyKey
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Performance")]
        private sealed class Node
        {
            public readonly Type SourceType;

            public readonly Type TargetType;

            public readonly Type ContextType;

            public readonly ContextObjectMapperInfo Item;

            public Node? Next;

            public Node(Type sourceType, Type targetType, Type contextType, ContextObjectMapperInfo item)
            {
                SourceType = sourceType;
                TargetType = targetType;
                ContextType = contextType;
                Item = item;
            }
        }
    }
}