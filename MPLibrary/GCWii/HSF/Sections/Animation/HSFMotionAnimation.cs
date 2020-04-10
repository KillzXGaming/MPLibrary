using System;
using System.Linq;
using System.Collections.Generic;
using Toolbox.Library.Animations;
using System.Runtime.InteropServices;
using STLibrary;
using OpenTK;

namespace MPLibrary
{
    public class HSFMotionAnimation : STAnimation
    {
        public EventHandler AnimationNextFrame;

        public List<TrackData> trackInfo = new List<TrackData>();

        public uint GetTrackCount()
        {
            uint numTracks = 0;
            foreach (AnimationNode group in AnimGroups)
                numTracks += (uint)group.TrackList.Count;

            return numTracks;
        }

        public List<AnimTrack> GetAllTracks()
        {
            List<AnimTrack> tracks = new List<AnimTrack>();
            for (int i = 0; i < AnimGroups.Count; i++)
                tracks.AddRange(((AnimationNode)AnimGroups[i]).TrackList);
            return tracks;
        }

        public STSkeleton GetActiveSkeleton()
        {
        
            return null;
        }

        public override void NextFrame()
        {
            if (Frame > FrameCount) return;

            AnimationNextFrame?.Invoke(this, EventArgs.Empty);

            var skeleton = GetActiveSkeleton();
            if (skeleton == null) return;

            if (Frame == 0)
                skeleton.reset();

            bool Updated = false; // no need to update skeleton of animations that didn't change
            foreach (AnimationNode node in this.AnimGroups)
            {
                var b = skeleton.GetBone(node.Name);
                if (b == null) continue;

                Updated = true;

                if (node.TranslateX.HasKeys)
                    b.pos.X = node.TranslateX.GetFrameValue(Frame) * HSF_Renderer.PreviewScale;
                if (node.TranslateY.HasKeys)
                    b.pos.Y = node.TranslateY.GetFrameValue(Frame) * HSF_Renderer.PreviewScale;
                if (node.TranslateZ.HasKeys)
                    b.pos.Z = node.TranslateZ.GetFrameValue(Frame) * HSF_Renderer.PreviewScale;

                if (node.ScaleX.HasKeys)
                    b.sca.X = node.ScaleX.GetFrameValue(Frame);
                else b.sca.X = 1;
                if (node.ScaleY.HasKeys)
                    b.sca.Y = node.ScaleY.GetFrameValue(Frame);
                else b.sca.Y = 1;
                if (node.ScaleZ.HasKeys)
                    b.sca.Z = node.ScaleZ.GetFrameValue(Frame);
                else b.sca.Z = 1;


                if (node.RotationX.HasKeys || node.RotationY.HasKeys || node.RotationZ.HasKeys)
                {
                    float x = node.RotationX.HasKeys ? MathHelper.DegreesToRadians(node.RotationX.GetFrameValue(Frame)) : b.EulerRotation.X;
                    float y = node.RotationY.HasKeys ? MathHelper.DegreesToRadians(node.RotationY.GetFrameValue(Frame)) : b.EulerRotation.Y;
                    float z = node.RotationZ.HasKeys ? MathHelper.DegreesToRadians(node.RotationZ.GetFrameValue(Frame)) : b.EulerRotation.Z;
                    b.rot = EulerToQuat(z, y, x);
                }
            }

            if (Updated)
            {
                skeleton.update();
            }
        }

        public static Quaternion EulerToQuat(float z, float y, float x)
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
    }

    public class AnimationNode : STAnimGroup
    {
        public AnimTrack TranslateX => FindByEffect(TrackEffect.TranslateX);
        public AnimTrack TranslateY => FindByEffect(TrackEffect.TranslateY);
        public AnimTrack TranslateZ => FindByEffect(TrackEffect.TranslateZ);
        public AnimTrack RotationX => FindByEffect(TrackEffect.RotationX);
        public AnimTrack RotationY => FindByEffect(TrackEffect.RotationY);
        public AnimTrack RotationZ => FindByEffect(TrackEffect.RotationZ);
        public AnimTrack ScaleX => FindByEffect(TrackEffect.ScaleX);
        public AnimTrack ScaleY => FindByEffect(TrackEffect.ScaleY);
        public AnimTrack ScaleZ => FindByEffect(TrackEffect.ScaleZ);

