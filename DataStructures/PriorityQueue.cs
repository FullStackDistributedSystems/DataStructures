﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace DataStructures
{
    public class PriorityQueue<T> : ICollection<T> where T : IComparable<T>
    {
        private readonly IComparer<T> _comparer;
        private T[] _heap;
        private const int DEFAULT_CAPACITY = 10;
        private const int SHRINK_RATIO = 4;
        private const int RESIZE_FACTOR = 2;

        private int _shrinkBound;
        private int _count;

        // ReSharper disable once StaticFieldInGenericType
        private static readonly InvalidOperationException EmptyCollectionException = new InvalidOperationException("Collection is empty.");

        public PriorityQueue(IComparer<T> comparer = null) : this(DEFAULT_CAPACITY, comparer) { }

        public PriorityQueue(int capacity, IComparer<T> comparer = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException("capacity", "Expected capacity greater than zero.");

            _comparer = comparer ?? Comparer<T>.Default;
            _shrinkBound = capacity / SHRINK_RATIO;
            _heap = new T[capacity];
        }

        public int Capacity { get { return _heap.Length; } }

        public IEnumerator<T> GetEnumerator()
        {
            var array = new T[Count];
            CopyTo(array, 0);
            return (IEnumerator<T>) array.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (Count == Capacity) GrowCapacity();

            _heap[_count++] = item;

            _heap.Sift(_count, _comparer); // move item "up" until heap principles are not met
        }

        public virtual T Take()
        {
            if (Count == 0) throw EmptyCollectionException;

            var item = _heap[0];
            _count--;
            _heap.Swap(0, _count);              // last element at count
            _heap[_count] = default(T);         // release hold on the object
            _heap.Sink(1, _count, _comparer);   // move item "down" while heap principles are not met            

            if (Count <= _shrinkBound && Count > DEFAULT_CAPACITY)
            {
                ShrinkCapacity();
            }

            return item;
        }

        public void Clear()
        {
            _heap = new T[DEFAULT_CAPACITY];
            _count = 0;
        }

        public bool Contains(T item)
        {
            return GetItemIndex(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException("arrayIndex");

            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Insufficient space in destination array.");
            
            Array.Copy(_heap, 0, array, arrayIndex, _count);

            array.HeapSort(arrayIndex, _count, _comparer);
        }

        public bool Remove(T item)
        {
            int index = GetItemIndex(item);
            switch (index)
            {
                case -1:
                    return false;
                case 0:
                    Take();
                    break;
                default:
                    _count--;
                    _heap.Swap(index, Count);   // last element at Count
                    _heap[Count] = default(T);  // release hold on the object
                    int parent = index / 2;     // get parent
                    // if new item at index is greater than it's parent then sift it up, else sink it down
                    if (_comparer.GreaterOrEqual(_heap[index], _heap[parent]))
                    {
                        _heap.Sift(index, _comparer);
                    }
                    else
                    {
                        _heap.Sink(index, Count, _comparer);
                    }
                    break;
            }

            return true;

        }

        public int Count
        {
            get { return _count; }
        }

        public bool IsReadOnly { get { return false; } }

        /// <summary>
        /// Returns index of the first occurrence of the given item or -1.
        /// </summary>
        private int GetItemIndex(T item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_comparer.Compare(_heap[i], item) == 0) return i;
            }
            return -1;
        }


        private void GrowCapacity()
        {
            int newCapacity = Capacity * RESIZE_FACTOR;
            Array.Resize(ref _heap, newCapacity);  // first element is at position 1
            _shrinkBound = newCapacity / SHRINK_RATIO;
        }

        private void ShrinkCapacity()
        {
            int newCapacity = Capacity / RESIZE_FACTOR;
            Array.Resize(ref _heap, newCapacity);  // first element is at position 1
            _shrinkBound = newCapacity / SHRINK_RATIO;
        }
    }

    internal static class HeapMethods
    {
        internal static void Swap<T>(this T[] array, int i, int j)
        {
            var tmp = array[i];
            array[i] = array[j];
            array[j] = tmp;
        }

        internal static bool GreaterOrEqual<T>(this IComparer<T> comparer, T x, T y)
        {
            return comparer.Compare(x, y) >= 0;
        }

        /// <summary>
        /// Moves the item with given index "down" the heap while heap principles are not met.
        /// </summary>
        /// <typeparam name="T">Any comparable type</typeparam>
        /// <param name="heap">Array, containing the heap</param>
        /// <param name="i">1-based index of the element to sink</param>
        /// <param name="count">Number of items in the heap</param>
        /// <param name="comparer">Comparer to compare the items</param>
        /// <param name="shift">Shift allows to compensate and work with arrays where heap starts not from the element at position 1.
        /// Default value of -1 allowes to work with 0-based heap as if it was 1-based. But the main reason for this is the CopyTo method.
        /// </param>
        internal static void Sink<T>(this T[] heap, int i, int count, IComparer<T> comparer, int shift = -1)
        {
            var lastIndex = count + shift;
            while (true)
            {
                var itemIndex = i + shift;
                var leftIndex = 2 * i + shift;
                if (leftIndex >= lastIndex) return;      // reached last item
                var rightIndex = leftIndex + 1;
                var hasRight = rightIndex < lastIndex;

                var item = heap[i + shift];
                var left = heap[leftIndex];
                var right = hasRight ? heap[rightIndex] : default(T);

                // if item is greater than children - exit
                if (GreaterOrEqual(comparer, item, left) && (!hasRight || GreaterOrEqual(comparer, item, right))) return;

                // else exchange with greater of children
                int greaterChildIndex = !hasRight || GreaterOrEqual(comparer, left, right) ? leftIndex : rightIndex;
                heap.Swap(itemIndex, greaterChildIndex);

                // continue at new position
                i = greaterChildIndex - shift;
            }
        }

        /// <summary>
        /// Moves the item with given index "up" the heap while heap principles are not met.
        /// </summary>
        /// <typeparam name="T">Any comparable type</typeparam>
        /// <param name="heap">Array, containing the heap</param>
        /// <param name="i">1-based index of the element to sink</param>
        /// <param name="comparer">Comparer to compare the items</param>
        /// <param name="shift">Shift allows to compensate and work with arrays where heap starts not from the element at position 1.
        /// Default value of -1 allowes to work with 0-based heap as if it was 1-based. But the main reason for this is the CopyTo method.
        /// </param>        
        internal static void Sift<T>(this T[] heap, int i, IComparer<T> comparer, int shift = -1)
        {
            while (true)
            {
                if (i <= 1) return;         // reached root
                int parent = i / 2 + shift; // get parent
                var index = i + shift;

                // if root is greater or equal - exit
                if (GreaterOrEqual(comparer, heap[parent], heap[index])) return;

                heap.Swap(parent, index);
                i = parent - shift;
            }
        }

        /// <summary>
        /// Sorts the heap in descending order.
        /// </summary>
        /// <typeparam name="T">Any comparable type</typeparam>
        /// <param name="heap">Array, containing the heap</param>
        /// <param name="startIndex">Index in the array, from which the heap structure begins</param>
        /// <param name="count">Number of items in the heap</param>
        /// <param name="comparer">Comparer to compare the items</param>
        internal static void HeapSort<T>(this T[] heap, int startIndex, int count, IComparer<T> comparer)
        {
            var shift = startIndex - 1;
            var lastIndex = startIndex + count;
            var left = count;
            while (lastIndex > startIndex)
            {
                lastIndex--;
                left--;
                heap.Swap(startIndex, lastIndex);
                heap.Sink(1, left, comparer, shift);
            }
            Array.Reverse(heap, startIndex, count);            
        }
    }
}
