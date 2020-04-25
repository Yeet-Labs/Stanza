using System;
using System.Text;

namespace Stanza
{
    public class Evaluation
    {
        public virtual string Information { get; set; }

        public virtual Result Result { get; set; }
    }

    public class Evaluation<T> : Evaluation
    {
        public T Data { get; set; }
    }
}