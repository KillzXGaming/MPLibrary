using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using Toolbox.Library.Animations;
using System.Runtime.InteropServices;
using STLibrary;
using OpenTK;

namespace MPLibrary
{
    public class TrackData
    {
        public TrackMode mode;
        public byte unk;
        public short stringOffset;
        public short valueIndex;
        public TrackEffect effect;
        public InterpolationMode interpolate_type;
        public short keyframe_count;
        public int keyframe_offset;
        public float Constant;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MotionData
    {
        public uint NameOffset;
        public uint TrackCount;
        public uint TrackDataOffset;
        public float MotionLength;
    }

    public enum TrackMode : byte
    {
        Normal = 2,
        Object = 3,
        Unknown = 4,
        Material = 9,
        Attriubute = 10,
    }

    public enum TrackEffect : short
    {
        TranslateX = 8,
        TranslateY = 9,
        TranslateZ = 10,
        RotationX = 28,
        RotationY = 29,
        RotationZ = 30,
        ScaleX = 31,
        ScaleY = 32,
        ScaleZ = 33,
    }

    public enum MaterialTrackEffect : short
    {
        LitAmbientColorR = 0,
        LitAmbientColorG = 1,
        LitAmbientColorB = 2,

        TranslateX = 8,
        TranslateY = 9,
        TranslateZ = 10,
        RotationX = 28,
        RotationY = 29,
        RotationZ = 30,
        ScaleX = 31,
        ScaleY = 32,
        ScaleZ = 33,

        AmbientColorR = 49,
        AmbientColorG = 50,
        AmbientColorB = 51,
        ShadowColorR = 52,
        ShadowColorG = 53,
        ShadowColorB = 54,
        HiliteScale = 55,
        Unknown = 56,
        Transparency = 57,
        MatUnknown3 = 58,
        MatUnknown4 = 59,
        ReflectionBrightness = 60,
        MatUnknown5 = 61,
    }


    public enum AttributeTrackEffect : short
    {
        //Used for texture SRT
        TranslateX = 8,
        TranslateY = 9,
        TranslateZ = 10,
        RotationX = 28,
        RotationY = 29,
        RotationZ = 30,
        ScaleX = 31,
        ScaleY = 32,
        ScaleZ = 33,
    }
    public enum InterpolationMode : short
    {
        Step = 0,
        Linear = 1,
        Bezier = 2,
        Bitmap = 3,
        Constant = 4,
        Zero = 5,
    }

    public class MotionDataSection : HSFSection
    {
        public List<HSFMotionAnimation> Animations = new List<HSFMotionAnimation>();

        public List<string> GetStrings()
        {
            List<string> values = new List<string>();
            foreach (var anim in Animations) {
                foreach (AnimationNode group in anim.AnimGroups)
                    if (group.ValueIndex == 0)
                        values.Add(group.Name);
                values.Add(anim.Name);
            }
            return values;
        }

