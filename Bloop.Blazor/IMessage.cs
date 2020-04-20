using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bloop.Blazor
{
    public interface IMessage
    {
        IEnumerable<Validation.Analysis> Results { get; }

        RenderFragment<Validation.Analysis> ChildContent { get; }
    }
}
