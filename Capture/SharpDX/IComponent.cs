using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDX
{
    // From SharpDX.Toolkit

    /// <summary>
    /// Base interface for a component base.
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Gets the name of this component.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; set; }
    }
}
