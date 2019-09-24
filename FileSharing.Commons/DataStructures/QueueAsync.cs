using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Commons.DataStructures
{
    public class QueueAsync<T>
    {
        private readonly Queue<T> m_collection = new Queue<T>();
        private readonly Queue<TaskCompletionSource<T>> m_waiting = new Queue<TaskCompletionSource<T>>();

        /// <summary>
        /// The return value is approximate.
        /// </summary>
        public int Count
        {
            get
            {
                return this.m_collection.Count;
            }
        }

        public void Add(T item)
        {
            lock (this.m_collection)
            {
                bool isSet = false;
                while (this.m_waiting.Count > 0)
                {
                    TaskCompletionSource<T> tcs = this.m_waiting.Dequeue();
                    if (tcs.TrySetResult(item))
                    {
                        isSet = true;
                        break;
                    }
                }

                if (!isSet)
                {
                    this.m_collection.Enqueue(item);
                }
            }
        }

        public Task<T> Take()
        {
            lock (this.m_collection)
            {
                if (this.m_collection.Count > 0)
                    return Task.FromResult(this.m_collection.Dequeue());
                else
                {
                    var tcs = new TaskCompletionSource<T>();
                    this.m_waiting.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }

        public void Clear()
        {
            lock (this.m_collection)
            {
                while (this.m_waiting.Count != 0)
                {
                    var tcs = this.m_waiting.Dequeue();
                    tcs.TrySetCanceled();
                }

                this.m_collection.Clear();
            }
        }
    }
}