        public TrackMode Mode = TrackMode.Normal;

        public List<AnimTrack> TrackList = new List<AnimTrack>();

        public short ValueIndex { get; set; }

        public bool IsBone
        {
            get
            {
                for (int i = 0; i < TrackList.Count; i++)
                    if (TrackList[i].TrackMode == TrackMode.Normal)
                        return true;
                return false;
            }
        }

        public override List<STAnimationTrack> GetTracks()
        {
            List<STAnimationTrack> tracks = new List<STAnimationTrack>();
            tracks.AddRange(TrackList);
            return tracks;
        }

        private AnimTrack FindByEffect(TrackEffect effect)
        {
            for (int i = 0; i < TrackList.Count; i++)
                if (TrackList[i].TrackEffect == effect)
                    return TrackList[i];

            return new AnimTrack(this);
        }
    }

    public class AnimTrack : STAnimationTrack
    {
        public TrackEffect TrackEffect;

        public float Constant;
        public short ConstantUnk;
        public short Type;
        public byte Unknown;
        public short ValueIdx;

        public TrackMode TrackMode;

        public AnimationNode ParentGroup;

        public AnimTrack(AnimationNode group) {
            ParentGroup = group;
        }

        public override float GetFrameValue(float frame, float startFrame = 0)
        {
            if (InterpolationType == STInterpoaltionType.Constant)
                return Constant;
             
            if (KeyFrames.Count == 0) return 0;
            if (KeyFrames.Count == 1) return KeyFrames[0].Value;

            STKeyFrame LK = KeyFrames.First();
            STKeyFrame RK = KeyFrames.Last();

            float Frame = frame - startFrame;

            foreach (STKeyFrame keyFrame in KeyFrames)
            {
                if (keyFrame.Frame <= Frame) LK = keyFrame;
                if (keyFrame.Frame >= Frame && keyFrame.Frame < RK.Frame) RK = keyFrame;
            }

            if (LK.Frame != RK.Frame)
            {
                float FrameDiff = Frame - LK.Frame;
                float Weight = FrameDiff / (RK.Frame - LK.Frame);

                switch (InterpolationType)
                {
                    case STInterpoaltionType.Constant: return LK.Value;
                    case STInterpoaltionType.Step: return LK.Value;
                    case STInterpoaltionType.Linear: return InterpolationHelper.Lerp(LK.Value, RK.Value, Weight);
                    case STInterpoaltionType.Bezier:
                        {
                            return InterpolationHelper.Lerp(LK.Value, RK.Value, Weight);

                            STBezierKeyFrame bezierKeyLK = (STBezierKeyFrame)LK;
                            STBezierKeyFrame bezierKeyRK = (STBezierKeyFrame)RK;

                            float length = RK.Frame - LK.Frame;

                            return InterpolationHelper.BezierInterpolate(frame,
                             bezierKeyLK.Frame,
                             bezierKeyRK.Frame,
                             bezierKeyLK.SlopeIn,
                             bezierKeyRK.SlopeOut,
                             bezierKeyLK.Value,
                             bezierKeyRK.Value);
                        }
                    case STInterpoaltionType.Hermite:
                        {
                            STHermiteKeyFrame hermiteKeyLK = (STHermiteKeyFrame)LK;
                            STHermiteKeyFrame hermiteKeyRK = (STHermiteKeyFrame)RK;

                            float length = RK.Frame - LK.Frame;

                            return InterpolationHelper.HermiteInterpolate(frame,
                             hermiteKeyLK.Frame,
                             hermiteKeyRK.Frame,
                             hermiteKeyLK.TangentIn,
                             hermiteKeyLK.TangentOut,
                             hermiteKeyLK.Value,
                             hermiteKeyRK.Value);
                        }
                }
            }

            return LK.Value;
        }
    }
}
