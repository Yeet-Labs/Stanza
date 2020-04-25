using BenchmarkDotNet.Running;
using System;

namespace Stanza.Tests.Performance
{
    class Program
    {
        // TODO: Add CLI control for which benchmarks to run.

        static void Main()
        {
            BenchmarkRunner.Run<Wholistic>();
            BenchmarkRunner.Run<Randomized>();
        }
    }
}
