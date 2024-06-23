using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using MPLibrary.MP10.IO;

namespace MPLibrary.MP10
{
    public class BnfmMatrix : IFileData
    {
        /// <summary>
        /// Gets or sets a 4x4 matrix.
        /// </summary>
        public Matrix4x4 Matrix { get; set; } = Matrix4x4.Identity;

        void IFileData.Read(FileReader reader)
        {
            Matrix = reader.ReadMatrix3x4();
        }

        void IFileData.Write(FileWriter writer)
        {
            writer.Write(Matrix);
        }
    }
}
