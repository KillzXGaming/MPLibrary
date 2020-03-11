using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Toolbox.Library
{
    public class STSkeleton
    {
        public List<STBone> bones = new List<STBone>();

        public List<STBone> getBoneTreeOrder()
        {
            List<STBone> bone = new List<STBone>();
            Queue<STBone> q = new Queue<STBone>();

            q.Enqueue(bones[0]);

            while (q.Count > 0)
            {
                STBone b = q.Dequeue();
                foreach (STBone bo in b.GetChildren())
                    q.Enqueue(bo);
                bone.Add(b);
            }
            return bone;
        }

        public Matrix4 GetBoneTransform(int index)
        {
            return GetBoneTransform(bones[index]);
        }

        public Matrix4 GetBoneTransform(STBone Bone)
        {
            if (Bone == null)
                return Matrix4.Identity;
            if (Bone.parentIndex == -1)
                return Bone.GetTransform();
            else
                return Bone.GetTransform() * GetBoneTransform(bones[Bone.parentIndex]);
        }

        public int boneIndex(string name)
        {
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].Name.Equals(name))
                {
                    return i;
                }
            }

            return -1;
        }

        public void reset(bool Main = true)
        {
            for (int i = 0; i < bones.Count; i++)
            {
                bones[i].pos = new Vector3(
                    bones[i].Position.X,
                    bones[i].Position.Y,
                    bones[i].Position.Z);
                bones[i].rot = new Quaternion(
                    bones[i].Rotation.X,
                    bones[i].Rotation.Y,
                    bones[i].Rotation.Z,
                    bones[i].Rotation.W);
                bones[i].sca = new Vector3(
                    bones[i].Scale.X,
                    bones[i].Scale.Y,
                    bones[i].Scale.Z);
            }

            update(true);

            for (int i = 0; i < bones.Count; i++)
            {
                try
                {
                    bones[i].invert = Matrix4.Invert(bones[i].Transform);
                }
                catch (InvalidOperationException)
                {
                    bones[i].invert = Matrix4.Zero;
                }
            }
            update();
        }

        public STBone GetBone(String name)
        {
            foreach (STBone bo in bones)
                if (bo.Name.Equals(name))
                    return bo;
            return null;
        }

        public static Quaternion FromQuaternionAngles(float z, float y, float x, float w)
        {
            {
                Quaternion q = new Quaternion();
                q.X = x;
                q.Y = y;
                q.Z = z;
                q.W = w;

                if (q.W < 0)
                    q *= -1;

                //return xRotation * yRotation * zRotation;
                return q;
            }
        }

        public static Quaternion FromEulerAngles(float z, float y, float x)
        {
            {
                Quaternion xRotation = Quaternion.FromAxisAngle(Vector3.UnitX, x);
                Quaternion yRotation = Quaternion.FromAxisAngle(Vector3.UnitY, y);
                Quaternion zRotation = Quaternion.FromAxisAngle(Vector3.UnitZ, z);

                Quaternion q = (zRotation * yRotation * xRotation);

                if (q.W < 0)
                    q *= -1;

                //return xRotation * yRotation * zRotation;
                return q;
            }
        }

        private bool Updated = false;
        public void update(bool reset = false)
        {
            Updated = true;
            foreach (STBone Bone in bones)
                Bone.Transform = GetMatrix(Bone);
        }

        private Matrix4 GetMatrix(STBone bone)
        {
            var transform = Matrix4.CreateScale(bone.sca) * Matrix4.CreateFromQuaternion(bone.rot) * Matrix4.CreateTranslation(bone.pos);
            if (bone.parentIndex != -1)
                return transform * GetMatrix((STBone)bone.Parent);
            else
                return transform;
        }
    }
}