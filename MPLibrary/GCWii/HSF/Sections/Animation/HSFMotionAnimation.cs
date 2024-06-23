using System;
using System.Linq;
using System.Collections.Generic;
using Toolbox.Core.Animations;
using System.Runtime.InteropServices;
using Toolbox.Core;
using OpenTK;
using System.Text.Json.Serialization;

namespace MPLibrary.GCN
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

        public AnimationNode GetGroup(string name) {
            return AnimGroups.FirstOrDefault(x => x.Name == name) as AnimationNode;
        }

        public STSkeleton GetActiveSkeleton()
        {
            return null;
        }

        public override void NextFrame()
        {
            if (Frame > FrameCount) return;

            AnimationNextFrame?.Invoke(this, EventArgs.Empty);
        }

        public override void Reset()
        {
            base.Reset();
        }

        public void Init()
        {

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

        public AnimTrack FindByEffect(TrackEffect effect)
        {
            for (int i = 0; i < TrackList.Count; i++)
                if (TrackList[i].TrackEffect == effect)
                    return TrackList[i];

            return new AnimTrack(this);
        }

        public AnimationNode() { }

        public AnimationNode(TrackMode mode)
        {
            this.Mode = TrackMode.Material;
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

        [JsonIgnore]
        public AnimationNode ParentGroup;

        public AnimTrack(AnimationNode group) {
            ParentGroup = group;
        }

        public AnimTrack(AnimationNode group, TrackMode mode, TrackEffect track, float constant)
        {
            ParentGroup = group;
            TrackMode = mode;
            TrackEffect = track;
            ConstantUnk = 0;
            Constant = constant;
            this.InterpolationType = STInterpoaltionType.Constant;
            this.OnKeyInserted += delegate
            {
                if (this.KeyFrames.Count > 0 && this.InterpolationType == STInterpoaltionType.Constant)
                {
                    this.InterpolationType = STInterpoaltionType.Linear;

                    if (TrackEffect == TrackEffect.Visible || TrackEffect == TrackEffect.TextureIndex)
                        this.InterpolationType = STInterpoaltionType.Step;
                }
            };
        }

        public void SetKeyFrame(float frame, float value, float in_slope = 0, float out_slope = 0)
        {
            if (this.InterpolationType == STInterpoaltionType.Bezier)
            {
                this.KeyFrames.Add(new STBezierKeyFrame()
                {
                    Frame = frame,
                    Value = value,
                    SlopeIn = in_slope,
                    SlopeOut = out_slope,
                });
            }
            else
            {
                this.KeyFrames.Add(new STKeyFrame()
                {
                    Frame = frame,
                    Value = value,
                });
            }
        }

        public override float GetFrameValue(float frame)
        {
            if (KeyFrames.Count == 0 && InterpolationType == STInterpoaltionType.Constant)
                return Constant;
             
            if (KeyFrames.Count == 0) return 0;
            if (KeyFrames.Count == 1) return KeyFrames[0].Value;

            STKeyFrame LK = KeyFrames.First();
            STKeyFrame RK = KeyFrames.Last();

            float Frame = frame - StartFrame;

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
                            STBezierKeyFrame bezierKeyLK = (STBezierKeyFrame)LK;
                            STBezierKeyFrame bezierKeyRK = (STBezierKeyFrame)RK;
                            float length = RK.Frame - LK.Frame;

                            return GetPointHermite(
                                bezierKeyLK.Value,
                                bezierKeyRK.Value,
                                bezierKeyLK.SlopeIn,
                                bezierKeyRK.SlopeOut, (frame - LK.Frame) / length);
                        }
                }
            }

            return LK.Value;
        }

        private static float GetPointHermite(float p0, float p1, float s0, float s1, float t)
        {
            float cf0 = (p0 * 2) + (p1 * -2) + (s0 * 1) + (s1 * 1);
            float cf1 = (p0 * -3) + (p1 * 3) + (s0 * -2) + (s1 * -1);
            float cf2 = (p0 * 0) + (p1 * 0) + (s0 * 1) + (s1 * 0);
            float cf3 = (p0 * 1) + (p1 * 0) + (s0 * 0) + (s1 * 0);
            return GetPointCubic(cf0, cf1, cf2, cf3, t);
        }

        private static float GetPointBezier(float p0, float p1, float p2, float p3, float t)
        {
            float cf0 = (p0 * -1) + (p1 * 3) + (p2 * -3) + (p3 * 1);
            float cf1 = (p0 * 3) + (p1 * -6) + (p2 * 3) + (p3 * 0);
            float cf2 = (p0 * -3) + (p1 * 3) + (p2 * 0) + (p3 * 0);
            float cf3 = (p0 * 1) + (p1 * 0) + (p2 * 0) + (p3 * 0);
            return GetPointCubic(cf0, cf1, cf2, cf3, t);
        }

        private static float GetPointCubic(float cf0, float cf1, float cf2, float cf3, float t)
        {
            return (((cf0 * t + cf1) * t + cf2) * t + cf3);
        }
    }
}
