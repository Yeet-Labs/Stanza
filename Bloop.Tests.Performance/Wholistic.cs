using BenchmarkDotNet.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

using static Bloop.Validation<string>;

namespace Bloop.Tests
{
    [RPlotExporter, SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.NetCoreApp31)]
    public class Wholistic
    {
        Processor LateBuiltWholeFeatureSetEvaluationTestProcessor => Evaluation.Everything.Value.BuildProcessor();

        Processor BuiltWholeFeatureSetEvaluationTestProcessor { get; } = Evaluation.Everything.Value.BuildProcessor();

        [Params(default, "", " ", "Peter", "Potato", "1234567890123", "123", "Toad", "Foot", "Frog", "Hand")]
        public string Data { get; set; }

        [GlobalSetup]
        public void Initialize()
        {
            Assert.IsNotNull(Evaluation.Everything.Value);
            Assert.IsNotNull(BuiltWholeFeatureSetEvaluationTestProcessor);
        }

        [Benchmark(Baseline = true, Description = "An operation which performs the whole feature set evaluation test validation set processor build and invoke at call time.")]
        public void BuildAndInvokeProcessor() => LateBuiltWholeFeatureSetEvaluationTestProcessor.Invoke(Data);
        
        [Benchmark(Description = "An operation which invokes a pre-built instance of the whole feature set evaluation test validation set processor with full caching enabled.")]
        public void InvokeBuiltProcessor() => BuiltWholeFeatureSetEvaluationTestProcessor.Invoke(Data);
    }
}
