using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Stanza
{
    using static Utilities;

    public delegate IEnumerable<Evaluation> Evaluator<T>(T value);

    public delegate Validation<T> Validator<T>(T value);

    public delegate Validator<T> Generator<T, TParameter>(TParameter customization);

    public delegate Validation<T> Builder<T, TParameter>(TParameter customization);

    public class Validation<T> : ICollection<Validation<T>>
    {
        Lazy<List<Validation<T>>> Nodes { get; } = new Lazy<List<Validation<T>>> { };

        public Validation(bool consecutive) => Consecutive = consecutive;

        public Validation() { }

        public Validation<T> this[int index]
        {
            get => Nodes.IsValueCreated ? Nodes.Value[index] : throw OutOfBoundsError;
            set
            {
                if (!Nodes.IsValueCreated)
                {
                    throw OutOfBoundsError;
                }

                Nodes.Value[index] = value;
            }
        }

        public bool Consecutive { get; private set; }

        public int Count => Behaviour switch
        {
            Behaviour.Descend when Nodes.IsValueCreated => Nodes.Value.Count,
            Behaviour.Descend => 0,
            _ => 1
        };

        public bool IsReadOnly => false;

        public Behaviour Behaviour { get; private set; }

        public Validator<T> Validator { get; private set; }

        public Evaluation Evaluation { get; private set; }

        public void Add(Validation<T> item) => Nodes.Value.Add(item);

        public void Add(Validator<T> evaluator) => Nodes.Value.Add(evaluator);

        public void Clear()
        {
            if (Nodes.IsValueCreated)
            {
                Nodes.Value.Clear();
            }
        }

        public bool Contains(Validation<T> item) => Nodes.IsValueCreated && Nodes.Value.Contains(item);

        public void CopyTo(Validation<T>[] array, int arrayIndex) => Nodes.Value.CopyTo(array, arrayIndex);

        public IEnumerator<Validation<T>> GetEnumerator() => Behaviour switch
        {
            Behaviour.Validate => this.Yield().GetEnumerator(),
            Behaviour.None => this.Yield().GetEnumerator(),
            Behaviour.Descend when Nodes.IsValueCreated => Nodes.Value.GetEnumerator(),
            _ => Enumerable.Empty<Validation<T>>().GetEnumerator()
        };

        public int IndexOf(Validation<T> item) => Nodes.IsValueCreated ? Nodes.Value.IndexOf(item) : -1;

        public void Insert(int index, Validation<T> item) => Nodes.Value.Insert(index, item);

        public bool Remove(Validation<T> item) => Nodes.IsValueCreated ? Nodes.Value.Remove(item) : false;

        public void RemoveAt(int index)
        {
            if (!Nodes.IsValueCreated)
            {
                throw OutOfBoundsError;
            }

            Nodes.Value.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static implicit operator Validation<T>(Validator<T> validator) => new Validation<T> { Validator = validator, Behaviour = Behaviour.Validate };

        public static implicit operator Validation<T>(Evaluation evaluation) => new Validation<T> { Evaluation = evaluation, Behaviour = Behaviour.None };

        public static implicit operator Validation<T>((string Information, Result Result) data) => new Evaluation { Information = data.Information, Result = data.Result };

        public static implicit operator Validation<T>((string Information, bool Success) data) => new Evaluation { Information = data.Information, Result = (Result)Convert.ToInt32(data.Success) };

        public static implicit operator Validation<T>(string information) => new Evaluation { Information = information, Result = Result.Failure };

        public static implicit operator Validation<T>(Result result) => new Evaluation { Result = result };

        public static implicit operator Validation<T>(bool success) => new Evaluation { Result = (Result)Convert.ToInt32(success) };

        public Validation<T> Fill(params Validation<T>[] nodes)
        {
            foreach (Validation<T> node in nodes)
            {
                Add(node);
            }

            return this;
        }

        public HashSet<Validation<T>> GetEvaluatorBuildCandidateNodes(Dictionary<Validation<T>, HashSet<Validation<T>>> lookup = default)
        {
            HashSet<Validation<T>> result = new HashSet<Validation<T>> { };
            lookup ??= new Dictionary<Validation<T>, HashSet<Validation<T>>> { };

            foreach (Validation<T> node in this)
            {
                _ = node switch
                {
                    null => throw new InvalidOperationException("Encountered null node in creation of built validation node set."),
                    Validation<T> { Behaviour: Behaviour.Descend, Nodes: Validation<T> { } set } when (lookup.TryGetValue(set, out HashSet<Validation<T>> optimizedSet) ? optimizedSet : lookup[set] = optimizedSet = set.GetEvaluatorBuildCandidateNodes(lookup)) is { } => (Consecutive, set.Consecutive) switch
                    {
                        (true, false) => result.Add(new Validation<T>(false).Fill(optimizedSet.Except(result).Where(GetPreviousNodeOccuranceFailStateContinuation).ToArray())) as object,
                        (false, true) => result.Add(new Validation<T>(true).Fill(optimizedSet.ToArray())),
                        _ => result = result.Concat(optimizedSet).ToHashSet()
                    },
                    _ => result.Add(node)
                };
            }

            return result;

            // Gets a value for whether or not a fail state in a subset addition candidate that appears elsewhere in the parent would cause an overall execution fail state to be reached before the candidate node would be executed. This value is used to determine whether or not a given node needs to be added to a subset, due to the fact that if the node occurance in the parent creates a fail state and causes an overall fail state in execution, the diagnostic information from that node's failure would surface and execution would halt before the candidate node would be executed, meaning it does not need to be added again. 

            bool GetPreviousNodeOccuranceFailStateContinuation(Validation<T> candidate)
            {
                return Process(result);

                bool Process(IEnumerable<Validation<T>> nodes)
                {
                    if (Consecutive)
                    {
                        if (result.Contains(candidate))
                        {
                            return false;
                        }

                        foreach (Validation<T> reference in nodes.Where(node => node is { Nodes: { } }).SelectMany(node => node.Nodes as IEnumerable<Validation<T>>))
                        {
                            return Process(reference);
                        }
                    }

                    return true;
                }
            }
        }

        public struct ValidationCacheValue
        {
            public T Data { get; set; }

            public override int GetHashCode() => Data is { } ? Data.GetHashCode() : 0;

            public static implicit operator ValidationCacheValue(T data) => new ValidationCacheValue { Data = data };
        }

        public Evaluator<T> BuildEvaluator(bool cacheRunResults = true, bool cacheNodeEvaluationResults = true, bool optimizeGeneratedNodes = false)
        {
            // TODO: Investigate caching the processing result of set nodes.
            // TOOD: Implement options in method signature.

            HashSet<Validation<T>> optimizedSet = Behaviour == Behaviour.Descend ? GetEvaluatorBuildCandidateNodes() : new HashSet<Validation<T>> { this };
            Dictionary<ValidationCacheValue, Dictionary<Func<T, Validation<T>>, IEnumerable<Evaluation>>> cache = new Dictionary<ValidationCacheValue, Dictionary<Func<T, Validation<T>>, IEnumerable<Evaluation>>> { };

            return value =>
            {
                if (!cache.TryGetValue(value, out Dictionary<Func<T, Validation<T>>, IEnumerable<Evaluation>> specificCache))
                {
                    cache[value] = specificCache = new Dictionary<Func<T, Validation<T>>, IEnumerable<Evaluation>> { };
                }

                IEnumerable<Evaluation> Process(IEnumerable<Validation<T>> nodes, bool sequential)
                {
                    foreach (Validation<T> node in nodes)
                    {
                        IEnumerable<Evaluation> validations = node switch
                        {
                            { Behaviour: Behaviour.None, Evaluation: { } validation } => validation.Yield(),
                            { Behaviour: Behaviour.Validate, Validator: { } evaluator } => GetResults(evaluator.Invoke),
                            { Behaviour: Behaviour.Descend, Nodes: { } } set => Process(set, set.Consecutive),
                            _ => throw new InvalidOperationException($"Encountered null node in built validation set node processor with input value of {value}.")
                        };

                        foreach (Evaluation validation in validations)
                        {
                            yield return validation;
                        }

                        if (sequential && validations.Any(validation => validation.Result != Result.Success))
                        {
                            yield break;
                        }
                    }

                    IEnumerable<Evaluation> GetResults(Func<T, Validation<T>> evaluator)
                    {
                        if (!specificCache.TryGetValue(evaluator, out IEnumerable<Evaluation> validations) && evaluator(value) is { } evaluation)
                        {
                            specificCache[evaluator] = validations = Process(evaluation, evaluation.Consecutive);
                        }

                        return validations;
                    }
                }

                return Process(optimizedSet, Consecutive).ToHashSet();
            };
        }
    }
}
