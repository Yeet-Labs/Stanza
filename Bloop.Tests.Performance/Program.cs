using BenchmarkDotNet.Running;
using System;

namespace Bloop.Tests.Performance
{
    class Program
    {
        // TODO: Parse arguments for which benchmark to run.

        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Wholistic>();
            BenchmarkRunner.Run<Randomized>();
        }
    }
}
