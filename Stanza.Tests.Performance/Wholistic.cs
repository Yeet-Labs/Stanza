using BenchmarkDotNet.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stanza.Tests
{
    // TODO: Add chain processing tests to calcualte overhead.

    [RPlotExporter, SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.NetCoreApp31)]
    public class Wholistic
    {
        Evaluator<string> LateBuiltWholeFeatureSetEvaluationTestEvaluator => Execution.Everything.BuildEvaluator();

        Evaluator<string> BuiltWholeFeatureSetEvaluationTestEvaluator { get; } = Execution.Everything.BuildEvaluator();

        [Params(default, "", " ", "Peter", "Potato", "1234567890123", "123", "Toad", "Foot", "Frog", "Hand")]
        public string Data { get; set; }

        [GlobalSetup]
        public void Initialize()
        {
            Assert.IsNotNull(Execution.Everything);
            Assert.IsNotNull(BuiltWholeFeatureSetEvaluationTestEvaluator);
        }

        [Benchmark(Baseline = true, Description = "Late Evaluator Build and Invocation")]
        public void BuildAndInvokeProcessor() => LateBuiltWholeFeatureSetEvaluationTestEvaluator.Invoke(Data);
        
        [Benchmark(Description = "Pre-Built Evaluator Invocation")]
        public void InvokeBuiltProcessor() => BuiltWholeFeatureSetEvaluationTestEvaluator.Invoke(Data);
    }
}
