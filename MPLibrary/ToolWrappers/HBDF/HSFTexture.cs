using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using MPLibrary.GCN;
using Toolbox.Core.Imaging;
using MPLibrary.DS;

namespace PartyStudio
{
    public class HBDFTexture : STGenericTexture
    {
        private ImageBlock TextureInfo;

        public HBDFTexture(ImageBlock info)
        {
            Name = info.Name;
            TextureInfo = info;
            Platform = new NitroSwizzle(TextureInfo.Format);
            if (TextureInfo.PaletteData != null)
                ((NitroSwizzle)Platform).PaletteData = TextureInfo.PaletteData.Data;
            ReloadInfo();
        }

        private void ReloadInfo()
        {
            Width = TextureInfo.Width;
            Height = TextureInfo.Height;

            ((NitroSwizzle)Platform).Format = TextureInfo.Format;
        }

        public override byte[] GetImageData(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0) {
            return TextureInfo.ImageData;
        }

        public override void SetImageData(List<byte[]> imageData, uint width, uint height, int arrayLevel = 0)
        {
            throw new NotImplementedException();
        }
    }
}
