using System;
using System.Collections.Generic;

namespace Code.Navigation.Pathfinding
{
    public class PriorityQueue<T> where T : IComparable<T>
    {
        public List<T> Heap = new();

        public int Count => Heap.Count;

        public void Clear()
        {
            Heap?.Clear();
        }

        public T Contains(T key)
        {
            int idx = Heap.IndexOf(key);
            return idx < 0 ? default : Heap[idx];
        }

        public void Push(T data)
        {
            Heap.Add(data); //데이터의 맨 끝에 새로운 데이터를 삽입한다.

            int now = Heap.Count - 1; //거기서부터 시작해서 부모까지 쭉 올라가며 비교를 시작한다.
            while (now > 0)
            {
                int next = (now - 1) / 2; //부모 인덱스를 구한다.
                if (Heap[now].CompareTo(Heap[next]) < 0 )  //비교 했는데 부모가 나보다 더 작다.
                    break; //제 위치를 찾았으니 멈춘다.

                //그렇지 않으면 2개의 값을 교환
                (Heap[now], Heap[next]) = (Heap[next], Heap[now]);
                now = next; //이제 부모인덱스가 자신이 된다. -> 다시 반복
            }
        }

        public T Pop()
        {
            T ret = Heap[0];
            int lastIdx = Heap.Count - 1;
            Heap[0] = Heap[lastIdx]; //마지막에 있는거를 맨 꼭대기 올린거
            Heap.RemoveAt(lastIdx); //마지막껄 제거
            lastIdx--;

            int now = 0;
            while (true)
            {
                int left = 2 * now + 1;
                int right = 2 * now + 2;

                int next = now;
                if (left <= lastIdx && Heap[next].CompareTo(Heap[left]) < 0) //왼쪽 자식이 나보다 작아.
                    next = left;

                if (right <= lastIdx && Heap[next].CompareTo(Heap[right]) < 0) //오른쪽 자식이 더 작아.
                    next = right;

                if(next == now)
                    break;

                (Heap[now], Heap[next]) = (Heap[next], Heap[now]);
                now = next;
            }

            return ret;
        }

        public T Peek()
        {
            return Heap.Count == 0 ? default : Heap[0];
        }
    }
}
