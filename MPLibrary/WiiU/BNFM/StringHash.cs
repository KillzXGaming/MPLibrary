using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPLibrary.MP10.IO;

namespace MPLibrary.MP10
{
    /// <summary>
    /// Represents a string using a FNV132 hash.
    /// </summary>
    public class StringHash
    {
        /// <summary>
        /// Gets or sets the string.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets the FNV132 hash value created by the Value string.
        /// This value will auto update on save.
        /// </summary>
        public uint Hash { get; internal set; }

        public StringHash() { }

        public StringHash(string value)
        {
            Value = value;
            CalculateHash();
        } 

        /// <summary>
        /// Updates and calculates the current hash.
        /// </summary>
        internal void CalculateHash() {
            if (Value == null) Value = "";

            Hash = FNV.FNV132.Create(Value);
        }

        public override string ToString() {
            return Value;
        }
    }
}
