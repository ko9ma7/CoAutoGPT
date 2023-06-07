using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ConversationNode
    {
        public Guid Id { get; set; }
        public string State { get; set; }
        public bool IsComplete { get; set; }

        public ConversationNode LeftChild { get; set; }

        public ConversationNode RightChild { get; set; }

        public float[] NodeEmbeddingValue { get; set; }

        public ConversationNode(string state, bool isComplete, float[] nodeEmbeddingValue)
        {
            Id = Guid.NewGuid();
            State = state;
            IsComplete = isComplete;
            NodeEmbeddingValue = nodeEmbeddingValue;
            LeftChild = null;
            RightChild = null;
        }
    }

    public class ConversationTree
    {
        public ConversationNode Root { get; set; }
        private Dictionary<Guid, ConversationNode> nodes = new Dictionary<Guid, ConversationNode>();

        public void Insert(ConversationNode newNode)
        {
            if (Root == null)
            {
                Root = newNode;
                nodes[newNode.Id] = newNode;
                return;
            }

            InsertNode(Root, newNode);
        }

        private void InsertNode(ConversationNode currentNode, ConversationNode newNode)
        {
            float currentSimilarity = CalculateSimilarity(currentNode.NodeEmbeddingValue, newNode.NodeEmbeddingValue);

            ConversationNode leftChild = currentNode.LeftChild;
            ConversationNode rightChild = currentNode.RightChild;

            if (leftChild == null && rightChild == null)
            {
                currentNode.LeftChild = newNode;
                nodes[newNode.Id] = newNode;
                return;
            }

            if (leftChild == null)
            {
                if (currentSimilarity < (float)CalculateSimilarity(rightChild.NodeEmbeddingValue, newNode.NodeEmbeddingValue))
                {
                    currentNode.LeftChild = newNode;
                    nodes[newNode.Id] = newNode;
                }
                else
                {
                    InsertNode(rightChild, newNode);
                }
                return;
            }

            if (rightChild == null)
            {
                if (currentSimilarity < (float)CalculateSimilarity(leftChild.NodeEmbeddingValue, newNode.NodeEmbeddingValue))
                {
                    currentNode.RightChild = newNode;
                    nodes[newNode.Id] = newNode;
                }
                else
                {
                    InsertNode(leftChild, newNode);
                }
                return;
            }

            float leftSimilarity = CalculateSimilarity(leftChild.NodeEmbeddingValue, newNode.NodeEmbeddingValue);
            float rightSimilarity = CalculateSimilarity(rightChild.NodeEmbeddingValue, newNode.NodeEmbeddingValue);

            if (currentSimilarity < leftSimilarity && currentSimilarity < rightSimilarity)
            {
                currentNode.LeftChild = newNode;
                nodes[newNode.Id] = newNode;
            }
            else if (leftSimilarity <= rightSimilarity)
            {
                InsertNode(leftChild, newNode);
            }
            else
            {
                InsertNode(rightChild, newNode);
            }
        }

        public void Remove(Guid id)
        {
            if (nodes.ContainsKey(id))
            {
                // Removal logic for your specific tree structure.

                // Finally, remove the node from the dictionary.
                nodes.Remove(id);
            }
        }

        public void BuildTree(List<ConversationNode> conversationNodes)
        {
            foreach (var node in conversationNodes)
            {
                Insert(node);
            }
        }

        public ConversationNode SearchBySimilarity(float[] targetEmbedding, float similarityThreshold)
        {
            return nodes.Values.FirstOrDefault(node => CalculateSimilarity(node.NodeEmbeddingValue, targetEmbedding) >= similarityThreshold);
        }

        public float CalculateSimilarity(float[] embedding1, float[] embedding2)
        {
            if (embedding1.Length != embedding2.Length)
            {
                throw new ArgumentException("Embeddings must have the same dimensionality.");
            }

            float sumOfSquaredDifferences = 0;

            for (int i = 0; i < embedding1.Length; i++)
            {
                float difference = embedding1[i] - embedding2[i];
                sumOfSquaredDifferences += difference * difference;
            }

            float euclideanDistance = (float)Math.Sqrt(sumOfSquaredDifferences);
            return euclideanDistance;
        }

        public ConversationNode SearchClosestNode(ConversationTree tree, float[] targetEmbedding)
        {
            if (tree == null || tree.Root == null)
            {
                throw new ArgumentException("The provided ConversationTree is null or has no nodes.");
            }

            ConversationNode closestNode = null;
            float minDistance = float.MaxValue;

            foreach (var node in tree.nodes.Values)
            {
                float distance = CalculateSimilarity(node.NodeEmbeddingValue, targetEmbedding);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestNode = node;
                }
            }

            return closestNode;
        }

    }

}
