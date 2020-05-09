using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using OpenTK;

namespace MPLibrary.GCN
{
    public class HSFBoneWrapper : STBone
    {
        public ObjectData ObjectData;

        public HSFBoneWrapper(ObjectData obj, STSkeleton skeleton) : base(skeleton) {
            ObjectData = obj;
        }

        public void UpdateTransform()
        {
            ObjectData.BaseTransform.Translate = new Vector3XYZ(
                Position.X,
                Position.Y,
                Position.Z);
            ObjectData.BaseTransform.Scale = new Vector3XYZ(
                Scale.X / HSF_Renderer.PreviewScale,
                Scale.Y / HSF_Renderer.PreviewScale,
                Scale.Z / HSF_Renderer.PreviewScale);
            ObjectData.BaseTransform.Rotate = new Vector3XYZ(
                EulerRotation.X * (float)(180f / Math.PI),
                EulerRotation.Y * (float)(180f / Math.PI),
                EulerRotation.Z * (float)(180f / Math.PI));
        }
    }
}
