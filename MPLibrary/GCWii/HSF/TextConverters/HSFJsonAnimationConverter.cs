using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Toolbox.Core.Animations;
using System.IO;

namespace MPLibrary.GCN
{
    public class HSFJsonAnimationConverter
    {
        public class HSFMotionDataText
        {
            public string Name { get; set; }
            public float FrameCount { get; set; }

            public List<HSFMotionGroupText> Groups = new List<HSFMotionGroupText>();

            public HSFMotionDataText(HSFMotionAnimation anim)
            {
                Name = anim.Name;
                FrameCount = anim.FrameCount;
                foreach (AnimationNode group in anim.AnimGroups)
                    Groups.Add(new HSFMotionGroupText(group));
            }
        }

        public class HSFMotionGroupText
        {
            public string Name { get; set; }
            public int ValueIndex { get; set; }

            public TrackMode Mode { get; set; }

            public List<HSFMotionTrackText> Tracks = new List<HSFMotionTrackText>();

            public HSFMotionGroupText(AnimationNode group)
            {
                Name = group.Name;
                ValueIndex = group.ValueIndex;
                Mode = group.Mode;

                foreach (AnimTrack track in group.GetTracks())
                    Tracks.Add(new HSFMotionTrackText(track));
            }
        }

        public class HSFMotionTrackText
        {
            public TrackEffect Effect { get; set; }
            public byte UnknownValue { get; set; }
            public STInterpoaltionType Interpolation { get; set; }
            public float ConstantValue { get; set; }
            public short ConstantFlags { get; set; }

            public List<HSFMotionKeyFrameText> KeyFrames = new List<HSFMotionKeyFrameText>();

            public HSFMotionTrackText(AnimTrack track)
            {
                Effect = track.TrackEffect;
                UnknownValue = track.Unknown;
                ConstantValue = track.Constant;
                Interpolation = track.InterpolationType;

                if (track.InterpolationType == STInterpoaltionType.Constant)
                {
                    ConstantFlags = track.ConstantUnk;
                }

                foreach (var key in track.KeyFrames)
                {
                    switch (track.InterpolationType)
                    {
                        case STInterpoaltionType.Bezier:
                            KeyFrames.Add(new HSFMotionBeizerKeyFrameText()
                            {
                                Value = key.Value,
                                Frame = key.Frame,
                                SlopeIn = ((STBezierKeyFrame)key).SlopeIn,
                                SlopeOut = ((STBezierKeyFrame)key).SlopeOut,
                            });
                            break;
                        default:
                            KeyFrames.Add(new HSFMotionKeyFrameText()
                            {
                                Value = key.Value,
                                Frame = key.Frame,
                            });
                            break;
                    }
                }
            }
        }

        public class HSFMotionKeyFrameText
        {
            public float Frame { get; set; }
            public float Value { get; set; }
        }

        public class HSFMotionBeizerKeyFrameText : HSFMotionKeyFrameText
        {
            public float SlopeIn { get; set; }
            public float SlopeOut { get; set; }
        }

