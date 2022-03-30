using Kabomu.Common.Internals;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Internals
{
    public class DefaultTransferCollectionTest
    {
        [Fact]
        public void TestUsageWithIncomingTransfers()
        {
            var instance = new DefaultTransferCollection<IncomingTransfer>();
            Assert.Equal(0, instance.Count);

            var actualItemList = new List<IncomingTransfer>();
            instance.ForEach(item => actualItemList.Add(item));
            Assert.Empty(actualItemList);

            instance.Clear();
            Assert.Empty(actualItemList);

            var item1 = new IncomingTransfer
            {
                MessageId = 1,
                StartedAtReceiver = true
            };
            var succeeded = instance.TryAdd(item1);
            Assert.True(succeeded);

            Assert.False(instance.TryAdd(item1));
            Assert.Equal(1, instance.Count);

            var item2 = new IncomingTransfer
            {
                MessageId = 1,
                StartedAtReceiver = true
            };
            Assert.False(instance.TryAdd(item2));

            item2.StartedAtReceiver = false;
            succeeded = instance.TryAdd(item2);
            Assert.True(succeeded);

            Assert.False(instance.TryAdd(item2));
            Assert.Equal(2, instance.Count);

            var item3 = new IncomingTransfer
            {
                MessageId = 10,
                StartedAtReceiver = true
            };
            succeeded = instance.TryAdd(item3);
            Assert.True(succeeded);
            Assert.False(instance.TryAdd(item3));
            Assert.Equal(3, instance.Count);

            var expected = item2;
            var actual = instance.TryGet(new IncomingTransfer { MessageId = 1, StartedAtReceiver = false });
            Assert.Equal(expected, actual);

            expected = item3;
            actual = instance.TryGet(new IncomingTransfer { MessageId = 10, StartedAtReceiver = true });
            Assert.Equal(expected, actual);

            expected = item1;
            actual = instance.TryGet(new IncomingTransfer { MessageId = 1, StartedAtReceiver = true });
            Assert.Equal(expected, actual);

            Assert.Null(instance.TryGet(new IncomingTransfer()));
            Assert.Equal(3, instance.Count);

            Assert.Null(instance.TryRemove(new IncomingTransfer()));
            Assert.Equal(3, instance.Count);

            expected = item2;
            actual = instance.TryRemove(new IncomingTransfer { MessageId = 1, StartedAtReceiver = false });
            Assert.Equal(expected, actual);
            Assert.Equal(2, instance.Count);

            actualItemList = new List<IncomingTransfer>();
            instance.ForEach(item => actualItemList.Add(item));
            if (!new List<IncomingTransfer> { item3, item1 }.Equals(actualItemList))
            {
                Assert.Equal(new List<IncomingTransfer> { item1, item3 }, actualItemList);
            }

            instance.Clear();
            Assert.Equal(0, instance.Count);
        }

        [Fact]
        public void TestUsageWithOutgoingTransfers()
        {
            var instance = new DefaultTransferCollection<OutgoingTransfer>();
            Assert.Equal(0, instance.Count);

            var actualItemList = new List<OutgoingTransfer>();
            instance.ForEach(item => actualItemList.Add(item));
            Assert.Empty(actualItemList);

            instance.Clear();
            Assert.Empty(actualItemList);

            var item1 = new OutgoingTransfer
            {
                MessageId = 1,
                StartedAtReceiver = true
            };
            var succeeded = instance.TryAdd(item1);
            Assert.True(succeeded);

            Assert.False(instance.TryAdd(item1));
            Assert.Equal(1, instance.Count);

            var item2 = new OutgoingTransfer
            {
                MessageId = 1,
                StartedAtReceiver = true
            };
            Assert.False(instance.TryAdd(item2));

            item2.StartedAtReceiver = false;
            succeeded = instance.TryAdd(item2);
            Assert.True(succeeded);

            Assert.False(instance.TryAdd(item2));
            Assert.Equal(2, instance.Count);

            var item3 = new OutgoingTransfer
            {
                MessageId = 10,
                StartedAtReceiver = true
            };
            succeeded = instance.TryAdd(item3);
            Assert.True(succeeded);
            Assert.False(instance.TryAdd(item3));
            Assert.Equal(3, instance.Count);

            var expected = item2;
            var actual = instance.TryGet(new OutgoingTransfer { MessageId = 1, StartedAtReceiver = false });
            Assert.Equal(expected, actual);

            expected = item3;
            actual = instance.TryGet(new OutgoingTransfer { MessageId = 10, StartedAtReceiver = true });
            Assert.Equal(expected, actual);

            expected = item1;
            actual = instance.TryGet(new OutgoingTransfer { MessageId = 1, StartedAtReceiver = true });
            Assert.Equal(expected, actual);

            Assert.Null(instance.TryGet(new OutgoingTransfer()));
            Assert.Equal(3, instance.Count);

            Assert.Null(instance.TryRemove(new OutgoingTransfer()));
            Assert.Equal(3, instance.Count);

            expected = item2;
            actual = instance.TryRemove(new OutgoingTransfer { MessageId = 1, StartedAtReceiver = false });
            Assert.Equal(expected, actual);
            Assert.Equal(2, instance.Count);

            actualItemList = new List<OutgoingTransfer>();
            instance.ForEach(item => actualItemList.Add(item));
            if (!new List<OutgoingTransfer> { item3, item1 }.Equals(actualItemList))
            {
                Assert.Equal(new List<OutgoingTransfer> { item1, item3 }, actualItemList);
            }

            instance.Clear();
            Assert.Equal(0, instance.Count);
        }
    }
}