        public override void Read(FileReader reader, HsfFile header)
        {
            List<MotionData> anims = reader.ReadMultipleStructs<MotionData>(this.Count);
            long pos = reader.Position;

            for (int i = 0; i < anims.Count; i++)
            {
                HSFMotionAnimation anm = new HSFMotionAnimation();
                Animations.Add(anm);

                anm.Name = header.GetString(reader, anims[i].NameOffset);
                anm.FrameCount = anims[i].MotionLength;

                reader.SeekBegin(pos + anims[i].TrackDataOffset);
                for (int j = 0; j < anims[i].TrackCount; j++) {
                    TrackData track = new TrackData();
                    track.mode = (TrackMode)reader.ReadByte();
                    track.unk = reader.ReadByte();
                    track.stringOffset = reader.ReadInt16();
                    track.valueIndex = reader.ReadInt16(); //Used if no string (stringOffset = -1)
                    track.effect = (TrackEffect)reader.ReadInt16();
                    track.interpolate_type = (InterpolationMode)reader.ReadInt16();
                    track.keyframe_count = reader.ReadInt16();
                    if (track.keyframe_count > 0 && track.interpolate_type != InterpolationMode.Constant)
                        track.keyframe_offset = reader.ReadInt32();
                    else
                        track.Constant = reader.ReadSingle();

                    Console.WriteLine($"{j} TrackMode {track.mode} valueIndex {track.valueIndex} effect {track.effect} interp {track.interpolate_type} Constant {track.Constant}");

                    if (track.valueIndex != 0)
                    {
                        //Console.WriteLine($"valueIndex {track.valueIndex} str {track.stringOffset}");
                    }

                    anm.trackInfo.Add(track);
                }
            }

            Dictionary<string, AnimationNode> animationNodes = new Dictionary<string, AnimationNode>();

            long dataStart = reader.Position;
            for (int i = 0; i < Animations.Count; i++)
            {
                var anim = Animations[i];
                for (int j = 0; j < anim.trackInfo.Count; j++)
                {
                    var track = anim.trackInfo[j];
                    string name = header.GetString(reader, (uint)track.stringOffset);
                    if (track.stringOffset == -1) {
                        name = $"{track.mode}_{track.valueIndex}";
                    }
                    else if (track.valueIndex > 0)
                        name = $"{name}_{track.valueIndex}";

                    if (!animationNodes.ContainsKey(name))
                        animationNodes.Add(name, new AnimationNode() 
                        {
                            Name = name,
                            Mode = track.mode,
                            ValueIndex = track.valueIndex,
                        });

                    AnimationNode currentGroup = animationNodes[name];

                    List<STKeyFrame> keyFrames = new List<STKeyFrame>();
                    if (track.keyframe_count > 0 && track.interpolate_type != InterpolationMode.Constant)
                    {
                        reader.SeekBegin(dataStart + track.keyframe_offset);
                        for (int key = 0; key < track.keyframe_count; key++)
                        {
                            switch (track.interpolate_type)
                            {
                                //8 bytes
                                case InterpolationMode.Step:
                                    {
                                        keyFrames.Add(new STKeyFrame()
                                        {
                                            Frame = reader.ReadSingle(),
                                            Value = reader.ReadSingle(),
                                        });
                                    }
                                    break;
                                //8 bytes
                                case InterpolationMode.Bitmap:
                                    {
                                        keyFrames.Add(new STKeyFrame()
                                        {
                                            Frame = reader.ReadSingle(),
                                            Value = reader.ReadInt32(),
                                        });
                                    }
                                    break;
                                //16 bytes
                                case InterpolationMode.Bezier:
                                    {
                                        keyFrames.Add(new STBezierKeyFrame()
                                        {
                                            Frame = reader.ReadSingle(),
                                            Value = reader.ReadSingle(),
                                            SlopeIn = reader.ReadSingle(),
                                            SlopeOut = reader.ReadSingle(),
                                        });
                                    }
                                    break;
                                case InterpolationMode.Linear:
                                    {
                                        keyFrames.Add(new STKeyFrame()
                                        {
                                            Frame = reader.ReadSingle(),
                                            Value = reader.ReadSingle(),
                                        });
                                    }
                                    break;
                                default:
                                    throw new Exception($"Unsupported interpolation mode! track {j} " + track.interpolate_type);
                            }
                        }
                    }

                    currentGroup.TrackList.Add(new AnimTrack(currentGroup)
                    {
                        ConstantUnk = track.keyframe_count, //When constant used, value is used for something else?
                        KeyFrames = keyFrames,
                        TrackEffect = track.effect,
                        TrackMode = track.mode,
                        ValueIdx = track.valueIndex,
                        Unknown = track.unk,
                        Constant = track.Constant,
                        InterpolationType = ConvertType(track.interpolate_type),
                    });
                }

                foreach (var group in animationNodes)
                    anim.AnimGroups.Add(group.Value);
            }
        }

        private STInterpoaltionType ConvertType(InterpolationMode mode)
        {
            switch (mode)
            {
                case InterpolationMode.Linear: return STInterpoaltionType.Linear;
                case InterpolationMode.Bezier: return STInterpoaltionType.Bezier;
                case InterpolationMode.Step: return STInterpoaltionType.Step;
                case InterpolationMode.Constant: return STInterpoaltionType.Constant;
                case InterpolationMode.Bitmap: return STInterpoaltionType.Bitmap;
                default: return STInterpoaltionType.Constant;
            }
        }

