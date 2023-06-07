using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    [Serializable]
    public class TreeNode<T>// : ICloneable
    {
        public TreeNode<T> Parent { get; set; }

        public T Data { get; set; }
        public List<TreeNode<T>> Children { get; set; }

        public TreeNode(T data)
        {
            Data = data;
            Children = new List<TreeNode<T>>();
        }


        public TreeNode<T> ShallowClone()
        {
            //I only want the Data property.  Used for building new trees
            var newNode = (TreeNode<T>)this.MemberwiseClone();
            newNode.Data = (T)((ICloneable)this.Data).Clone();
            //newNode.Children = new List<TreeNode<T>>();
            //foreach (var child in this.Children)
            //{
            //    TreeNode<T> clonedChild = child.Clone();
            //    clonedChild.Parent = newNode;
            //    newNode.Children.Add(clonedChild);
            //}
            newNode.Children = new List<TreeNode<T>>();
            //newNode.Parent = null;
            return newNode;
        }

        public List<T> GetChildrenAsList()
        {
            List<T> children = new List<T>();

            foreach(TreeNode<T> node in Children)
            {
                children.Add(node.Data);
            }

            return children;
        }
    }
}
