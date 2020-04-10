using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using Toolbox.Library.Animations;

namespace MPLibrary.MP10
{
    public class BNFMSA
    {
        public static BNFM_SkeletalAnimation ParseAnimations(BnfmFile header, FileReader reader)
        {
            BNFM_SkeletalAnimation anim = new BNFM_SkeletalAnimation();
            anim.Name = "Animation0";

            List<BNFM_Bone> Bones = new List<BNFM_Bone>();

            //Base data
            uint numBoneInfos = reader.ReadUInt32();
            uint numAnimInfos = reader.ReadUInt32(); //1
            uint numBoneAnims = reader.ReadUInt32();
            uint numTracks = reader.ReadUInt32();
            uint unk = reader.ReadUInt32(); //0
            uint numConstantTracks = reader.ReadUInt32();
            uint numKeyFrames = reader.ReadUInt32();
            uint numKeyedTracks = reader.ReadUInt32();
            uint unk2 = reader.ReadUInt32();//0
            uint unk3 = reader.ReadUInt32();//0
            uint unk4 = reader.ReadUInt32();//0

            uint boneInfoOffset = reader.ReadUInt32();
            uint animInfoOffset = reader.ReadUInt32();
            uint boneAnimOffset = reader.ReadUInt32();
            uint boneTrackOffset = reader.ReadUInt32();
            uint unkOffset = reader.ReadUInt32();
            uint constantKeysOffset = reader.ReadUInt32();
            uint keyedFramesOffset = reader.ReadUInt32();
            uint stringTableOffset = reader.ReadUInt32();

            //The same structure as the model bone
            //This one blanks out some offsets (ie bone connected ones)
            //Also lacks matrices due to being regenerated on animation
            if (numBoneInfos > 0) {
                reader.SeekBegin(boneInfoOffset);
                for (int i = 0; i < numBoneInfos; i++)
                    Bones.Add(new BNFM_Bone(header, reader));
            }
            //This is for multiple animations?
            //Seems to be 1 so skip using this for now
            if (numAnimInfos > 0) {
                reader.SeekBegin(animInfoOffset);
                for (int i = 0; i < numAnimInfos; i++)
                {
                    string name = header.GetString(reader, reader.ReadUInt32());
                    uint nameHash = reader.ReadUInt32();
                    uint trackOffset = reader.ReadUInt32();
                    uint numBones = reader.ReadUInt32();
                    uint unk5 = reader.ReadUInt32(); //1
                    uint unk6 = reader.ReadUInt32(); //0
                    uint frameCount = reader.ReadUInt32();
                    uint unk7 = reader.ReadUInt32(); //0

                    Console.WriteLine($"frameCount {frameCount}");

                    anim.FrameCount = frameCount;
                    if (name != string.Empty)
                        anim.Name = name;
                }
            }

            for (int i = 0; i < numBoneAnims; i++)
            {
                reader.SeekBegin(boneAnimOffset + (i * 0x54));
                uint boneOffset = reader.ReadUInt32();
                //Bones have 10 tracks for SRT
                uint[] numKeysPerTrack = reader.ReadUInt32s(10);
                uint[] trackOffsets = reader.ReadUInt32s(10);

                var group = new AnimationNode();
                anim.AnimGroups.Add(group);

                using (reader.TemporarySeek(boneOffset, System.IO.SeekOrigin.Begin)) {
                    group.Name = header.GetString(reader, reader.ReadUInt32());
                }
                LoadTrackList(trackOffsets, group, reader);
            }

            return anim;
        }

        private static void LoadTrackList(uint[] offsets, AnimationNode group, FileReader reader)
        {
            for (int t = 0; t < 10; t++) {
                if (offsets[t] != uint.MaxValue) {
                    reader.SeekBegin(offsets[t]);
                    switch ((TrackType)t) {
                        case TrackType.TranslateX: LoadTrack(group, group.TranslateX, reader); break;
                        case TrackType.TranslateY: LoadTrack(group, group.TranslateY, reader); break;
                        case TrackType.TranslateZ: LoadTrack(group, group.TranslateZ, reader); break;
                        case TrackType.ScaleX: LoadTrack(group, group.ScaleX, reader); break;
                        case TrackType.ScaleY: LoadTrack(group, group.ScaleY, reader); break;
                        case TrackType.ScaleZ: LoadTrack(group, group.ScaleZ, reader); break;
                        case TrackType.RotateX: LoadTrack(group, group.RotationX, reader); break;
                        case TrackType.RotateY: LoadTrack(group, group.RotationY, reader); break;
                        case TrackType.RotateZ: LoadTrack(group, group.RotationZ, reader); break;
                        case TrackType.RotateW: LoadTrack(group, group.RotationW, reader); break;
                    }
                }
            }
        }

        private static void LoadTrack(AnimationNode group, AnimTrack track,  FileReader reader)
        {
            track = new AnimTrack(group);

            uint keyOffset = reader.ReadUInt32();
            uint frameCount = reader.ReadUInt32();
            uint unk = reader.ReadUInt32(); //0
            uint numKeyFrames = reader.ReadUInt32();
            uint unk2 = reader.ReadUInt32(); //0
            uint unk3 = reader.ReadUInt32(); //0
            reader.ReadUInt16(); // 0
            KeyType type = (KeyType)reader.ReadByte();
            byte unk4 = reader.ReadByte();

            Console.WriteLine($"numKeyFrames {numKeyFrames} {type}");

            reader.SeekBegin(keyOffset);
            for (int i = 0; i < numKeyFrames; i++)
            {
                if (type == KeyType.Normal)
                {
                    float frame = reader.ReadSingle();
                    float value = reader.ReadSingle();

                    track.KeyFrames.Add(new STKeyFrame()
                    {
                       Frame = frame,
                       Value = value,
                    });
                }
                else if (type == KeyType.Hermite)
                {
                    float frame = i;
                    float value = reader.ReadSingle();
                    float padding = reader.ReadSingle();
                    float slopeIn = reader.ReadSingle();
                    float padding2 = reader.ReadSingle();
                    float slopeOut = reader.ReadSingle();

                    track.KeyFrames.Add(new STHermiteKeyFrame()
                    {
                        Frame = frame,
                        Value = value,
                        TangentIn = slopeIn,
                        TangentOut = slopeOut,
                    });
                }
            }
        }

        public enum KeyType
        {
            Normal = 1,
            Hermite = 2,
        }

        public enum TrackType
        {
            TranslateX = 0,
            TranslateY = 1,
            TranslateZ = 2,
            ScaleX = 3,
            ScaleY = 4,
            ScaleZ = 5,
            RotateX = 6,
            RotateY = 7,
            RotateZ = 8,
            RotateW = 9,
        }
    }
}