        private static HSFMotionAnimation TextConverter(string text)
        {
            HSFMotionAnimation anim = new HSFMotionAnimation();

            StringReader sr = new StringReader(text);
            using (JsonReader reader = new JsonTextReader(sr))
            {
                AnimationNode group = null;
                while (reader.Read())
                {
                    if (reader.Value == null)
                        continue;

                    if (reader.Value.Equals("Name"))
                        anim.Name = reader.ReadAsString();
                    else if (reader.Value.Equals("FrameCount"))
                        anim.FrameCount = (float)reader.ReadAsDecimal();
                    else if (reader.Value.Equals("Group")) {
                        group = new AnimationNode();
                        anim.AnimGroups.Add(group);

                        string name = reader.ReadAsString();
                        reader.Read(); //ID
                        group.ValueIndex = (short)reader.ReadAsInt32(); //index
                        reader.Read(); //mode
                        group.Mode = (TrackMode)reader.ReadAsInt32();

                        if (group.Mode == TrackMode.Normal || group.Mode == TrackMode.Object)
                            group.Name = name;
                        else
                            group.Name = "";
                    }
                    else if (reader.Value.Equals("Track_Constant"))
                    {
                        string effect = reader.ReadAsString();
                        reader.Read(); //Value
                        float value = (float)reader.ReadAsDecimal();
                        reader.Read(); //Flags
                        string flags = reader.ReadAsString().Replace("0x", string.Empty);

                        AnimTrack track = new AnimTrack(group);
                        track.ValueIdx = group.ValueIndex;
                        track.TrackMode = group.Mode;
                        track.TrackEffect = ParseEffect(group.Mode, effect);
                        track.Constant = value;
                        track.ConstantUnk = short.Parse(flags, System.Globalization.NumberStyles.HexNumber);
                        track.InterpolationType = STInterpoaltionType.Constant;
                        track.KeyFrames = new List<STKeyFrame>();
                        track.Unknown = 0;
                        group.TrackList.Add(track);
                    }
                    else if (reader.Value.Equals("Track_Bezier"))
                        group.TrackList.Add(ParseAnimTrack(reader, group, STInterpoaltionType.Bezier));
                    else if (reader.Value.Equals("Track_Linear"))
                        group.TrackList.Add(ParseAnimTrack(reader, group, STInterpoaltionType.Linear));
                    else if (reader.Value.Equals("Track_Bezier"))
                        group.TrackList.Add(ParseAnimTrack(reader, group, STInterpoaltionType.Bezier));
                    else if (reader.Value.Equals("Track_Bitmap"))
                        group.TrackList.Add(ParseAnimTrack(reader, group, STInterpoaltionType.Bitmap));
                    else if (reader.Value.Equals("Track_Step"))
                        group.TrackList.Add(ParseAnimTrack(reader, group, STInterpoaltionType.Step));
                }
            }

            return anim;
        }

        private static AnimTrack ParseAnimTrack(JsonReader reader,AnimationNode group, STInterpoaltionType type)
        {
            string effect = reader.ReadAsString();

            AnimTrack track = new AnimTrack(group);
            track.ValueIdx = group.ValueIndex;
            track.TrackMode = group.Mode;
            track.TrackEffect = ParseEffect(group.Mode, effect);
            track.InterpolationType = type;
            track.KeyFrames = new List<STKeyFrame>();
            track.Unknown = 0;
            reader.Read();
            track.KeyFrames = ParseKeyFrames(reader, type);
            return track;
        }

        private static List<STKeyFrame> ParseKeyFrames(JsonReader reader, STInterpoaltionType type)
        {
            if (type == STInterpoaltionType.Constant)
                return new List<STKeyFrame>();

            List<STKeyFrame> keyFrames = new List<STKeyFrame>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.Value == null)
                    continue;

