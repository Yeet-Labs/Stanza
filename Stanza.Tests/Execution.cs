using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Stanza.Tests.Properties.Resources;

namespace Stanza.Tests
{
    [TestClass]
    public class Execution
    {
        public static Validator<string> Essential { get; } = input => input switch
        {
            null => EssentialFailureA,
            _ => true
        };

        public static Validation<string> Basic { get; set; } = new Validation<string>(true)
        {
            Essential,
            input => input switch
            {
                "" => BasicFailureA,
                { Length: 1 } => BasicFailureB,
                _ => true
            }
        };

        public static Validation<string> Arbitrary { get; } = new Validation<string>(true)
        {
            Basic,
            input => input switch
            {
                "Peter" => ArbitraryFailureA,
                "Potato" => ArbitraryFailureB,
                _ => true
            }
        };

        public static Validation<string> Example { get; } = new Validation<string>(true)
        {
            Arbitrary,
            Arbitrary.GetValidation("Blah"),
            input => input switch
            {
                { Length: 13 } => ExampleFailureA,
                { Length: int length } when length >= 15 => $"{{0}} can't be {length} characters long!", // NOTE: Currently untestable.
                _ => true
            }
        };

        public static Validation<string> Auxiliary { get; } = new Validation<string>(true)
        {
            Arbitrary,
            Arbitrary.GetValidation("Boof"),
            input => input switch
            {
                { Length: 3 } => AuxiliaryFailureA,
                { Length: int length } when length >= 16 => $"{{0}} can't be {length} characters long!", // NOTE: Currently untestable.
                _ => true
            }
        };

        public static Validation<string> Everything { get; } = new Validation<string>(true)
        {
            Example,
            Auxiliary
        };

        public static bool Validate(Evaluation evaluation) => evaluation.Result == Result.Success;

        [DataTestMethod, DataRow("Toad", "Foot", DisplayName = "Dataset 1"), DataRow("Frog", "Hand", DisplayName = "Dataset 2")]
        public void TestValidationExecutionWithValidData(string inputA, string inputB)
        {
            IEnumerable<Evaluation> resultsA = Example.Execute(inputA), resultsB = Auxiliary.Execute(inputB);

            Assert.IsNotNull(resultsA);
            Assert.IsNotNull(resultsB);

            Assert.IsTrue(resultsA.All(Validate));
            Assert.IsTrue(resultsB.All(Validate));
        }

        [DataTestMethod, DataRow(default, nameof(EssentialFailureA), DisplayName = "Essential Failure Case 1"), DataRow("", nameof(BasicFailureA), DisplayName = "Basic Failure Case 1"), DataRow(" ", nameof(BasicFailureB), DisplayName = "Basic Failure Case 2"), DataRow("Peter", nameof(ArbitraryFailureA), DisplayName = "Arbitrary Failure Case 1"), DataRow("Potato", nameof(ArbitraryFailureB), DisplayName = "Arbitrary Failure Case 2"), DataRow("1234567890123", nameof(ExampleFailureA), DisplayName = "Example Failure Case 1"), DataRow("123", nameof(AuxiliaryFailureA), DisplayName = "Auxiliary Failure Case 1")]
        public void TestValidationExecutionWithInvalidData(string input, string resource)
        {
            IEnumerable<Evaluation> results = Everything.Execute(input);

            Assert.IsNotNull(results);

            Assert.AreEqual(1, results.Count(FailedResultFilter.Invoke));

            Evaluation evaluation = results.FirstOrDefault(FailedResultFilter.Invoke);

            Assert.AreEqual(Result.Failure, evaluation.Result);
            Assert.AreEqual(ResourceManager.GetString(resource), evaluation.Information);
        }

        static Predicate<Evaluation> FailedResultFilter { get; } = evaluation => evaluation is { Result: Result.Failure };

        static Chain ValidDataExecutionTestChain { get; } = new Chain
        {
            (Example, "A"),
            (Auxiliary, "B")
        };

        [DataTestMethod, DataRow("Toad", "Foot", DisplayName = "Dataset 1"), DataRow("Frog", "Hand", DisplayName = "Dataset 2")]
        public void TestValidationChainExecutionWithValidData(string inputA, string inputB)
        {
            // Chain.Create(Example.Bind(inputA, "a"), Auxiliary.Bind(inputB, "b"))

            Chain.Execution execution = ValidDataExecutionTestChain.BindLoose(inputA, inputB).Process();

            Assert.IsNotNull(execution);

            Assert.IsFalse(execution.Failed);

            Chain.Execution.Result resultA = execution["A"], resultB = execution["A"];

            Assert.IsNotNull(resultA);
            Assert.IsNotNull(resultB);

            Assert.AreEqual(0, resultA.Failures.Count());
            Assert.AreEqual(0, resultB.Failures.Count());
        }

        static Chain InvalidDataExecutionTestChain { get; } = new Chain
        {
            (Everything, "A"),
            (Arbitrary, "Reference", "Boof")
        };

        [DataTestMethod, DataRow(default, nameof(EssentialFailureA), DisplayName = "Essential Failure Case 1"), DataRow("", nameof(BasicFailureA), DisplayName = "Basic Failure Case 1"), DataRow(" ", nameof(BasicFailureB), DisplayName = "Basic Failure Case 2"), DataRow("Peter", nameof(ArbitraryFailureA), DisplayName = "Arbitrary Failure Case 1"), DataRow("Potato", nameof(ArbitraryFailureB), DisplayName = "Arbitrary Failure Case 2"), DataRow("1234567890123", nameof(ExampleFailureA), DisplayName = "Example Failure Case 1"), DataRow("123", nameof(AuxiliaryFailureA), DisplayName = "Auxiliary Failure Case 1")]
        public void TestValidationChainExecutionWithInvalidData(string input, string resource)
        {
            // Chain.Create(Everything.Bind(input, "input"), Arbitrary.Bind("boof", "reference"))

            Chain.Execution execution = InvalidDataExecutionTestChain.BindLoose(input).Process();

            Assert.IsNotNull(execution);

            Assert.IsTrue(execution.Failed);

            Chain.Execution.Result resultA = execution["A"], resultB = execution["Reference"];

            Assert.IsNotNull(resultA);
            Assert.IsNotNull(resultB);

            Assert.AreEqual(1, resultA.Failures.Count());
            Assert.AreEqual(0, resultB.Failures.Count());

            string unformattedInformation = ResourceManager.GetString(resource);

            Assert.IsNotNull(unformattedInformation);

            Assert.AreEqual(unformattedInformation.Contains("{0}") ? String.Format(unformattedInformation, "A") : unformattedInformation, resultA.Failures.First().Information);
        }
    }
}
