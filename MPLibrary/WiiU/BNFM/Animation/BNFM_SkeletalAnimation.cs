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
    public class BNFM_SkeletalAnimation : STSkeletonAnimation
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
    }
}
