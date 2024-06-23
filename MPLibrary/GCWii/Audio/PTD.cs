using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using static Toolbox.Core.WiiU.GX2;

namespace MPLibrary.GCWii.Audio
{
    public class PtdFile
    {
        public List<DspFile> Files =  new List<DspFile>();

        public ushort Version = 1;
        public uint Unknown2 = 2;
        public uint SampleRate = 32000;
        public uint ChannelCount = 2;

        public PtdFile() { }

        public PtdFile(Stream stream)
        {
            using (var reader = new FileReader(stream)) {
                Read(reader);
            }
        }

        public void Save(Stream stream)
        {
            using (var writer = new FileWriter(stream)) {
                Write(writer);
            }
        }

        private void Read(FileReader reader)
        {
            reader.SetByteOrder(true);

            Version = reader.ReadUInt16();
            ushort numFiles = reader.ReadUInt16();
            Unknown2 = reader.ReadUInt32();
            SampleRate = reader.ReadUInt32();
            ChannelCount = reader.ReadUInt32();
            uint entry_offsets = reader.ReadUInt32();
            uint coef_offset = reader.ReadUInt32();
            uint header_offset = reader.ReadUInt32();
            uint stream_offset = reader.ReadUInt32();

            reader.SeekBegin(entry_offsets);
            uint[] offset_list = reader.ReadUInt32s(numFiles);

            for (int i = 0; i < numFiles; i++)
            {
                DspFile file = new DspFile();
                Files.Add(file);

                if (offset_list[i] == 0)
                    continue;

                file.Channels.Add(new Channel());

                reader.SeekBegin(offset_list[i]);
                file.Flags = reader.ReadUInt32();
                file.SampleRate = reader.ReadUInt32();
                file.NibbleCount = reader.ReadUInt32();
                file.LoopStart = reader.ReadUInt32();
                uint channel_1_Stream_Offset = reader.ReadUInt32();
                ushort channel_1_Coeff_Idx = reader.ReadUInt16();
                file.Channels[0].Unknown = reader.ReadUInt16();

                if ((file.Flags & 0x01000000) != 0)
                {
                    file.Channels.Add(new Channel());

                    uint channel_2_Stream_Offset = reader.ReadUInt32();
                    uint channel_2_Coeff_Idx = reader.ReadUInt16();
                    file.Channels[1].Unknown = reader.ReadUInt16();

                    if (file.Flags == 23871488)
                    {
                        file.UnknownData = reader.ReadBytes(144);
                    }

                    reader.SeekBegin(coef_offset + channel_2_Coeff_Idx * 0x20);
                    file.Channels[1].Coeff = reader.ReadUInt16s(16);

                    file.Channels[1].StartAddress = channel_2_Stream_Offset - channel_1_Stream_Offset;
                }


                reader.SeekBegin(channel_1_Stream_Offset);
                file.Data = reader.ReadBytes((int)file.NibbleCount);

                file.Channels[0].StartAddress = channel_1_Stream_Offset - stream_offset;

                reader.SeekBegin(coef_offset + channel_1_Coeff_Idx * 0x20);
                file.Channels[0].Coeff = reader.ReadUInt16s(16);
            }
        }

