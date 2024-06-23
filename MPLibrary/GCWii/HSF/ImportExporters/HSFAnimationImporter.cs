using IONET;
using IONET.Core;
using IONET.Core.Animation;
using MPLibrary.GCN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.Animations;
using static GCNRenderLibrary.Rendering.GXTextureMatrix;

namespace MPLibrary.GCWii.HSF
{
    public class HSFAnimationImporter
    {
        public static void Replace(HsfFile hsf, HSFMotionAnimation anim, string filePath)
        {
            //Clear all obj related animations
            var motion_list = anim.AnimGroups.Where(x => ((AnimationNode)x).Mode == TrackMode.Object).ToList();

            foreach (var motion in motion_list)
                anim.AnimGroups.Remove(motion);

            Import(hsf, anim, filePath);
        }

        public static void Import(HsfFile hsf, HSFMotionAnimation anim, string filePath)
        {
            IOScene scene = IOManager.LoadScene(filePath, new ImportSettings());
            foreach (var g in scene.Animations)
                LoadObjectAnimations(hsf, anim, g);
        }

        static void LoadObjectAnimations(HsfFile hsf, HSFMotionAnimation hsfMotion, IOAnimation anim)
        {
            //Find 
            foreach (var obj in hsf.ObjectNodes)
            {
                //animation group matches an animated object name in the scene hierarchy
                if (obj.Name == anim.Name)
                {
                    //Check if already animated
                    var node_anim = hsfMotion.GetGroup(anim.Name);
                    //No animation group found, create new one
                    if (node_anim == null)
                    {
                        node_anim = CreateObjectAnimation(obj);
                        hsfMotion.AnimGroups.Add(node_anim);
                    }

                    //Insert each track key
                    foreach (var iotrack in anim.Tracks)
                    {
                        if (iotrack.KeyFrames.Count == 1)
                        {
                            return;
                        }

                        switch (iotrack.ChannelType)
                        {
                            case IOAnimationTrackType.PositionX: CreateTrack(node_anim.TranslateX, iotrack); break;
                            case IOAnimationTrackType.PositionY: CreateTrack(node_anim.TranslateY, iotrack); break;
                            case IOAnimationTrackType.PositionZ: CreateTrack(node_anim.TranslateZ, iotrack); break;
                            case IOAnimationTrackType.RotationEulerX: CreateRotationTrack(node_anim.RotationX, iotrack); break;
                            case IOAnimationTrackType.RotationEulerY: CreateRotationTrack(node_anim.RotationY, iotrack); break;
                            case IOAnimationTrackType.RotationEulerZ: CreateRotationTrack(node_anim.RotationZ, iotrack); break;
                            case IOAnimationTrackType.ScaleX: CreateTrack(node_anim.ScaleX, iotrack); break;
                            case IOAnimationTrackType.ScaleY: CreateTrack(node_anim.ScaleY, iotrack); break;
                            case IOAnimationTrackType.ScaleZ: CreateTrack(node_anim.ScaleZ, iotrack); break;
//
                        }
                    }

                    //quats (have to be baked)
                    var quatX = anim.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.QuatX);
                    var quatY = anim.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.QuatY);
                    var quatZ = anim.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.QuatZ);
                    var quatW = anim.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.QuatW);

                    bool hasQuat = quatX != null || quatY != null || quatZ != null;

