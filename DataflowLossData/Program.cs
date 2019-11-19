using System;

namespace DataflowLossData
{
    class Program
    {
        static void Main(string[] args)
        {
            var iteration = 0;
            var lossIteration = 0;
            while (iteration < 100000)
            {
                var isLost = DataLossWorker.WorkAsync().Result;
                if (!isLost)
                {
                    lossIteration++;
                    Console.WriteLine($"Iteration: {iteration} lost data.");
                }
                iteration++;
                if (lossIteration >= 10)
                {
                    break;
                }
            }

            Console.WriteLine("Press <Enter>");
            Console.ReadLine();
        }
    }
}
