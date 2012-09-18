using System;
using System.Threading;
using BlockingCollectionWrapper;
using NUnit.Framework;

namespace BlockingCollectionWrapperTests
{
    [TestFixture]
    public class BlockingCollectionTests
    {
        [Test]
        public void TestCollection()
        {
            // create the wrapper
            var asynchCollection = new BlockingCollectionWrapper<string>();

            // make sure we dispose of it. this will stop the internal threads
            using (asynchCollection)
            {
                // register a consuming action
                asynchCollection.QueueConsumingAction = (producedItem) =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Console.WriteLine(DateTime.Now + ": Consuming item: " + producedItem);
                };

                // start consuming
                asynchCollection.Start();

                // start producing
                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine(DateTime.Now + ": Produced item " + i);
                    asynchCollection.AddItem(i.ToString());
                }
            }

            while (!asynchCollection.Finished)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            Assert.True(asynchCollection.Finished);
        }
    }
}
