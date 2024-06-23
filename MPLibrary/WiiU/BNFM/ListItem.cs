using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPLibrary.MP10.IO;

namespace MPLibrary.MP10
{
    /// <summary>
    /// Represents an item that contains an index and a flag.
    /// These may refer to the data inside .mcf file binaries/xml.
    /// </summary>
    public class ListItem : IListIndex
    {
        /// <summary>
        /// The 0 index based position of the item in a list.
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// A sort of flag with an unknown purpose.
        /// </summary>
        public int Flag { get; set; }
    }
}
