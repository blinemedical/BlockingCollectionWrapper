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
            var asyncCollection = new BlockingCollectionWrapper<string>();

            // make sure we dispose of it. this will stop the internal threads
            using (asyncCollection)
            {
                // register a consuming action
                asyncCollection.QueueConsumingAction = (producedItem) =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Console.WriteLine(DateTime.Now + ": Consuming item: " + producedItem);
                };

                // start consuming
                asyncCollection.Start();

                // start producing
                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine(DateTime.Now + ": Produced item " + i);
                    asyncCollection.AddItem(i.ToString());
                }
            }

            while (!asyncCollection.Finished)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            Assert.True(asyncCollection.Finished);
        }
    }
}
