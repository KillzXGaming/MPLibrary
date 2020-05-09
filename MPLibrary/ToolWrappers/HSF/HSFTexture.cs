using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using MPLibrary.GCN;
using Toolbox.Core.Imaging;

namespace PartyStudio
{
    public class HSFTexture : STGenericTexture
    {
        private TextureInfo TextureInfo;
        private byte[] ImageData;

        public HSFTexture(string name, TextureInfo info, byte[] data)
        {
            Name = name;
            TextureInfo = info;
            ImageData = data;

            Platform = new GamecubeSwizzle();
            ReloadInfo();
        }

        private void ReloadInfo()
        {
            Width = TextureInfo.Width;
            Height = TextureInfo.Height;

            var gcFormat = FormatList[TextureInfo.Format];
            if (gcFormat == Decode_Gamecube.TextureFormats.C8)
            {
                if (TextureInfo.Bpp == 4)
                    gcFormat = Decode_Gamecube.TextureFormats.C4;
            }
            ((GamecubeSwizzle)Platform).Format = gcFormat;
        }

        public static Dictionary<int, Decode_Gamecube.TextureFormats> FormatList = new Dictionary<int, Decode_Gamecube.TextureFormats>()
        {
            { 0x00, Decode_Gamecube.TextureFormats.I8 },
            { 0x01, Decode_Gamecube.TextureFormats.I8 },
            { 0x02, Decode_Gamecube.TextureFormats.IA4 },
            { 0x03, Decode_Gamecube.TextureFormats.IA8 },
            { 0x04, Decode_Gamecube.TextureFormats.RGB565 },
            { 0x05, Decode_Gamecube.TextureFormats.RGB5A3 },
            { 0x06, Decode_Gamecube.TextureFormats.RGBA32 },
            { 0x07, Decode_Gamecube.TextureFormats.CMPR },
            { 0x09, Decode_Gamecube.TextureFormats.C8 }, //C4 if BPP == 4
            { 0x0A, Decode_Gamecube.TextureFormats.C8 }, //C4 if BPP == 4
            { 0x0B, Decode_Gamecube.TextureFormats.C8 }, //C4 if BPP == 4
        };

        public override byte[] GetImageData(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0)
        {
            return Decode_Gamecube.GetMipLevel(ImageData, Width, Height, MipCount, (uint)MipLevel, ((GamecubeSwizzle)Platform).Format);
        }

        public override void SetImageData(List<byte[]> imageData, uint width, uint height, int arrayLevel = 0)
        {
            throw new NotImplementedException();
        }
    }
}
