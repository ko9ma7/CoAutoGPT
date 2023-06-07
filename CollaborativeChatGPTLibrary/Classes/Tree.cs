using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    [Serializable]
    public class Tree<T>
    {
        private TreeNode<T> root;

        public Tree(T data)
        {
            root = new TreeNode<T>(data);
        }

        public TreeNode<T> GetRoot()
        {
            return root;
        }

        public static void InsertChildren(TreeNode<AgentTask> parentNode, List<TreeNode<AgentTask>> newNodes)
        {
            TreeNode<AgentTask> newRoot = parentNode;
            // Iterate over new nodes
            foreach (var newNode in newNodes)
            {
                TreeNode<AgentTask> cloned = newNode.ShallowClone();

                if (!newRoot.Data.Section.Equals(cloned.Parent.Data.Section))
                {
                    newRoot = cloned.Parent.ShallowClone();
                }

                
                // Add new node to parent's children list
                newRoot.Children.Add(cloned);

                // Set parent of new node
                cloned.Parent = newRoot;

                // Update section number
                if (string.IsNullOrEmpty(newRoot.Data.SectionNumber))
                {
                    //newNode.Data.SectionNumber = $"{newNode.Data.SectionNumber}.{newRoot.Children.Count}";
                }
                else
                {
                    cloned.Data.SectionNumber = $"{newRoot.Data.SectionNumber}.{newRoot.Children.Count}";
                }
                

                // Recursively update section numbers of children of new node
                UpdateSectionNumbers(cloned);
            }
        }

        public static void UpdateSectionNumbers(TreeNode<AgentTask> parentNode)
        {
            for (int i = 0; i < parentNode.Children.Count; i++)
            {
                var childNode = parentNode.Children[i];

                if (!string.IsNullOrEmpty(parentNode.Data.SectionNumber))
                {
                    if (parentNode.Data.SectionNumber[parentNode.Data.SectionNumber.Length - 1].Equals('.'))
                    {
                        childNode.Data.SectionNumber = $"{parentNode.Data.SectionNumber}{i + 1}";
                    }
                    else
                    {
                        childNode.Data.SectionNumber = $"{parentNode.Data.SectionNumber}.{i + 1}";
                    }
                }
                
                string[] levelParts = childNode.Data.SectionNumber.Split('.');
                List<int> sectionValueArray = new List<int>();

                for (int j = 0; j < levelParts.Length; j++)
                {
                    if (!string.IsNullOrEmpty(levelParts[j]))
                    {
                        sectionValueArray.Add(int.Parse(levelParts[j]));
                    }

                }

                childNode.Data.SectionParts = sectionValueArray;
                UpdateSectionNumbers(childNode);
            }
        }

        public void RemoveNode(TreeNode<T> nodeToRemove)
        {
            if (nodeToRemove == null)
            {
                return;
            }

            TreeNode<T> parentNode = FindParentNode(root, nodeToRemove);

            if (parentNode != null)
            {
                parentNode.Children.Remove(nodeToRemove);
            }
        }

        private TreeNode<T> FindParentNode(TreeNode<T> currentNode, TreeNode<T> nodeToRemove)
        {
            if (currentNode == null)
            {
                return null;
            }

            foreach (TreeNode<T> childNode in currentNode.Children)
            {
                if (childNode == nodeToRemove)
                {
                    return currentNode;
                }

                TreeNode<T> parent = FindParentNode(childNode, nodeToRemove);
                if (parent != null)
                {
                    return parent;
                }
            }

            return null;
        }

        public List<TreeNode<T>> GetAllNodes(TreeNode<T> parentNode)
        {
            List<TreeNode<T>> allNodes = new List<TreeNode<T>>();

            Traverse(parentNode, allNodes);

            return allNodes;
        }

        private void Traverse(TreeNode<T> node, List<TreeNode<T>> allNodes)
        {
            allNodes.Add(node);

            foreach (TreeNode<T> child in node.Children)
            {
                Traverse(child, allNodes);
            }
        }

        public TreeNode<T> AddChild(TreeNode<T> parent, T childData)
        {
            TreeNode<T> child = new TreeNode<T>(childData);
            child.Parent = parent;
            parent.Children.Add(child);

            return child;
        }

        public void Traverse(TreeNode<T> node)
        {
            Console.WriteLine(node.Data);

            foreach (TreeNode<T> child in node.Children)
            {
                Traverse(child);
            }
        }

        public void Traverse(TreeNode<T> node, string indent = "")
        {
            Console.WriteLine(indent + node.Data);

            foreach (TreeNode<T> child in node.Children)
            {
                Traverse(child, indent + "  ");
            }
        }
    }
}
