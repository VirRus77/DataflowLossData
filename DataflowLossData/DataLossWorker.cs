using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DataflowLossData
{
    public static class DataLossWorker
    {
        private const int CountElements = 10;
        private const int MessagePerInstance = 2;
        private static ICollection<int> Elements = Enumerable.Range(0, CountElements).ToArray();

        public static async Task<bool> WorkAsync()
        {
            ICollection<Func<int, int>> instances = new Func<int, int>[]
            {
                v => v,
                v => v,
                v => v,
                v => v,
            };

            var buffer = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 10 });

            var transformBlocks = instances.Select(
                    func => new TransformBlock<int, int>(
                        v => func(v),
                        new ExecutionDataflowBlockOptions
                        {
                            BoundedCapacity = MessagePerInstance,
                            MaxDegreeOfParallelism = MessagePerInstance,
                        }
                    )
                )
                .ToArray();

            var bufferOutput = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 10 });

            var actionList = new BlockingCollection<int>();
            var countAction = 0;

            var action = new ActionBlock<int>(
                v =>
                {
                    actionList.Add(v);
                    Interlocked.Increment(ref countAction);
                }
            );

            var inputLinks = transformBlocks
                .Select(transformBlock => buffer.LinkTo(transformBlock, new DataflowLinkOptions { PropagateCompletion = true }))
                .ToArray();
            var outputLinks = transformBlocks
                .Select(transformBlock => transformBlock.LinkTo(bufferOutput, new DataflowLinkOptions { PropagateCompletion = false }))
                .ToArray();

            var outputActionLink = bufferOutput.LinkTo(action, new DataflowLinkOptions { PropagateCompletion = true });

            var fillTask = Task.Run(
                async () =>
                {
                    foreach (var value in Elements)
                    {
                        await buffer.SendAsync(value);
                    }

                    buffer.Complete();
                }
            );

            await fillTask;

            Task.WaitAll(transformBlocks.Select(v => v.Completion).ToArray());
            bufferOutput.Complete();

            await action.Completion;

            if (actionList.Count != CountElements)
            {
                var actionValue = actionList
                    .ToArray();
                var notExist = Elements.Where(v => !actionValue.Contains(v)).ToArray();
                Console.WriteLine($"Not exist action [{countAction}]: {string.Join(", ", notExist)}");

                return false;
            }

            return true;
        }
    }
}
