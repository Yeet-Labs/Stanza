using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using Maybe.BloomFilter;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BenchmarkDotNet.Jobs;

namespace Stanza.Tests.Performance
{
    // TODO: Add chain processing tests to calcualte overhead.

    [RPlotExporter, SimpleJob(RuntimeMoniker.NetCoreApp31)]
    public class Randomized
    {
        Evaluator<string> LateBuiltWholeFeatureSetEvaluationTestProcessor => Execution.Everything.BuildEvaluator();

        Evaluator<string> BuiltWholeFeatureSetEvaluationTestProcessor { get; } = Execution.Everything.BuildEvaluator();

        [Params(1, 10, 100, 1000)]
        public int Length { get; set; }

        public ScalableBloomFilter<string> DataClashFilter { get; set; }

        public Random Generator { get; } = new Random { };

        public string Data => new string(Enumerable.Range(0, Length).Select(number => (char)Generator.Next(0, 1000000)).ToArray());

        public string GuaranteedFreshData { get; set; }

        [GlobalSetup]
        public void Initialize()
        {
            DataClashFilter = new ScalableBloomFilter<string>(0.02);

            Assert.IsNotNull(Execution.Everything);
            Assert.IsNotNull(BuiltWholeFeatureSetEvaluationTestProcessor);
        }

        [IterationSetup]
        public void GenerateUnseenData()
        {
            while (Data is { } data && !DataClashFilter.Contains(data))
            {
                GuaranteedFreshData = data;
                return;
            }

            Assert.IsNotNull(GuaranteedFreshData);
            Assert.AreEqual(GuaranteedFreshData.Length, Length);
        }

        [Benchmark(Baseline = true, Description = "Late Processor Build and Invocation")]
        public void BuildAndInvokeProcessor() => LateBuiltWholeFeatureSetEvaluationTestProcessor.Invoke(Data);

        [Benchmark(Description = "Pre-Built Processor Invocation")]
        public void InvokeBuiltProcessor() => BuiltWholeFeatureSetEvaluationTestProcessor.Invoke(Data);
    }
}
