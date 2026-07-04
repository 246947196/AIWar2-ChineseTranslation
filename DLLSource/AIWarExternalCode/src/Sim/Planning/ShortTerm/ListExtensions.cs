using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Arcen.AIW2.External
{
    public static class ListExtensions
   {
       public static bool Empty(this IList list)
       {
           return (list.Count == 0);
       }

       public static void Swap<T>(this List<T> list, int idx0, int idx1)
       {
           var t = list[idx0];
           list[idx0] = list[idx1];
           list[idx1] = t;
       }

       /// <summary>
       /// Remove and returns the last element in a list.
       /// Throws an exception if the list is empty.
       /// </summary>       
      public static T PopLast<T>(this List<T> list)
      {
         if (list.Count == 0)
             throw new Exception("Cannot PopLast on an empty list.");
          
         var last = list.Last();
         list.RemoveAt(list.Count - 1);

         return last;
      }

      /// <summary>
      /// Returns the last element in a list without removing it.
      /// Throws an exception if the list is empty.
      /// </summary>
      public static T PeekLast<T>(this List<T> list)
      {
          if (list.Count == 0)
              throw new Exception("Cannot PeekLast on an empty list.");

          return list.Last();
      }

      /// <summary>
      /// Remove and returns the first element in a list.
      /// Throws an exception if the list is empty.
      /// </summary>    
      public static T PopFirst<T>(this List<T> list)
      {
          if (list.Count == 0)
              throw new Exception("Cannot PopLast on an empty list.");

         var first = list.First();
         list.RemoveAt(0);

         return first;
      }

      public static void AddUnique<T>(this List<T> list, T item)
      {
         if (!list.Contains(item))
            list.Add(item);
      }

      public static void AddRangeUnique<T>(this List<T> dstList, List<T> list)
      {
         foreach (var i in list)
         {
            if (!dstList.Contains(i))
               dstList.Add(i);
         }
      }

      [Conditional("VALIDATE")]
      public static void CheckDuplicates<T>(this List<T> list) where T : class
      {
#if VALIDATE
            foreach (var a in list)
            {
                int count = 0;

                foreach (var b in list)
                {
                    if (a == b)
                        count++;
                }

                Debug.Assert(count == 1, "Duplicate found.");
            }
#endif
      }

      // Compares a passed object type to its internal key.
      public interface IKeyComparer<T>
      {
         int Compare(T a);
      }

      public static T BinarySearch<T>(this List<T> list, IKeyComparer<T> comparer) where T : class
      {
         int min = 0;
         int max = list.Count - 1;

         while (min <= max)
         {
            int mid = (min + max)/2;
            int comparison = comparer.Compare(list[mid]);

            if (comparison == 0)
            {
               return list[mid];
            }

            if (comparison < 0)
            {
               min = mid + 1;
            }
            else
            {
               max = mid - 1;
            }
         }

         return null;
      }

      public static T BinarySearch<T>(this ReadOnlyCollection<T> list, IKeyComparer<T> comparer) where T : class
      {
         int min = 0;
         int max = list.Count - 1;

         while (min <= max)
         {
            int mid = (min + max)/2;
            int comparison = comparer.Compare(list[mid]);

            if (comparison == 0)
            {
               return list[mid];
            }

            if (comparison < 0)
            {
               min = mid + 1;
            }
            else
            {
               max = mid - 1;
            }
         }

         return null;
      }

      /// <summary>
      /// Insertion sort derived from http://www.csharp411.com/c-stable-sort/
      /// </summary>
      public static void InsertionSort<T>(this List<T> list, Comparison<T> comparison)
      {
         for (var j = 1; j < list.Count; j++)
         {
            T key = list[j];

            int i = j - 1;
            for (; i >= 0 && comparison(list[i], key) > 0; i--)
            {
               list[i + 1] = list[i];
            }
            list[i + 1] = key;
         }
      }

      /// <summary>
      /// Insertion sort, IComparer edition.
      /// </summary>
      public static void InsertionSort<T>(this List<T> list, IComparer<T> comparer)
      {
         for (var j = 1; j < list.Count; j++)
         {
            T key = list[j];

            int i = j - 1;
            for (; i >= 0 && comparer.Compare(list[i], key) > 0; i--)
            {
               list[i + 1] = list[i];
            }
            list[i + 1] = key;
         }
      }

      /// <summary>
      /// Delegate for returning the key for a given object.
      /// </summary>       
      public delegate TKey GetKeyMethod<TKey, TObj>(TObj obj);

       /// <summary>
       /// Delegate for comparing two objects of the same type.
       /// </summary>       
       public delegate int CompareKeysMethod<TKey>(TKey a, TKey b);

      /// <summary>
      /// Example: GuidSortedUnitList.BinarySearch( guid, e => e.Id );
      /// 
      /// It is safe to use Structures which implement IComparable as TKey.
      /// This will not generate garabage from boxing because this method is generic
      /// and does not need to do any casting.       
      /// </summary>        
      public static int BinarySearch<TKey, TObj>(this IList<TObj> list, TKey key, GetKeyMethod<TKey, TObj> getElementKey)
         where TKey : IComparable<TKey>
      {
         int min = 0;
         int max = list.Count - 1;

         while (min <= max)
         {
            int mid = (min + max)/2;
            int comparison = -(key.CompareTo(getElementKey(list[mid])));

            if (comparison == 0)
            {
               return mid;
            }

            if (comparison < 0)
            {
               min = mid + 1;
            }
            else
            {
               max = mid - 1;
            }
         }

         return ~min;
      }

      public static int BinarySearch<TKey, TObj>(this IList<TObj> list, TKey key, GetKeyMethod<TKey, TObj> getKey, CompareKeysMethod<TKey> compareKeys)          
      {
          int min = 0;
          int max = list.Count - 1;

          while (min <= max)
          {
              int mid = (min + max) / 2;
              int comparison = -(compareKeys(key, getKey(list[mid])));

              if (comparison == 0)
              {
                  return mid;
              }

              if (comparison < 0)
              {
                  min = mid + 1;
              }
              else
              {
                  max = mid - 1;
              }
          }

          return ~min;
      }

      public static int BinarySearch<TKey>(this IList<TKey> list, TKey key, CompareKeysMethod<TKey> compareKeys)
      {
          int min = 0;
          int max = list.Count - 1;

          while (min <= max)
          {
              int mid = (min + max) / 2;
              int comparison = -(compareKeys(key, list[mid]));

              if (comparison == 0)
              {
                  return mid;
              }

              if (comparison < 0)
              {
                  min = mid + 1;
              }
              else
              {
                  max = mid - 1;
              }
          }

          return ~min;
      }

      /// <summary>
      /// For a list of elements sorted by key, where key is expressed by
      /// the GetKeyMethod(element), return the index of the first element
      /// with key greater than or equal to the search key.
      /// </summary>                
      public static int BinarySearchEqualOrGreater<KEY, OBJ>(this IList<OBJ> list,
                                                             KEY key,
                                                             GetKeyMethod<KEY, OBJ> getElementKey)
         where KEY : IComparable
      {
         // NOTE: Not thoroughly tested yet!

         if (list == null)
            throw new ArgumentNullException("list");

         if (list.Count == 0)
            return 0;
         var comp = Comparer<KEY>.Default;

         int lo = 0, hi = list.Count - 1;

         while (lo < hi)
         {
            int m = (hi + lo)/2;

            if (comp.Compare(getElementKey(list[m]), key) < 0)
               lo = m + 1;
            else
               hi = m - 1;
         }

         if (comp.Compare(getElementKey(list[lo]), key) < 0)
            lo++;

         return lo;
      }

      public static ReadOnlyCollection<T> AsReadOnly<T>(this List<T> list)
      {
         return new ReadOnlyCollection<T>(list);
      }

      public static int CountContained<TSource>(this IEnumerable<TSource> source, TSource key)
      {
         int num = 0;
         foreach (TSource i in source)
         {
            if (i.Equals(key))
               num++;
         }
         return num;
      }
   }
}