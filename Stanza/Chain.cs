using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Stanza.Definitions.Provided;

namespace Stanza
{
    public class Chain : List<IBundle>
    {
        public void Add<T>((Validation<T> Validation, string Identifier) unboundBundle) => Add(new DirectBundle<T> { Validation = unboundBundle.Validation, Identifier = unboundBundle.Identifier, Loose = true });

        public void Add<T>((Validator<T> Validator, string Identifier) unboundBundle) => Add(new DirectBundle<T> { Validation = unboundBundle.Validator, Identifier = unboundBundle.Identifier, Loose = true });
        
        public void Add<T>((Validation<T> Validation, string Identifier, object Data) bundle) => Add(new DirectBundle<T> { Validation = bundle.Validation, Identifier = bundle.Identifier, Data = (T)bundle.Data });

        public void Add<T>((Validator<T> Validator, string Identifier, object Data) bundle) => Add(new DirectBundle<T> { Validation = bundle.Validator, Identifier = bundle.Identifier, Data = (T)bundle.Data });

        /// <summary>
        /// Binds <see cref="IBundle"/> implementation instances which also implement <see cref="IMutableBundle"/>.
        /// </summary>
        /// <param name="data">The data parameters in the order of the bundles they are to be bound to.</param>
        /// <returns>A chain instance with the modified bundles.</returns>
        public Chain BindLoose(params object[] data)
        {
            IEnumerable<IMutableBundle> mutableBundles = this.OfType<IMutableBundle>();
            for (int index = 0; index < data.Length; index++)
            {
                mutableBundles.ElementAt(index).Data = data[index];
            }

            return this;
        }

        public class Execution : Dictionary<string, Execution.Result>, IEnumerable<Evaluation>, IEnumerable<Execution.Result>
        {
            public class Result : List<Evaluation>
            {
                public Result(IEnumerable<Evaluation> validations, string identifier)
                {
                    foreach (Evaluation validation in validations)
                    {
                        Add(validation);
                    }
                }

                public IEnumerable<Evaluation> Failures => this.Where(validation => validation.Result == Stanza.Result.Failure);

                public IEnumerable<Evaluation> Successes => this.Where(validation => validation.Result == Stanza.Result.Success);

                public IEnumerable<Evaluation> Miscellaneous => this.Where(validation => validation.Result == Stanza.Result.Other);
            }

            public bool Failed { get; set; }

            public bool Halted { get; set; }

            IEnumerator<Evaluation> IEnumerable<Evaluation>.GetEnumerator() => Values.SelectMany(result => result).GetEnumerator();

            IEnumerator<Result> IEnumerable<Result>.GetEnumerator() => Values.GetEnumerator();
        }

        public static Chain Create(params IBundle[] bundles) => new Chain { }.Fill(bundles);

        public Chain Fill(params IBundle[] bundles)
        {
            foreach (IBundle bundle in bundles)
            {
                Add(bundle);
            }

            return this;
        }

        public Execution Process(bool haltOnFailure = false)
        {
            Execution execution = new Execution { };
            Execution.Result validations = default;

            foreach (IBundle bundle in this)
            {
                execution[bundle.Identifier] = validations = new Execution.Result(bundle.Evaluations, bundle.Identifier);

                if (!execution.Failed && validations.Any(validation => validation.Result == Result.Failure))
                {
                    execution.Failed = true;

                    if (haltOnFailure)
                    {
                        execution.Halted = true;
                        break;
                    }
                }
            }

            return execution;
        }
    }
}