        private InterpolationMode ConvertType(STInterpoaltionType mode)
        {
            switch (mode)
            {
                case STInterpoaltionType.Linear: return InterpolationMode.Linear;
                case STInterpoaltionType.Bezier: return InterpolationMode.Bezier;
                case STInterpoaltionType.Step: return InterpolationMode.Step;
                case STInterpoaltionType.Constant: return InterpolationMode.Constant;
                case STInterpoaltionType.Bitmap: return InterpolationMode.Bitmap;
                default: return InterpolationMode.Constant;
            }
        }


        public override void Write(FileWriter writer, HsfFile header)
        {
            //Turn our animation back into HSF structures
            List<MotionData> motionData = new List<MotionData>();
            foreach (var anim in Animations)
            {
                var motion = new MotionData()
                {
                    NameOffset = (uint)header.GetStringOffset(anim.Name),
                    MotionLength = anim.FrameCount,
                    TrackCount = anim.GetTrackCount(),
                    TrackDataOffset = 0,
                };
                writer.WriteStruct(motion);
            }

            long animStart = writer.Position;

            long trackStart = writer.Position;
            for (int i = 0; i < Animations.Count; i++) {
                writer.WriteUint32Offset(animStart + 12 + (i * 16), trackStart);

                var tracks = Animations[i].GetAllTracks();
                foreach (var track in tracks)
                {
                    string name = track.ParentGroup.Name;
                    var interpolation = ConvertType(track.InterpolationType);

                    writer.Write((byte)track.TrackMode);
                    writer.Write((byte)track.Unknown);
                    if (track.TrackMode ==  TrackMode.Normal || 
                        track.TrackMode == TrackMode.Material || 
                        track.TrackMode == TrackMode.Object)
                        writer.Write((short)header.GetStringOffset(name));
                    else
                        writer.Write(ushort.MaxValue);
                    writer.Write((short)track.ValueIdx);
                    writer.Write((short)track.TrackEffect);
                    writer.Write((short)interpolation);
                    if (track.InterpolationType != STInterpoaltionType.Constant)
                    {
                        writer.Write((short)track.KeyFrames.Count);
                        writer.Write(uint.MaxValue);
                    }
                    else
                    {
                        writer.Write((short)track.ConstantUnk);
                        writer.Write(track.Constant);
                    }
                }
            }

            long dataStart = writer.Position;
            int trackIndex = 0;
            for (int i = 0; i < Animations.Count; i++)
            {
                var anim = Animations[i];
                var tracks = anim.GetAllTracks();

                for (int j = 0; j < tracks.Count; j++)
                {
                    var track = tracks[j];

                    if (track.InterpolationType != STInterpoaltionType.Constant)
                    {
                        //Save the keyframe offset
                        if (track.KeyFrames.Count > 0)
                        {
                            writer.WriteUint32Offset(trackStart + 12 + (trackIndex * 16), dataStart);
                        }

                        for (int key = 0; key < track.KeyFrames.Count; key++)
                        {
                            var keyFrame = track.KeyFrames[key];
                            switch (track.InterpolationType)
                            {
                                //8 bytes
                                case STInterpoaltionType.Step:
                                    {
                                        writer.Write(keyFrame.Frame);
                                        writer.Write(keyFrame.Value);
                                    }
                                    break;
                                //8 bytes
                                case STInterpoaltionType.Bitmap:
                                    {
                                        writer.Write(keyFrame.Frame);
                                        writer.Write((int)keyFrame.Value);
                                    }
                                    break;
                                //16 bytes
                                case STInterpoaltionType.Bezier:
                                    {
                                        writer.Write(keyFrame.Frame);
                                        writer.Write(keyFrame.Value);
                                        writer.Write(((STBezierKeyFrame)keyFrame).SlopeIn);
                                        writer.Write(((STBezierKeyFrame)keyFrame).SlopeOut);
                                    }
                                    break;
                                default:
                                    throw new Exception("Unsupported interpolation mode! " + track.InterpolationType);
                            }
                        }
                    }
                  
                    trackIndex++;
                }
            }
        }
    }

}
