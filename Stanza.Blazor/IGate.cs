using System;
using System.Collections.Generic;
using System.Text;

namespace Stanza.Blazor
{
    // ICollector, IValidator, IGate, ISluice

    public interface IGate
    {
        Action StateModificationHandler { get; }

        HashSet<IEvaluationView> ChildEvaluationViews { get; }
    }
}