        private void Write(FileWriter writer)
        {
            writer.SetByteOrder(true);
            writer.Write(Version);
            writer.Write((ushort)Files.Count);
            writer.Write(Unknown2);
            writer.Write(SampleRate);
            writer.Write(ChannelCount);

            writer.Write(0); //entry_offsets
            writer.Write(0); //coef_offset
            writer.Write(0); //header_offset
            writer.Write(0); //stream_offset

            writer.WriteUint32Offset(16);

            long offset_pos = writer.Position;
            writer.Write(new uint[Files.Count]); //offsets for later

            //coef
            writer.AlignBytes(16);
            writer.WriteUint32Offset(20);
            foreach (var f in Files)
            {
                foreach (var chan in f.Channels)
                  writer.Write(chan.Coeff);
            }

            writer.AlignBytes(16);
            writer.WriteUint32Offset(24);

            int coefIdx = 0;
            uint streamOffset = (uint)writer.Position;

            for (int i = 0; i < Files.Count; i++)
            {
                if (Files[i].Channels.Count > 0)
                    streamOffset += 24;
                if (Files[i].Channels.Count > 1)
                    streamOffset += 8;

                if (Files[i].Flags == 23871488)
                    streamOffset += 144;
            }

            uint alignment = 8;

            streamOffset += (uint)(-streamOffset % alignment + alignment) % alignment;
            streamOffset += 16;

            for (int i = 0; i < Files.Count; i++)
            {
                if (Files[i].Channels.Count == 0)
                    continue;

                var f = Files[i];

                writer.WriteUint32Offset(offset_pos + (i * 4));

                writer.Write(f.Flags);
                writer.Write(f.SampleRate);
                writer.Write(f.NibbleCount);
                writer.Write(f.LoopStart);
                writer.Write(streamOffset); //channel 1 address
                writer.Write((ushort)coefIdx++); //channel 1 coef idx
                writer.Write((ushort)f.Channels[0].Unknown);

                if (f.Channels.Count > 0)
                {
                    var streamOffset_2 = streamOffset +(f.NibbleCount / 2);
                    //alignment
                    streamOffset_2 += (uint)(-streamOffset_2 % 4 + 4) % 4;

                    writer.Write(streamOffset_2); //channel 2 address
                    writer.Write((ushort)coefIdx++); //channel 2 coef idx
                    writer.Write((ushort)f.Channels[1].Unknown);

                    if (f.Flags == 23871488)
                    {
                        writer.Write(f.UnknownData);
                    }
                }

                streamOffset += (uint)f.Data.Length;
                //alignment
                streamOffset += (uint)(-streamOffset % alignment + alignment) % alignment;
            }

            writer.AlignBytes(16);
            writer.Write(new byte[16]);

            writer.WriteUint32Offset(28);

            for (int i = 0; i < Files.Count; i++)
            {
                if (Files[i].Channels.Count == 0)
                    continue;

                writer.AlignBytes(8);
                writer.Write(Files[i].Data);
            }
        }

        public class DspFile
        {
            public uint Flags;

            public uint NibbleCount;

            public uint SampleRate = 32000;

            public uint LoopStart;

            public byte[] UnknownData;

            public  byte[] Data;

            public List<Channel> Channels = new List<Channel>();

            public byte[] CreateDSP(int channel = 0)
            {
                if (Channels.Count == 0) return new byte[0]; //empty file

                int loopFlag = 0;
                if ((Flags & 0x02000000) != 0) loopFlag = 1;

                var mem = new MemoryStream();
                using (var writer = new FileWriter(mem))
                {
                    writer.SetByteOrder(true);
                    writer.Write(nibblesToSamples(NibbleCount)); //sample_count
                    writer.Write(NibbleCount); 
                    writer.Write(SampleRate);
                    writer.Write((ushort)loopFlag);
                    writer.Write((ushort)0); //format always 0 ADPCM
                    writer.Write(LoopStart); //loop_start_offset
                    writer.Write(NibbleCount - 1); //loop_end_offset
                    writer.Write(0); //initial_offset

                    for (int j = 0; j < 16; j++)
                        writer.Write((ushort)Channels[channel].Coeff[j]);

                    writer.Write((ushort)0); //gain (0 for ADPCM)

                    writer.Write((ushort)Data[0]); //initial_ps
                    writer.Write((ushort)0); //initial_hist1
                    writer.Write((ushort)0); //initial_hist2
                    writer.Write((ushort)0); //loop_ps
                    writer.Write((ushort)0); //loop_hist1
                    writer.Write((ushort)0); //loop_hist2

                    for (int j = 0; j < 11; j++) //padding
                        writer.Write((ushort)0);

                    for (int j = 0; j < NibbleCount; j++)
                        writer.Write(Data[j]);
                }
                return mem.ToArray();
            }

            uint nibblesToSamples(uint nibbles)
            {
                var whole_frames = nibbles / 16;
                var remainder = nibbles % 16;

                if (remainder > 0) return whole_frames * 14 + remainder - 2;
                else return whole_frames * 14;
            }
        }

        public class Channel
        {
            public ushort[] Coeff = new ushort[16];

            public ushort Unknown;

            public uint StartAddress;
        }
    }
}
