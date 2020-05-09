using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPLibrary.DS
{
    public class MaterialData
    {
        public string Name { get; set; }

        public MaterialBlock MaterialBlock;

        public MaterialData(MaterialBlock block) {
            MaterialBlock = block;
        }

        public bool LightEnabled(int index) {
            return (MaterialBlock.LightingFlags >> 16 + index) == 1;
        }
    }
}
