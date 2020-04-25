using System;
using System.Collections.Generic;
using System.Linq;

namespace Stanza
{
    public static class Utilities
    {
        internal static int GetRelativeIndex(this Index index, int length) => index.IsFromEnd ? length - index.Value : index.Value;

        internal static ArgumentOutOfRangeException OutOfBoundsError => new ArgumentOutOfRangeException("index", "throw new ArgumentOutOfRangeException(nameof(index)", "Index was out of range. Must be non-negative and less than the size of the collection.");
     
        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt;
        /// consisting of a single item.
        /// </summary>
        /// <typeparam name="T"> Type of the object. </typeparam>
        /// <param name="item"> The instance that will be wrapped. </param>
        /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
        internal static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        /// <summary>
        /// Attaches and applies a <see cref="Evaluation{T}.Generator{TCustomParameter}"/> to a new sequential action item set with a given value for <paramref name="input"/>.
        /// </summary>
        /// <typeparam name="TValidation"></typeparam>
        /// <typeparam name="TCustomParameter"></typeparam>
        /// <param name="node"></param>
        /// <param name="generator"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Validation<TValidation> Attach<TValidation, TCustomParameter>(this Validation<TValidation> node, Generator<TValidation, TCustomParameter> generator, TCustomParameter input) => new Validation<TValidation>(true)
        {
            node,
            generator(input)
        };

        public static Validation<TValidation> Attach<TValidation, TCustomParameter>(this Validation<TValidation> node, Builder<TValidation, TCustomParameter> restrictor, TCustomParameter input) => new Validation<TValidation>(true)
        {
            node,
            restrictor(input)
        };

        // Chain

        public static Validation<TValidation> Combine<TValidation>(this Validation<TValidation> head, params Validation<TValidation>[] nodes)
        {
            Validation<TValidation> result = new Validation<TValidation>(true) { head };

            foreach (Validation<TValidation> analyzer in nodes)
            {
                result.Add(analyzer);
            }

            return result;
        }

        public static Validation<TValidation> Combine<TValidation>(this Validator<TValidation> head, params Validation<TValidation>[] nodes) => Combine((Validation<TValidation>)head, nodes);

        // Affix

        public static Validation<TValidation> Combine<TValidation>(this Validator<TValidation> head, bool reverse = true, params Validation<TValidation>[] nodes) => Combine((Validation<TValidation>)head, reverse, nodes);

        public static Validation<TValidation> Combine<TValidation>(this Validation<TValidation> head, bool reverse = true, params Validation<TValidation>[] nodes)
        {
            if (reverse)
            {
                Validation<TValidation> result = new Validation<TValidation>(true) { };

                foreach (Validation<TValidation> analyzer in nodes.Reverse())
                {
                    result.Add(analyzer);
                }

                result.Add(head);

                return result;
            }
            else return head.Combine(nodes);
        }

        public static IEnumerable<Evaluation> Execute<TValidation>(this Validation<TValidation> validation, TValidation value) => validation.BuildEvaluator().Invoke(value);

        public static Validation<TValidation> GetValidation<TValidation>(this Validation<TValidation> validation, TValidation value, string identifier = default, bool consecutive = false) => new Validation<TValidation>(consecutive).Fill((identifier is { } ? validation.Bind(value, identifier).Evaluations : validation.Execute(value)).Select(evaluation => (Validation<TValidation>)evaluation).ToArray());

        public static Bundle<TValidation> Bind<TValidation>(this Validation<TValidation> validation, TValidation data, string identifier) => new DirectBundle<TValidation> { Identifier = identifier, Data = data, Validation = validation };

        public static Bundle<TValidation> Bind<TValidation>(this Validation<TValidation> validation, Func<TValidation> fetcher, string identifier) => new LateBundle<TValidation> { Identifier = identifier, Fetcher = fetcher, Validation = validation };

        public static IEnumerable<Evaluation> GetFormattedEvaluations(this IEnumerable<Evaluation> evaluations, string identifier, bool allowIdentifierModification = true)
        {
            if (identifier is { })
            {
                foreach (Evaluation evaluation in evaluations)
                {
                    yield return (Evaluation: evaluation, Information: evaluation.Information?.IndexOf("{0}") is { } index && index > -1 ? evaluation.Information = String.Format(evaluation.Information, index > 0 && allowIdentifierModification ? $"{Char.ToLower(identifier[0])}{identifier[1..]}" : identifier) : evaluation.Information).Evaluation;
                }
            }
        }
    }
}
