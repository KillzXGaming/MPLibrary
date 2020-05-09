using System;
using System.Linq;
using System.Collections.Generic;
using Toolbox.Core.Animations;
using System.Runtime.InteropServices;
using Toolbox.Core;
using OpenTK;
using MPLibrary.GCN;

namespace MPLibrary.MP10
{
    public class BNFM_SkeletalAnimation : STAnimation
    {
        public EventHandler AnimationNextFrame;

        public List<TrackData> trackInfo = new List<TrackData>();

        public uint GetTrackCount()
        {
            int numTracks = 0;
            foreach (var group in AnimGroups)
                numTracks += group.GetTracks().Count;

            return (uint)trackInfo.Count;
        }

        public List<STAnimationTrack> GetAllTracks()
        {
            List<STAnimationTrack> tracks = new List<STAnimationTrack>();
            for (int i = 0; i < AnimGroups.Count; i++)
                tracks.AddRange(AnimGroups[i].GetTracks());
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
        }
    }

    public class AnimationNode : STAnimGroup
    {
        public AnimTrack TranslateX { get; set; }
        public AnimTrack TranslateY { get; set; }
        public AnimTrack TranslateZ { get; set; }
        public AnimTrack RotationX { get; set; }
        public AnimTrack RotationY { get; set; }
        public AnimTrack RotationZ { get; set; }
        public AnimTrack RotationW { get; set; }
        public AnimTrack ScaleX { get; set; }
        public AnimTrack ScaleY { get; set; }
        public AnimTrack ScaleZ { get; set; }

        public override List<STAnimationTrack> GetTracks()
        {
            List<STAnimationTrack> tracks = new List<STAnimationTrack>();
            tracks.Add(TranslateX);
            tracks.Add(TranslateY);
            tracks.Add(TranslateZ);
            tracks.Add(RotationX);
            tracks.Add(RotationY);
            tracks.Add(RotationZ);
            tracks.Add(RotationW);
            tracks.Add(ScaleX);
            tracks.Add(ScaleY);
            tracks.Add(ScaleZ);
            return tracks;
        }
    }

    public class AnimTrack : STAnimationTrack
    {
        public AnimationNode ParentGroup;

        public AnimTrack(AnimationNode group)
        {
            ParentGroup = group;
        }
    }
}
