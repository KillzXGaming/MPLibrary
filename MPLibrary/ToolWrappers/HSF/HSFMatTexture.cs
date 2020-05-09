using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using MPLibrary;
using OpenTK;

namespace MPLibrary.GCN
{
    public class HSFMatTexture : STGenericTextureMap
    {
        public int TextureIndex { get; set; }
        public AttributeData Attribute { get; set; }

        private HSF ParentHSF;
        public HSFMatTexture(HSF hsf)
        {
            ParentHSF = hsf;
        }

        public void UpdateTransform()
        {
            Vector2 shift = new Vector2(
              Attribute.TexAnimStart.Position.X,
              Attribute.TexAnimStart.Position.Y);
            Vector2 scale = new Vector2(
              Attribute.TexAnimStart.Scale.X,
              Attribute.TexAnimStart.Scale.Y);

            shift = new Vector2(0.5f) + new Vector2(shift.X / 1 - 0.5f, shift.Y / 1 - 0.5f);
            scale = new Vector2(1 / scale.X, 1 / scale.Y);
            Transform = new STTextureTransform()
            {
                Translate = shift,
                Scale = scale,
            };
        }



        public override STGenericTexture GetTexture()
        {
            if (TextureIndex < ParentHSF.GenericTextures.Count && TextureIndex != -1)
                return ParentHSF.GenericTextures[TextureIndex];

            return null;
        }
    }

}
