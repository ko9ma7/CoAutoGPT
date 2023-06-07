using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    [Serializable]
    public class TaskLinkedList
    {
        public TaskNode Head;

        public TaskLinkedList()
        {
            Head = null;
        }

        public void Add(AgentTask value)
        {
            TaskNode newNode = new TaskNode(value);

            if (Head == null)
            {
                Head = newNode;
            }
            else
            {
                TaskNode current = Head;
                while (current.Next != null)
                {
                    current = current.Next;
                }
                current.Next = newNode;
            }
        }

        public bool Remove(AgentTask value)
        {
            if (Head == null)
            {
                return false;
            }

            if (Head.Value == value)
            {
                Head = Head.Next;
                return true;
            }

            TaskNode current = Head;
            while (current.Next != null)
            {
                if (current.Next.Value == value)
                {
                    current.Next = current.Next.Next;
                    return true;
                }
                current = current.Next;
            }

            return false;
        }

        public bool Insert(AgentTask value, int position)
        {
            if (position < 0)
            {
                return false;
            }

            TaskNode newNode = new TaskNode(value);

            if (position == 0)
            {
                newNode.Next = Head;
                Head = newNode;
                return true;
            }

            int index = 1;
            TaskNode current = Head;
            while (current.Next != null)
            {
                if (index == position)
                {
                    newNode.Next = current.Next;
                    current.Next = newNode;
                    return true;
                }

                current = current.Next;
                index++;
            }

            if (index == position)
            {
                current.Next = newNode;
                return true;
            }

            return false;
        }

        public List<AgentTask> GetFlatList()
        {
            List<AgentTask> flatList = new List<AgentTask>();

            TaskNode current = Head;
            while (current != null)
            {
                flatList.Add(current.Value);
                current = current.Next;
            }

            return flatList;
        }

        public TaskNode GetNext()
        {
            TaskNode current = Head;
            while (current != null)
            {
                //Console.Write(current.Value + " -> ");
                current = current.Next;
            }

            //Increment
            Head = current;

            return current;
        }

        public void PrintList()
        {
            TaskNode current = Head;
            while (current != null)
            {
                Console.Write(current.Value + " -> ");
                current = current.Next;
            }
            Console.WriteLine("null");
        }
    }
}
