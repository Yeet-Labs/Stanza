using System;
using System.Collections.Generic;
using System.Text;

namespace Stanza
{
    // TODO: Make Identifier a key for Chain extraction and add Data to host format data, or somehow store extraction identifiers with chained bundles in nodes that hold such information in Chain so that Identifier is used for formatting only.

    public interface IBundle
    { 
        string Identifier { get; }

        IEnumerable<Evaluation> Evaluations { get; }
    }

    public interface IMutableBundle : IBundle
    {
        object Data { set; }
    }

    public abstract class Bundle<T> : IBundle
    {
        public Validation<T> Validation { get; set; }

        public string Identifier { get; set; }

        public virtual T Data { get; set; }

        public Evaluator<T> Evaluator { get; set; }

        public IEnumerable<Evaluation> Evaluations => (Evaluator ??= Validation.BuildEvaluator()).Invoke(Data).GetFormattedEvaluations(Identifier);
    }

    //DeferredBundle

    public class LateBundle<T> : Bundle<T>
    {
        public Func<T> Fetcher { get; set; }

        public override T Data => Fetcher.Invoke();
    }

    public class DirectBundle<T> : Bundle<T>, IMutableBundle
    {
        public bool Loose { get; set; }

        object IMutableBundle.Data
        {
            set
            {
                if (Loose)
                {
                    Data = (T)value;
                }
            }
        }
    }
}
