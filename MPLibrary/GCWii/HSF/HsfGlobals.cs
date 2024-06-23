using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPLibrary.GCN
{
    public class HsfGlobals
    {
        public static readonly uint NULL = 3435973836;

        public static readonly int PASS_BITS = 0xF; //If pass_flags when ANDed by this value isn't 0 then Z Writes are Disabled

        public static readonly int BILLBOARD = 0x1;
        public static readonly int DONT_CULL_BACKFACES = 0x2;
        public static readonly int DRAW_SHADOW = 0x8;
        public static readonly int BLEND_MODE_MASK = 0x30;
        public static readonly int BLEND_SRCALPHA_ONE = 0x10;
        public static readonly int BLEND_ZERO_INVSRCCLR = 0x20;
        public static readonly int BLEND_SRCALPHA_INVSRCALPHA = 0x0;
        public static readonly int PUNCHTHROUGH_ALPHA_BITS = 0x1200;
        public static readonly int MATERIAL_INDEX_MASK = 0xFFF;
        public static readonly int HIGHLIGHT_FRAME_MASK = 0xF0;
        public static readonly int HIGHLIGHT_ENABLE = 0x100;
        public static readonly int TOON_ENABLE = 0x200;
        public static readonly int CULL_FRONTFACES = 0x800000;
        public static readonly int OBJ_HIDE = 0x400; //used by item hook meshes
        public static readonly int NO_DEPTH_WRITE = 0x800;

        //Attribute Defines
        public static readonly int WRAP_CLAMP = 0;
        public static readonly int WRAP_REPEAT = 1;
        public static readonly int ENABLE_NEAREST_FILTER = 0x40;
        public static readonly int ENABLE_MIPMAP = 0x80;
        public static readonly int MIPMAP_BIT_POS = 7;
    }
}
