// -------------------------------------------------------------------------------------------------
//  <copyright file="ObservableCollectionExtensions.cs">
//
//    Copyright (c) 2025-2026 Sam Gerené
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
//  </copyright>
//  ------------------------------------------------------------------------------------------------

namespace Argus.Health.Pulse.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Extension methods for <see cref="ObservableCollection{T}"/>
    /// </summary>
    public static class ObservableCollectionExtensions
    {
        /// <summary>
        /// Changes the collection so that it contains the items of <paramref name="target"/> in the same
        /// order, by removing, inserting and moving single items. An item that stays is never removed and
        /// added again, so a view that is bound to the collection keeps its selection and scroll position
        /// </summary>
        /// <typeparam name="T">
        /// The type of the items
        /// </typeparam>
        /// <param name="collection">
        /// The <see cref="ObservableCollection{T}"/> to change
        /// </param>
        /// <param name="target">
        /// The items the collection contains afterwards; it must not contain the same item twice
        /// </param>
        public static void SyncWith<T>(this ObservableCollection<T> collection, IReadOnlyList<T> target)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(target);

            var wanted = new HashSet<T>(target);

            for (var index = collection.Count - 1; index >= 0; index--)
            {
                if (!wanted.Contains(collection[index]))
                {
                    collection.RemoveAt(index);
                }
            }

            var comparer = EqualityComparer<T>.Default;

            for (var index = 0; index < target.Count; index++)
            {
                var item = target[index];

                if (index < collection.Count && comparer.Equals(collection[index], item))
                {
                    continue;
                }

                var currentIndex = collection.IndexOf(item);

                if (currentIndex >= 0)
                {
                    collection.Move(currentIndex, index);
                }
                else
                {
                    collection.Insert(index, item);
                }
            }
        }
    }
}
