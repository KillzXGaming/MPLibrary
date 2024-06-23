using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPLibrary.MP10.IO;

namespace MPLibrary.MP10
{ 
    /// <summary>
     /// Represents an item that contains an index.
     /// </summary>

    public interface IListIndex
    {
        /// <summary>
        /// The 0 index based position of the item in a list.
        /// </summary>
        int Index { get; set; }
    }
}
