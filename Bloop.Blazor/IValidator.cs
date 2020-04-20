using System;
using System.Collections.Generic;
using System.Text;

namespace Bloop.Blazor
{
    public interface IValidator
    {
        Action StateModificationHandler { get; }

        HashSet<IMessage> Messages { get; }
    }
}