                    if (hasQuat)
                    {
                        node_anim.RotationX.InterpolationType = STInterpoaltionType.Linear;
                        node_anim.RotationY.InterpolationType = STInterpoaltionType.Linear;
                        node_anim.RotationZ.InterpolationType = STInterpoaltionType.Linear;
                        node_anim.RotationX.KeyFrames.Clear();
                        node_anim.RotationY.KeyFrames.Clear();
                        node_anim.RotationZ.KeyFrames.Clear();

                        float last_value_x = 0;
                        float last_value_y = 0;
                        float last_value_z = 0;

                        bool HasKey(IOAnimationTrack track, float frame)
                        {
                            if (track == null) return false;

                            return track.KeyFrames.Any(x => x.Frame == frame);
                        }

                        for (int i = 0; i < hsfMotion.FrameCount; i++)
                        {
                            float frame = i;

                            //Check if the current frame has any keyframes present in any 4 tracks
                            if (!(HasKey(quatX, frame) || HasKey(quatY, frame) ||
                                  HasKey(quatZ, frame) || HasKey(quatW, frame)))
                                continue;


                            Quaternion quat = new Quaternion(
                                 quatX != null ? quatX.GetFrameValue(i) : 0,
                                 quatY != null ? quatY.GetFrameValue(i) : 0,
                                 quatZ != null ? quatZ.GetFrameValue(i) : 0,
                                 quatW != null ? quatW.GetFrameValue(i) : 1);

                            //to euler rotations
                            var rot = ToEulerAngles(quat) * STMath.Rad2Deg;

                            if (rot.X != last_value_x)
                                node_anim.RotationX.SetKeyFrame(frame, rot.X, 0, 0);
                            if (rot.Y != last_value_y)
                                node_anim.RotationY.SetKeyFrame(frame, rot.Y, 0, 0);
                            if (rot.Z != last_value_z)
                                node_anim.RotationZ.SetKeyFrame(frame, rot.Z, 0, 0);

                            last_value_x = rot.X;
                            last_value_y = rot.Y;
                            last_value_z = rot.Z;
                        }
                    }
                }
            }

            foreach (var g in anim.Groups)
                LoadObjectAnimations(hsf, hsfMotion, g);
        }

        public static Vector3 ToEulerAngles(Quaternion q)
        {
            Vector3 angles = new();

            // roll (x-axis rotation)
            float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.Z = MathF.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            float sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
            {
                angles.X = MathF.CopySign(MathF.PI / 2, sinp);
            }
            else
            {
                angles.X = MathF.Asin(sinp);
            }

            // yaw (z-axis rotation)
            float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Y = MathF.Atan2(siny_cosp, cosy_cosp);

            return angles;

            float Clamp(float v, float min, float max)
            {
                if (v < min) return min;
                if (v > max) return max;
                return v;
            }

            Matrix4x4 mat = Matrix4x4.CreateFromQuaternion(q);
            float x, y, z;
            y = (float)Math.Asin(Clamp(mat.M13, -1, 1));

            if (Math.Abs(mat.M13) < 0.99999)
            {
                x = (float)Math.Atan2(-mat.M23, mat.M33);
                z = (float)Math.Atan2(-mat.M12, mat.M11);
            }
            else
            {
                x = (float)Math.Atan2(mat.M32, mat.M22);
                z = 0;
            }
            return new Vector3(x, y, z) * -1;
        }

        static void CreateRotationTrack(AnimTrack track, IOAnimationTrack iotrack)
        {
            if (iotrack.KeyFrames.Count == 1) //constant
            {
                track.InterpolationType = STInterpoaltionType.Constant;
                track.SetKeyFrame(0, iotrack.KeyFrames[0].ValueF32 * STMath.Rad2Deg, 0, 0);
                return;
            }

            bool is_bezier = iotrack.KeyFrames.Any(x => x is IOKeyFrameHermite);

            track.InterpolationType = STInterpoaltionType.Linear;

            if (is_bezier)
                track.InterpolationType = STInterpoaltionType.Bezier;

            track.KeyFrames.Clear(); //clear keys incase already keyed
            foreach (var key in iotrack.KeyFrames)
            {
                if (key is IOKeyFrameHermite)
                    track.SetKeyFrame(key.Frame, key.ValueF32,
                        ((IOKeyFrameHermite)key).TangentSlopeInput,
                        ((IOKeyFrameHermite)key).TangentSlopeOutput);
                else
                    track.SetKeyFrame(key.Frame, key.ValueF32, 0, 0);
            }
        }

        static void CreateTrack(AnimTrack track, IOAnimationTrack iotrack)
        {
            if (iotrack.KeyFrames.Count == 1) //constant
            {
                track.InterpolationType = STInterpoaltionType.Constant;
                track.SetKeyFrame(0, iotrack.KeyFrames[0].ValueF32 * STMath.Rad2Deg, 0, 0);
                return;
            }

            bool is_bezier = iotrack.KeyFrames.Any(x => x is IOKeyFrameHermite);

            track.InterpolationType = STInterpoaltionType.Linear;

            if (is_bezier)
                track.InterpolationType = STInterpoaltionType.Bezier;

            float last_value = 0XFFF;

            track.KeyFrames.Clear(); //clear keys incase already keyed
            foreach (var key in iotrack.KeyFrames)
            {
                if (last_value == key.ValueF32)
                    continue; //skip duped keys to optimize file size

                if (key is IOKeyFrameHermite)
                    track.SetKeyFrame(key.Frame, key.ValueF32,
                        ((IOKeyFrameHermite)key).TangentSlopeInput,
                        ((IOKeyFrameHermite)key).TangentSlopeOutput);
                else
                    track.SetKeyFrame(key.Frame, key.ValueF32, 0, 0);

                last_value = key.ValueF32;
            }
        }

        /// <summary>
        /// Creates the default object node group before keying additional motion data
        /// </summary>
        /// <param name="obj"></param>
        static AnimationNode CreateObjectAnimation(HSFObject obj)
        {
            AnimationNode node = new AnimationNode(TrackMode.Object);
            node.Mode = TrackMode.Normal;
            node.Name = obj.Name;

            void CreateTrack(TrackEffect track, float value) {
                node.TrackList.Add(new AnimTrack(node, TrackMode.Normal, track, value));
            }

            CreateTrack(TrackEffect.Visible, 1.0f);
            CreateTrack((TrackEffect)25, 1.0f);
            CreateTrack((TrackEffect)26, 1.0f);
            CreateTrack((TrackEffect)27, 1.0f);
            CreateTrack(TrackEffect.TranslateX, obj.Data.BaseTransform.Translate.X);
            CreateTrack(TrackEffect.TranslateY, obj.Data.BaseTransform.Translate.Y);
            CreateTrack(TrackEffect.TranslateZ, obj.Data.BaseTransform.Translate.Z);
            CreateTrack(TrackEffect.RotationX, obj.Data.BaseTransform.Rotate.X);
            CreateTrack(TrackEffect.RotationY, obj.Data.BaseTransform.Rotate.Y);
            CreateTrack(TrackEffect.RotationZ, obj.Data.BaseTransform.Rotate.Z);
            CreateTrack(TrackEffect.ScaleX, obj.Data.BaseTransform.Scale.X);
            CreateTrack(TrackEffect.ScaleY, obj.Data.BaseTransform.Scale.Y);
            CreateTrack(TrackEffect.ScaleZ, obj.Data.BaseTransform.Scale.Z);

            return node;
        }
    }
}
