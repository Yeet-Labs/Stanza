using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stanza.Blazor
{
    public interface IEvaluationView
    {
        IEnumerable<Evaluation> Results { get; }

        RenderFragment<Evaluation> ChildContent { get; }
    }
}
