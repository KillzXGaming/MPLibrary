using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;

namespace MPLibrary.DS
{
    public class TextureBlock : IBlockSection
    {
        public List<ImageBlock> Images = new List<ImageBlock>();

        public void Read(HsdfFile header, FileReader reader)
        {
            List<TextureMapperBlock> NameMappers = new List<TextureMapperBlock>();
            List<PaletteDataBlock> PaletteData = new List<PaletteDataBlock>();

            ushort numInfos = reader.ReadUInt16();
            ushort numImages = reader.ReadUInt16();
            ushort numPalettes = reader.ReadUInt16();
            reader.ReadUInt16(); //padding
            for (int i = 0; i < numInfos + numImages + numPalettes; i++)
            {
                var block = header.ReadBlock(reader);
                if (block is TextureMapperBlock)
                    NameMappers.Add((TextureMapperBlock)block);
                if (block is ImageBlock)
                    Images.Add((ImageBlock)block);
                if (block is PaletteDataBlock)
                    PaletteData.Add((PaletteDataBlock)block);
            }

            foreach (var image in Images) {
                var mapper = NameMappers.FirstOrDefault(x => x.Name == image.Name);
                if (mapper != null) {
                    var palette = PaletteData.FirstOrDefault(x => x.Name == mapper.PaletteName);
                    if (palette != null)
                        image.PaletteData = palette;
                }
            }

            PaletteData.Clear();
            NameMappers.Clear();
        }

        public void Write(HsdfFile header, FileWriter writer)
        {

        }
    }
}