                if (reader.Value.Equals("Frame"))
                {
                    if (type == STInterpoaltionType.Bezier)
                    {
                        STBezierKeyFrame keyFrame = new STBezierKeyFrame();
                        keyFrame.Frame = (float)reader.ReadAsDecimal();
                        reader.Read(); //Value
                        keyFrame.Value = (float)reader.ReadAsDecimal();
                        reader.Read(); //in
                        keyFrame.SlopeIn = (float)reader.ReadAsDecimal();
                        reader.Read(); //out
                        keyFrame.SlopeOut = (float)reader.ReadAsDecimal();
                        keyFrames.Add(keyFrame);
                    }
                    else
                    {
                        STKeyFrame keyFrame = new STKeyFrame();
                        keyFrame.Frame = (float)reader.ReadAsDecimal();
                        reader.Read(); //Value
                        keyFrame.Value = (float)reader.ReadAsDecimal();
                        keyFrames.Add(keyFrame);
                    }
                }
            }
            return keyFrames;
        }

        private static TrackEffect ParseEffect(TrackMode mode, string value)
        {
          /*  if (mode == TrackMode.Attriubute)
                return (TrackEffect)Enum.Parse(typeof(MaterialTrackEffect), value);
            if (mode == TrackMode.Material)
                return (TrackEffect)Enum.Parse(typeof(MaterialTrackEffect), value);
           */
                return (TrackEffect)Enum.Parse(typeof(TrackEffect), value);
        }

        private static string TextConverter(HSFMotionAnimation anim)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("Name");
                writer.WriteValue(anim.Name);
                writer.WritePropertyName("FrameCount");
                writer.WriteValue(anim.FrameCount);
                writer.WritePropertyName("Groups");
                writer.WriteStartObject();
                foreach (AnimationNode group in anim.AnimGroups)
                {
                    writer.WritePropertyName("Group");
                    writer.WriteValue(group.Name);

                    writer.Formatting = Formatting.None;

                    writer.WritePropertyName("Id");
                    writer.WriteValue(group.ValueIndex);

                    writer.WritePropertyName("Mode");
                    writer.WriteValue(group.Mode);
                    writer.WritePropertyName("Tracks");
                    writer.WriteStartObject();

                    writer.Formatting = Formatting.Indented;

                    var tracks = group.GetTracks().OrderBy(x => x.InterpolationType);

                    foreach (AnimTrack track in tracks)
                    {
                        writer.WritePropertyName($"Track_{track.InterpolationType}");
                        switch (track.TrackMode)
                        {
                            //case TrackMode.Material:
                             //   writer.WriteValue($"{(MaterialTrackEffect)track.TrackEffect}");
                              //  break;
                               // writer.WriteValue($"{(AttributeTrackEffect)track.TrackEffect}");
                                //break;
                            default:
                                writer.WriteValue($"{track.TrackEffect}");
                                break;
                        }

                        //  writer.WritePropertyName("Unk");
                        //   writer.WriteValue(track.Unknown);

                        writer.Formatting = Formatting.None;

                        if (track.InterpolationType == STInterpoaltionType.Constant)
                        {
                            writer.WritePropertyName("Value");
                            writer.WriteValue(track.Constant);

                            writer.WritePropertyName("Flags");
                            writer.WriteValue($"0x{track.ConstantUnk.ToString("X")}");
                        }

                        writer.Formatting = Formatting.Indented;

                        if (track.HasKeys)
                        {
                            writer.WritePropertyName("KeyFrames");
                            writer.WriteStartObject();
                        }

                        foreach (var key in track.KeyFrames)
                        {
                            switch (track.InterpolationType)
                            {
                                case STInterpoaltionType.Bezier:
                                    writer.WritePropertyName("Frame");
                                    writer.WriteValue(key.Frame);

                                    writer.Formatting = Formatting.None;

                                    writer.WritePropertyName("Value");
                                    writer.WriteValue((float)Math.Round(key.Value, 5));

                                    writer.WritePropertyName("in");
                                    writer.WriteValue((float)Math.Round(((STBezierKeyFrame)key).SlopeIn, 5));

                                    writer.WritePropertyName("out");
                                    writer.WriteValue((float)Math.Round(((STBezierKeyFrame)key).SlopeOut, 5));

                                    writer.Formatting = Formatting.Indented;

                                    break;
                                default:
                                    writer.WritePropertyName("Frame");
                                    writer.WriteValue(key.Frame);

                                    writer.Formatting = Formatting.None;

                                    writer.WritePropertyName("Value");
                                    writer.WriteValue(key.Value);

                                    writer.Formatting = Formatting.Indented;
                                    break;
                            }
                        }

                        if (track.HasKeys)
                            writer.WriteEndObject();
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            return sb.ToString();
        }

        public static string Export(HSFMotionAnimation anim) {
            return TextConverter(anim);
        }

        public static HSFMotionAnimation ImportFromFile(string fileName) {
            return TextConverter(System.IO.File.ReadAllText(fileName));
        }

        public static HSFMotionAnimation Import(string text) {
            return TextConverter(text);
        }
    }
}
