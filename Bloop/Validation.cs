using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bloop
{
    using static Validation;

    public static class Validation
    {
        public class Analysis
        {
            public bool Positive { get; set; }

            // public T Value { get; set; }

            public string Message { get; set; }

            public override int GetHashCode() => HashCode.Combine(Positive, Message);
        }
    }

    public static class Validation<T>
    {
        /// <summary>
        /// A value analyzer that takes an input and emits diagnostic information or further action items to process.
        /// </summary>
        /// <param name="input">The input value.</param>
        /// <returns>An evaluation of the input data.</returns>
        public delegate Evaluation Analyzer(T input);

        /// <summary>
        /// A delegate that returns a delegate which generates a set of action items specific to given values of <typeparamref name="TInput"/> and/or <typeparamref name="T"/>. This is like a restrictor but the <see cref="Set"/> returned from the second 
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public delegate Restrictor<T> Generator<TInput>(TInput input);

        /// <summary>
        /// A delegate that takes a typed restriction, and returns a set of action items to be processed for a <typeparamref name="T"/> value.
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public delegate Set Restrictor<TInput>(TInput input);

        // TODO: Consider renaming this to Analyzer as an overload for Analyzer(T input).

        public delegate Set Queuer();

        // TODO: Remove one of the identical delegates.

        public delegate IEnumerable<Analysis> Collector(T input);

        public delegate IEnumerable<Analysis> Processor(T input);

        // TODO: Investigate if implementing ICollection of the implicit conversions to Set.Node is necessary.

        public class Set : ICollection<Set.Node>
        {
            public class Node
            {
                public enum Branch
                {
                    Evaluate = 0,
                    Descend,
                    Generate
                }

                public Branch Behaviour { get; set; } = Branch.Evaluate;

                public Analyzer Evaluator { get; set; }

                /// <summary>
                /// An analyzer that returns a set of validation items (actionable data).
                /// </summary>
                public Restrictor<T> Restrictor { get; set; }

                public Set Nodes { get; set; }

                public static implicit operator Node(Analyzer evaluator) => new Node { Evaluator = evaluator, Behaviour = Branch.Evaluate };

                public static implicit operator Node(Restrictor<T> restrictor) => new Node { Restrictor = restrictor, Behaviour = Branch.Generate };

                public static implicit operator Node(Set queue) => new Node { Nodes = queue, Behaviour = Branch.Descend };

                public override int GetHashCode() => Behaviour switch
                {
                    Branch.Evaluate => Evaluator.Method.GetHashCode(),
                    Branch.Generate => Restrictor.Method.GetHashCode(),
                    _ => Nodes.GetHashCode()
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => Nodes.Value.GetEnumerator();

            public bool IsReadOnly { get; } = false;

            public int Count => Nodes.IsValueCreated ? Nodes.Value.Count : 0;

            public void Clear()
            {
                if (Nodes.IsValueCreated)
                {
                    Nodes.Value.Clear();
                }
            }

            public IEnumerator<Node> GetEnumerator() => Nodes.IsValueCreated ? Nodes.Value.GetEnumerator() : Enumerable.Empty<Node>().GetEnumerator();

            protected static Predicate<Node> DescendNodeBranchPredicate { get; } = GenerateNodeBranchPredicate(Node.Branch.Descend);

            protected static Predicate<Node> EvaluateNodeBranchPredicate { get; } = GenerateNodeBranchPredicate(Node.Branch.Evaluate);

            protected static Predicate<Node> GenerateNodeBranchPredicate(Node.Branch branch) => node => node is { Behaviour: var behaviour } && behaviour == branch;

            protected Lazy<List<Node>> Nodes { get; } = new Lazy<List<Node>> { };

            public void Add(Node item) => Nodes.Value.Add(item);

            public void Add(Analyzer item) => Nodes.Value.Add(item);
            
            public void Add(Set item) => Nodes.Value.Add(item);

            public bool Contains(Node item) => Nodes.IsValueCreated && Nodes.Value.Contains(item);

            public bool Contains(Analyzer item) => Nodes.IsValueCreated && Nodes.Value.Any(node => item == node.Evaluator);
            
            public bool Contains(Set item) => Nodes.IsValueCreated && Nodes.Value.Any(node => item == node.Nodes);

            public void CopyTo(Node[] array, int arrayIndex) => Nodes.Value.CopyTo(array, arrayIndex);

            public void CopyTo(Analyzer[] array, int arrayIndex) => Nodes.Value.Where(node => node is { Behaviour: Node.Branch.Evaluate }).Select(node => node.Evaluator).ToArray().CopyTo(array, arrayIndex);
            
            public void CopyTo(Set[] array, int arrayIndex) => Nodes.Value.Where(node => node is { Behaviour: Node.Branch.Descend }).Select(node => node.Nodes).ToArray().CopyTo(array, arrayIndex);

            public bool Remove(Node item) => Nodes.Value.Remove(item);

            public bool Remove(Analyzer item) => Remove(Nodes.Value.Find(node => node.Evaluator == item));
            
            public bool Remove(Set item) => Remove(Nodes.Value.Find(node => node.Nodes == item));

            public Set Fill(params Node[] nodes)
            {
                foreach (Node node in nodes)
                {
                    Add(node);
                }

                return this;
            }

            public static implicit operator Set(Analyzer analyzer) => new Set { analyzer };

            public bool Sequential { get; set; }

            public override int GetHashCode() => Nodes.IsValueCreated ? 0 : (Nodes as IEnumerable<Node>).Aggregate(17, (hash, node) => hash * 31 + node.GetHashCode());

            public Set() { }

            public Set(bool sequential) => Sequential = sequential;
        }

        public class Evaluation : Set
        {
            public new class Node : Set.Node
            {
                public bool Stale { get; set; }

                public Analysis Analysis { get; set; }

                public override int GetHashCode() => Stale ? Analysis.GetHashCode() : base.GetHashCode();

                public static implicit operator Node(Analysis result) => new Node { Analysis = result, Stale = true };

                public static implicit operator Node(bool result) => new Node { Analysis = new Analysis { Positive = result }, Stale = true };

                public static implicit operator Node(string message) => new Node { Analysis = new Analysis { Message = message, Positive = false }, Stale = true };
            }

            public void Add(Node item) => Nodes.Value.Add(item);

            public bool Remove(Node item) => Nodes.Value.Remove(item);

            public bool Contains(Node item) => Nodes.Value.Contains(item);

            public void CopyTo(Node[] array, int arrayIndex) => Nodes.Value.OfType<Node>().ToArray().CopyTo(array, arrayIndex);

            public static implicit operator Evaluation(bool result) => new Evaluation { result };

            public static implicit operator Evaluation(string message) => new Evaluation { message };

            public Evaluation() { }

            public Evaluation(bool sequential) => Sequential = sequential;
        }
    }

    public static class Toaster
    {
        public static int GetRelativeIndex(this Index index, int length) => index.IsFromEnd ? length - index.Value : index.Value;

        /// <summary>
        /// Attaches and applies a <see cref="Validation{T}.Generator{TInput}"/> to a new sequential action item set with a given value for <paramref name="input"/>.
        /// </summary>
        /// <typeparam name="TValidation"></typeparam>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="node"></param>
        /// <param name="generator"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Validation<TValidation>.Set Attach<TValidation, TInput>(this Validation<TValidation>.Set.Node node, Validation<TValidation>.Generator<TInput> generator, TInput input) => new Validation<TValidation>.Set(true)
        {
            node,
            generator(input)
        };

        public static Validation<TValidation>.Set Restrict<TValidation, TInput>(this Validation<TValidation>.Set.Node node, Validation<TValidation>.Restrictor<TInput> restrictor, TInput input) => new Validation<TValidation>.Set(true)
        {
            node,
            restrictor(input)
        };

        public static Validation<TValidation>.Set Affix<TValidation, TInput>(this Validation<TValidation>.Set.Node head, bool reverse = true, params Validation<TValidation>.Set.Node[] nodes)
        {
            if (reverse)
            {
                Validation<TValidation>.Set result = new Validation<TValidation>.Set(true) { };

                foreach (Validation<TValidation>.Set.Node analyzer in nodes.Reverse())
                {
                    result.Add(analyzer);
                }

                result.Add(head);

                return result;
            }
            else return head.Chain(nodes);
        }

        public static Validation<TValidation>.Set Chain<TValidation>(this Validation<TValidation>.Analyzer head, params Validation<TValidation>.Set.Node[] nodes) => ((Validation<TValidation>.Set.Node)head).Chain(nodes);

        public static Validation<TValidation>.Set Chain<TValidation>(this Validation<TValidation>.Set head, params Validation<TValidation>.Set.Node[] nodes) => ((Validation<TValidation>.Set.Node)head).Chain(nodes);

        public static Validation<TValidation>.Set Chain<TValidation>(this Validation<TValidation>.Set.Node head, params Validation<TValidation>.Set.Node[] nodes)
        {
            Validation<TValidation>.Set result = new Validation<TValidation>.Set(true) { head };

            foreach (Validation<TValidation>.Set.Node analyzer in nodes)
            {
                result.Add(analyzer);
            }

            return result;
        }
        
        public static HashSet<Validation<TValidation>.Set.Node> GetCandidateNodes<TValidation>(this Validation<TValidation>.Set target, Dictionary<Validation<TValidation>.Set, HashSet<Validation<TValidation>.Set.Node>> lookup = default)
        {
            HashSet<Validation<TValidation>.Set.Node> result = new HashSet<Validation<TValidation>.Set.Node> { };
            lookup ??= new Dictionary<Validation<TValidation>.Set, HashSet<Validation<TValidation>.Set.Node>> { };

            // foreach (Validation<TValidation>.Set.Node node in target)
            // {
            //     switch (node)
            //     {
            //         case null: throw new InvalidOperationException { };
            //         case { Behaviour: Validation<TValidation>.Set.Node.Branch.Descend, Nodes: { } set } when (lookup.TryGetValue(set, out HashSet<Validation<TValidation>.Set.Node> optimizedSet) ? optimizedSet : lookup[set] = optimizedSet = set.GetCandidateNodes(lookup)) is { }:
            //             switch (target.Sequential, set.Sequential)
            //             {
            //                 case (true, false):
            //                     result.Add(new Validation<TValidation>.Set(false).Fill(optimizedSet.Except(result).Where(GetPreviousNodeOccuranceFailStateContinuation).ToArray()));
            //                     break;
            //                 case (false, true):
            //                     result.Add(new Validation<TValidation>.Set(true).Fill(optimizedSet.ToArray()));
            //                     break;
            //                 default:
            //                     result = result.Concat(optimizedSet).ToHashSet();
            //                     break;
            //             }
            //             break;
            //         default: 
            //             result.Add(node);
            //             break;
            //     }
            // }

            target.Select(node => node switch
            {
                null => throw new InvalidOperationException("Encountered null node in creation of built validation node set."),
                Validation<TValidation>.Set.Node { Behaviour: Validation<TValidation>.Set.Node.Branch.Descend, Nodes: Validation<TValidation>.Set { } set } when (lookup.TryGetValue(set, out HashSet<Validation<TValidation>.Set.Node> optimizedSet) ? optimizedSet : lookup[set] = optimizedSet = set.GetCandidateNodes(lookup)) is { } => (target.Sequential, set.Sequential) switch
                {
                    (true, false) => result.Add(new Validation<TValidation>.Set(false).Fill(optimizedSet.Except(result).Where(GetPreviousNodeOccuranceFailStateContinuation).ToArray())),
                    (false, true) => result.Add(new Validation<TValidation>.Set(true).Fill(optimizedSet.ToArray())),
                    _ => (result = result.Concat(optimizedSet).ToHashSet()) as object
                },
                _ => result.Add(node)
            }).ToList();

            return result;

            // Gets a value for whether or not a fail state in a subset addition candidate that appears elsewhere in the parent would cause an overall execution fail state to be reached before the candidate node would be executed. This value is used to determine whether or not a given node needs to be added to a subset, due to the fact that if the node occurance in the parent creates a fail state and causes an overall fail state in execution, the diagnostic information from that node's failure would surface and execution would halt before the candidate node would be executed, meaning it does not need to be added again. 

            bool GetPreviousNodeOccuranceFailStateContinuation(Validation<TValidation>.Set.Node candidate)
            {
                return Process(result);

                bool Process(IEnumerable<Validation<TValidation>.Set.Node> nodes)
                {
                    if (target.Sequential)
                    {
                        if (result.Contains(candidate))
                        {
                            return false;
                        }

                        foreach (Validation<TValidation>.Set reference in nodes.Where(node => node is { Nodes: { } }).SelectMany(node => node.Nodes as IEnumerable<Validation<TValidation>.Set>))
                        {
                            return Process(reference);
                        }
                    }

                    return true;
                }
            }
        }

        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt;
        /// consisting of a single item.
        /// </summary>
        /// <typeparam name="T"> Type of the object. </typeparam>
        /// <param name="item"> The instance that will be wrapped. </param>
        /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
        static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        public struct ValidationCacheValue<TValidation>
        {
            public TValidation Data { get; set; }

            public override int GetHashCode() => Data is { } ? Data.GetHashCode() : 0;
        }

        public static ValidationCacheValue<TValidation> GetCacheValue<TValidation>(this TValidation data) => new ValidationCacheValue<TValidation> { Data = data };

        public static Validation<TValidation>.Processor BuildProcessor<TValidation>(this Validation<TValidation>.Set target, bool cacheRunResults = true, bool cacheNodeEvaluationResults = true, bool optimizeGeneratedNodes = false)
        {
            // TODO: Investigate caching the processing result of set nodes.
            // TOOD: Implement options in method signature.

            HashSet<Validation<TValidation>.Set.Node> optimizedSet = target.GetCandidateNodes();
            Dictionary<ValidationCacheValue<TValidation>, Dictionary<Func<TValidation, Validation<TValidation>.Set>, IEnumerable<Analysis>>> cache = new Dictionary<ValidationCacheValue<TValidation>, Dictionary<Func<TValidation, Validation<TValidation>.Set>, IEnumerable<Analysis>>> { };

            return value =>
            {
                if (!cache.TryGetValue(value.GetCacheValue(), out Dictionary<Func<TValidation, Validation<TValidation>.Set>, IEnumerable<Analysis>> specificCache))
                {
                    cache[value.GetCacheValue()] = specificCache = new Dictionary<Func<TValidation, Validation<TValidation>.Set>, IEnumerable<Analysis>> { };
                }

                IEnumerable<Analysis> Process(IEnumerable<Validation<TValidation>.Set.Node> nodes, bool sequential)
                {
                    foreach (Validation<TValidation>.Set.Node node in nodes)
                    {
                        IEnumerable<Analysis> results = node switch
                        {
                            Validation<TValidation>.Evaluation.Node { Stale: true, Analysis: { } analysis } => analysis.Yield(),
                            { Behaviour: Validation<TValidation>.Set.Node.Branch.Evaluate, Evaluator: { } evaluator } => GetResults(evaluator.Invoke),
                            { Behaviour: Validation<TValidation>.Set.Node.Branch.Generate, Restrictor: { } restrictor } => GetResults(restrictor.Invoke),
                            { Behaviour: Validation<TValidation>.Set.Node.Branch.Descend, Nodes: { } set } => Process(set, set.Sequential),
                            _ => throw new InvalidOperationException($"Encountered null node in built validation set node processor with input value of {value}.")
                        };

                        foreach (Analysis result in results)
                        {
                            yield return result;
                        }

                        if (sequential && results.Any(result => !result.Positive))
                        {
                            yield break;
                        }
                    }

                    IEnumerable<Analysis> GetResults(Func<TValidation, Validation<TValidation>.Set> evaluator)
                    {
                        if (!specificCache.TryGetValue(evaluator, out IEnumerable<Analysis> analyses) && evaluator(value) is { } evaluation)
                        {
                            specificCache[evaluator] = analyses = Process(evaluation, evaluation.Sequential);
                        }

                        return analyses;
                    }
                }

                // return new Validation<TValidation>.Evaluation { }.Fill(Process(optimizedSet, target.Sequential).ToHashSet().Cast<Validation<TValidation>.Set.Node>().ToArray()) as Validation<TValidation>.Evaluation;

                return Process(optimizedSet, target.Sequential).ToHashSet();
            };
        }
    }

    public static class Test
    {
        // Implement Analyzer.Restrict(Generator, Parameter Value) and Analyzer.Chain(Analyzer...)

        public static Validation<string>.Evaluation Donkey { get; } = new Validation<string>.Evaluation 
        { 
            
        };

        public static Validation<string>.Analyzer Potato { get; } = chicken => "";

        public static Validation<string>.Analyzer Tomato { get; } = chicken => new Validation<string>.Evaluation 
        { 
            tomato => "", 
            "" 
        };

        // public static Validation<string>.Analyzer Bruh { get; } = value => value => "";

        public static Validation<string>.Analyzer Hydrated { get; } = value => value switch
        {
            null => "{0} is required.",
            "" => "{0} cannot be empty.",
            { } when String.IsNullOrWhiteSpace(value) => "{0} cannot be blank.",
            _ => true
        };

        public static Validation<string>.Restrictor<(int Target, bool Minimum)> Length { get; } = length => new Validation<string>.Set(true)
        {
            Hydrated,
            value => (length.Minimum, value!) switch
            {
                (true, { }) when value.Length < length.Target => $"{{0}} is only {value.Length} characters long, but must be at least {length.Target}.",
                (false, { }) when value.Length > length.Target => $"{{0}} is {value.Length} characters long, must be less than or equal to {length.Target}.",
                _ => true
            }
        };

        public static Validation<string>.Generator<Range> Range { get; } = range => value => new Validation<string>.Set(true)
        {
            Hydrated,
            Length((range.Start.GetRelativeIndex(value.Length), Minimum: true)),
            Length((range.End.GetRelativeIndex(value.Length), false))
        };

        // TODO: Look into making it standard for these kinds of constructs to be Lazy<Validation<string>.Set>.

        public static Lazy<Validation<string>.Set> Wobble { get; } = new Lazy<Validation<string>.Set>(() => new Validation<string>.Set
        {
            new Validation<string>.Set(true)
            {
                Hydrated,
                value => value! switch
                {
                    { } when value.Length % 50 != 0 => "The length of {0} must be a multiple of 50.",
                    _  => true
                }
            },
            Range(4..100)
        });
    }
}
