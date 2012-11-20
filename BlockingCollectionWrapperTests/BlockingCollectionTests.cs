using System;
using System.Threading;
using BlockingCollectionWrapper;
using NUnit.Framework;

namespace BlockingCollectionWrapperTests
{
    [TestFixture]
    public class BlockingCollectionTests
    {
        private readonly ManualResetEvent _testMutex = new ManualResetEvent(false);

        [Test]
        public void TestCollection()
        {
            // create the wrapper
            var asyncCollection = new BlockingCollectionWrapper<string>();

            asyncCollection.FinishedEvent += FinishedEventHandler;

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

            _testMutex.WaitOne();

            Assert.True(asyncCollection.Finished);
        }

        private void FinishedEventHandler(object sender, BlockingCollectionEventArgs e)
        {
            _testMutex.Set();
        }
    }
}
