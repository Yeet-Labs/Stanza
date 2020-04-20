using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Bloop.Tests.Properties.Resources;
using static Bloop.Validation;
using static Bloop.Validation<string>;
using static Bloop.Validation<string>.Evaluation;

namespace Bloop.Tests
{
    [TestClass]
    public class Evaluation
    {
        public static Analyzer Essential { get; } = input => input switch
        {
            null => EssentialFailureA,
            _ => true
        };

        public static Lazy<Set> Basic { get; set; } = new Lazy<Set>(() => new Set(true)
        {
            Essential,
            input => input switch
            {
                "" => BasicFailureA,
                { Length: 1 } => BasicFailureB,
                _ => true
            }
        });

        public static Lazy<Set> Arbitrary { get; } = new Lazy<Set>(() => new Set(true)
        {
            Basic.Value,
            input => input switch
            {
                "Peter" => ArbitraryFailureA,
                "Potato" => ArbitraryFailureB,
                _ => true
            }
        });

        public static Lazy<Set> Example { get; } = new Lazy<Set>(() => new Validation<string>.Evaluation(true)
        {
            Arbitrary.Value,
            new Validation<string>.Evaluation(true).Fill(Arbitrary.Value.BuildProcessor().Invoke("Blah").Select(analysis => (Node)analysis).ToArray()),
            input => input switch
            {
                { Length: 13 } => ExampleFailureA,
                { Length: int length } when length >= 15 => $"{{0}} can't be {length} characters long!", // NOTE: Currently untestable.
                _ => true
            }
        });

        public static Lazy<Set> Auxiliary { get; } = new Lazy<Set>(() => new Validation<string>.Evaluation(true)
        {
            Arbitrary.Value,
            new Validation<string>.Evaluation(true).Fill(Arbitrary.Value.BuildProcessor().Invoke("Boof").Select(analysis => (Node)analysis).ToArray()),
            input => input switch
            {
                { Length: 3 } => AuxiliaryFailureA,
                { Length: int length } when length >= 16 => $"{{0}} can't be {length} characters long!", // NOTE: Currently untestable.
                _ => true
            }
        });

        public static Lazy<Set> Everything { get; } = new Lazy<Set>(() => new Set(true)
        {
            Example.Value,
            Auxiliary.Value
        });

        public static bool Validate(Analysis result) => result.Positive;

        [DataTestMethod, DataRow("Toad", "Foot", DisplayName = "Dataset 1"), DataRow("Frog", "Hand", DisplayName = "Dataset 2")]
        public void TestSetEvaluationWithValidData(string inputA, string inputB)
        {
            IEnumerable<Analysis> resultsA = Example.Value.BuildProcessor().Invoke(inputA), resultsB = Example.Value.BuildProcessor().Invoke(inputB);

            Assert.IsNotNull(resultsA);
            Assert.IsNotNull(resultsB);

            Assert.IsTrue(resultsA.All(Validate));
            Assert.IsTrue(resultsB.All(Validate));
        }

        [DataTestMethod, DataRow(default, nameof(EssentialFailureA), DisplayName = "Essential Failure Case 1"), DataRow("", nameof(BasicFailureA), DisplayName = "Basic Failure Case 1"), DataRow(" ", nameof(BasicFailureB), DisplayName = "Basic Failure Case 2"), DataRow("Peter", nameof(ArbitraryFailureA), DisplayName = "Arbitrary Failure Case 1"), DataRow("Potato", nameof(ArbitraryFailureB), DisplayName = "Arbitrary Failure Case 2"), DataRow("1234567890123", nameof(ExampleFailureA), DisplayName = "Example Failure Case 1"), DataRow("123", nameof(AuxiliaryFailureA), DisplayName = "Auxiliary Failure Case 1")]
        public void TestSetEvaluationWithInvalidData(string input, string resource)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, data) => Debug.WriteLine($"Test Execution Failure: {data.ExceptionObject}");

            IEnumerable<Analysis> results = Everything.Value.BuildProcessor().Invoke(input);

            Assert.IsNotNull(results);

            Assert.AreEqual(1, results.Count(FailedResultFilter.Invoke));

            Analysis result = results.FirstOrDefault(FailedResultFilter.Invoke);

            Assert.IsFalse(result.Positive);
            Assert.AreEqual(result.Message, ResourceManager.GetString(resource));
        }

        static Predicate<Analysis> FailedResultFilter { get; } = result => result?.Positive == false;
    }
}
