using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    [Serializable]
    public class StackList<T>
    {
        private List<T> stack;

        public StackList()
        {
            stack = new List<T>();
        }

        public int Count
        {
            get { return stack.Count; }
        }

        public void Push(T item)
        {
            stack.Add(item);
        }

        public T Pop()
        {
            if (stack.Count == 0)
            {
                throw new InvalidOperationException("The stack is empty.");
            }

            T item = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return item;
        }

        public T Peek()
        {
            if (stack.Count == 0)
            {
                throw new InvalidOperationException("The stack is empty.");
            }

            return stack[stack.Count - 1];
        }

        public void Clear()
        {
            stack.Clear();
        }
    }
}
