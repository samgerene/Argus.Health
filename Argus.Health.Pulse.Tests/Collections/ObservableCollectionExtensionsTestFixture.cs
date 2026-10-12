// -------------------------------------------------------------------------------------------------
//  <copyright file="ObservableCollectionExtensionsTestFixture.cs">
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

namespace Argus.Health.Pulse.Tests.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;

    using Argus.Health.Pulse.Collections;

    using NUnit.Framework;

    /// <summary>
    /// Suite of tests for the <see cref="ObservableCollectionExtensions"/> class
    /// </summary>
    [TestFixture]
    public class ObservableCollectionExtensionsTestFixture
    {
        [Test]
        public void Verify_that_SyncWith_fills_an_empty_collection_in_the_target_order()
        {
            var collection = new ObservableCollection<string>();

            collection.SyncWith(new[] { "a", "b", "c" });

            Assert.That(collection, Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void Verify_that_SyncWith_removes_items_that_are_not_in_the_target()
        {
            var collection = new ObservableCollection<string> { "a", "b", "c", "d" };

            collection.SyncWith(new[] { "b", "d" });

            Assert.That(collection, Is.EqualTo(new[] { "b", "d" }));
        }

        [Test]
        public void Verify_that_SyncWith_inserts_new_items_at_their_target_position()
        {
            var collection = new ObservableCollection<string> { "a", "c" };

            collection.SyncWith(new[] { "a", "b", "c", "d" });

            Assert.That(collection, Is.EqualTo(new[] { "a", "b", "c", "d" }));
        }

        [Test]
        public void Verify_that_SyncWith_reorders_by_moving_and_never_removes_an_item_that_stays()
        {
            var collection = new ObservableCollection<string> { "a", "b", "c" };
            var actions = new List<NotifyCollectionChangedAction>();

            collection.CollectionChanged += (_, e) => actions.Add(e.Action);

            collection.SyncWith(new[] { "c", "b", "a" });

            Assert.That(collection, Is.EqualTo(new[] { "c", "b", "a" }));
            Assert.That(actions, Is.Not.Empty);
            Assert.That(actions, Has.All.EqualTo(NotifyCollectionChangedAction.Move));
        }

        [Test]
        public void Verify_that_SyncWith_raises_no_change_when_the_collection_already_matches()
        {
            var collection = new ObservableCollection<string> { "a", "b" };
            var changes = 0;

            collection.CollectionChanged += (_, _) => changes++;

            collection.SyncWith(new[] { "a", "b" });

            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void Verify_that_SyncWith_empties_the_collection_for_an_empty_target()
        {
            var collection = new ObservableCollection<string> { "a", "b" };

            collection.SyncWith(Array.Empty<string>());

            Assert.That(collection, Is.Empty);
        }

        [Test]
        public void Verify_that_SyncWith_throws_on_null_arguments()
        {
            var collection = new ObservableCollection<string>();

            Assert.That(() => ((ObservableCollection<string>)null).SyncWith(new[] { "a" }), Throws.ArgumentNullException);
            Assert.That(() => collection.SyncWith(null), Throws.ArgumentNullException);
        }
    }
}
