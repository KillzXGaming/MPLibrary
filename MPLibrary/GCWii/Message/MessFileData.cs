using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using Newtonsoft.Json;

namespace MPLibrary.GCN
{
    public class MessFileData
    {
        public List<MessageData> MessageFiles = new List<MessageData>();

        public MessFileData() { }

        public MessFileData(string fileName, Encoding encoding, uint version = 4) {
            Read(new FileReader(fileName), encoding, version);
        }

        public MessFileData(System.IO.Stream stream, Encoding encoding, uint version = 4) {
            Read(new FileReader(stream), encoding, version);
        }

        public void Save(string fileName, uint version = 4, Encoding encoding = null)
        {
            using (var  writer = new FileWriter(fileName)) {
                Write(writer, encoding == null ? Encoding.UTF8 : encoding, version);
            }
        }

        public void Save(System.IO.Stream stream, uint version = 4, Encoding encoding = null)
        {
            using (var writer = new FileWriter(stream)) {
                Write(writer, encoding == null ? Encoding.UTF8 : encoding, version);
            }
        }

        public static string Export(MessFileData message) {
            return JsonConvert.SerializeObject(message, Formatting.Indented);
        }

        public static MessFileData Import(string fileName) {
            return JsonConvert.DeserializeObject<MessFileData>(System.IO.File.ReadAllText(fileName));
        }

        void Read(FileReader reader, Encoding encoding, uint version = 4)
        {
            uint startPos = 0;
            if (version > 4)
                startPos = 4;

            reader.SetByteOrder(true);
            uint numFiles = reader.ReadUInt32();
            for (int i = 0; i < numFiles; i++)
            {
                reader.SeekBegin(4 + (i * 4));
                uint offset = reader.ReadUInt32() + startPos;
                uint nextOffset = reader.ReadUInt32() + startPos;
                if (i == numFiles - 1)
                    nextOffset = (uint)reader.BaseStream.Length;
                    
                uint size = nextOffset - offset;

                reader.SeekBegin(offset);
                byte[] data = reader.ReadBytes((int)size);
                MessageFiles.Add(ReadMessageData(data, encoding, version));
            }
        }

        void Write(FileWriter writer, Encoding encoding, uint version)
        {
            writer.SetByteOrder(true);
            writer.Write(MessageFiles.Count);
            writer.Write(new uint[MessageFiles.Count]);
            long sectionSizePos = writer.Position;
            if (version <= 5)
            {
                writer.Write(uint.MaxValue);
                for (int i = 0; i < MessageFiles.Count; i++)
                {
                    writer.WriteUint32Offset(4 + (i * 4));
                    WriteMessageData(writer, MessageFiles[i], encoding, version);
                }
                writer.WriteSectionSizeU32(sectionSizePos, writer.BaseStream.Length);
            }
            else
            {
                for (int i = 0; i < MessageFiles.Count; i++)
                {
                    writer.WriteUint32Offset(4 + (i * 4), 4);
                    WriteMessageData(writer, MessageFiles[i], encoding, version);
                }
            }
        }

        private void WriteMessageData(FileWriter writer, MessageData messData, Encoding encoding, uint version)
        {
            if (messData.Entries.Count == 0)
                writer.Write(messData.Data);
            else
            {
                if (version <= 5)
                {
                    long startPos = writer.Position;
                    writer.Write(messData.Entries.Count);
                    writer.Write(new uint[messData.Entries.Count]);
                    long sectionSizePos = writer.Position;
                    writer.Write(uint.MaxValue);
                    for (int i = 0; i < messData.Entries.Count; i++)
                    {
                        writer.WriteUint32Offset(startPos + 4 + (i * 4), startPos);
                        writer.Write((byte)0xB);
                        WriteString(writer, messData.Entries[i].Value);
                        writer.AlignBytes(4);
                    }

                    writer.WriteSectionSizeU32(sectionSizePos, writer.Position - startPos);
                }
                else
                {
                    long startPos = writer.Position;
                    writer.Write(messData.Entries.Count);
                    writer.Write(new uint[messData.Entries.Count]);
                    long sectionSizePos = writer.Position;
                    for (int i = 0; i < messData.Entries.Count; i++)
                    {
                        writer.WriteUint32Offset(startPos + 4 + (i * 4), startPos + 4);
                        writer.Write(((MessageEntryV2)messData.Entries[i]).ID);
                        WriteString(writer, messData.Entries[i].Value);
                        writer.AlignBytes(4);
                    }
                }
            }
        }

        private MessageData ReadMessageData(byte[] data, Encoding encoding, uint version = 4)
        {
            uint startPos = 0;
            if (version > 4)
                startPos = 4;

            MessageData messageData = new MessageData();
            messageData.Data = data;
            using (var reader = new FileReader(data))
            {
                reader.SetByteOrder(true);
                uint numValues = reader.ReadUInt32();
                uint firstOffset = reader.ReadUInt32() + startPos;

                   if (version >= 6)
                   {
                    if (firstOffset != 4 + (numValues * 4))
                           return messageData;
                   }
                   else
                   {
                       if (firstOffset != 8 + (numValues * 4))
                           return messageData;
                   }

                //Message files 

                for (int i = 0; i < numValues; i++)
                {
                    reader.SeekBegin(4 + (i * 4));
                    uint offset = reader.ReadUInt32() + startPos;
                    uint id = 0;

                    reader.SeekBegin(offset);
                    if (version >= 6)
                        id = reader.ReadUInt32();
                    else
                        reader.ReadByte(); //0xb at start of every string

                    string value = ReadString(reader, encoding);
                    if (version >= 6)
                        messageData.Entries.Add(new MessageEntryV2(id, value));
                    else
                        messageData.Entries.Add(new MessageEntry(value));
                }
                messageData.Data = new byte[0];
            }
            return messageData;
        }

        private static string ReadString(FileReader reader, Encoding encoding)
        {
            List<byte> values = new List<byte>();
            while (!reader.EndOfStream)
            {
                byte cha = reader.ReadByte();
                if (cha == 0x0)
                    break;
                values.Add(cha);
            }

            List<char> text = new List<char>();
            for (int i = 0; i < values.Count; i++)
            {
                var val = values[i];

                if (CharacterTable.ContainsKey(val))
                    text.Add(CharacterTable[val]);
                else if (val == 0x0C)
                {
                    int count = 1;
                    while (values[i + 1] == 0x0C) {
                        count++;
                        i++;
                    }
                    text.AddRange($"(Align_{count})");
                }
                else if (val == 0x0D)
                    text.AddRange("(Select2)");
                else if (val == 0x0F)
                    text.AddRange("(Select)");
                else if (val == 0x1C)
                {
                    text.AddRange($"(Dialog:[{(DialogCodes)values[i + 1]}])");
                    i++;
                }
                else if (val == 0x1E)
                {
                    //End of color tag
                    if (values[i + 1] == 8)
                        text.AddRange($")");
                    else
                        text.AddRange($"(COLOR:[{(ColorCodes)values[i+1]}]");

                    i++;
                }
                else if (val == 0x1F)
                {
                    text.AddRange($"(INSERT:[{(RuntimeCodes)values[i + 1]}])");
                    i++;
                }
                else if (val == 0x0E)
                {
                    text.AddRange($"(ICON:[{(IconCodes)values[i + 1]}])");
                    i++;
                }
                else
                    text.Add((char)val);
            }


            return new string(text.ToArray());
        }

        static bool colorTagActive = false;
        static bool startQuoteActive = false;
        private static void WriteString(FileWriter writer, string text)
        {
            List<byte> values = new List<byte>();
            for (int i = 0; i < text.Length; i++)
            {
                char cha = text[i];
                if (cha == '(')
                {
                    List<byte> specials = new List<byte>();
                    specials.AddRange(TryParseSpecial(text, ref i, "COLOR", typeof(ColorCodes)));
                    if (specials.Count == 0)
                        specials.AddRange(TryParseSpecial(text, ref i, "ICON", typeof(IconCodes)));
                    if (specials.Count == 0)
                        specials.AddRange(TryParseSpecial(text, ref i, "INSERT", typeof(RuntimeCodes)));
                    if (specials.Count == 0)
                        specials.AddRange(TryParseSpecial(text, ref i, "Dialog", typeof(DialogCodes)));

                    if (specials.Count == 0)
                        values.Add((byte)cha);
                    else
                        values.AddRange(specials);
                }
                else if (cha == ')')
                {
                    values.Add((byte)0x1E);
                    values.Add((byte)8);

                    colorTagActive = false;
                }
                else
                {
                    if (cha == '"' && startQuoteActive) {
                        values.Add((byte)0xC1);
                        startQuoteActive = false;
                    }
                    else if (cha == '"')
                    {
                        values.Add((byte)0xC0);
                        startQuoteActive = true;
                    }
                    else if (CharacterTable.Values.Any(x => x == cha)) {
                        var charCode = CharacterTable.FirstOrDefault(x => x.Value == cha).Key;
                        values.Add((byte)charCode);
                    }
                    else
                        values.Add((byte)cha);
                }
            }
            values.Add((byte)0);
            writer.Write(values.ToArray());
        }

        private static List<byte> TryParseSpecial(string text, ref int i, string type, Type enumType)
        {
            List<byte> values = new List<byte>();

            int sizeCheck = type.Length + 3; //Include (, :, and [
            if (text.Length > i + sizeCheck && text.Substring(i + 1, type.Length) == type)
            {
                //Go from the start of the tag and split at the end to get our value
                var valeText = text.Remove(0, i + sizeCheck).Split(']').FirstOrDefault();
                var valueCode = (byte)Enum.Parse(enumType, valeText);

                if (type == "Dialog")
                    values.Add(0x1C);
                if (type == "COLOR")
                    values.Add(0x1E);
                if (type == "INSERT")
                    values.Add(0x1F);
                if (type == "ICON")
                    values.Add(0x0E);

                if (type == "COLOR")
                    colorTagActive = true;

                values.Add(valueCode);

                i += (sizeCheck + valeText.Length);
                if (type != "COLOR")
                    i++;
            }
            else if (text.Length > i + 8 && text.Substring(i + 1, 7) == "Select2")
            {
                values.Add(0x0D);
                i += 8;
            }
            else if (text.Length > i + 7 && text.Substring(i + 1, 6) == "Select")
            {
                values.Add(0x0F);
                i += 7;
            }
            else if (text.Length > i + 9 && text.Substring(i + 1, 5) == "Align")
            {
                var numValues = int.Parse(text.Substring(i + 7, 1));
                byte[] valueCodes = new byte[numValues];
                for (int v = 0; v < numValues; v++)
                    valueCodes[v] = 0x0C;

                values.AddRange(valueCodes);
                i += 8;
            }

                return values;
        }

        static Dictionary<byte, char> CharacterTable = new Dictionary<byte, char>()
        {
            { 0x0A, '\n' },

            { 0x3D, '-' },

            { 0x10, ' ' },
            { 0x20, '\t' }, //Tabs for the start of some text

            { 0x1D, '*' }, //Bold characters

            { 0x5C, '\'' },

            { 0x3F, '/' },

            { 0x7E, '~' }, //Todo

            { 0x7B, ':' },
            { 0x82, ',' },
            { 0x84, '@' }, //Todo. Seems to be used in numbers so add a random character for now
            { 0x85, '.' },

            { 0xC0, '"' }, //start
            { 0xC1, '"' }, //end
            { 0xC2, '!' },
            { 0xC3, '?' },

            { 0xFF, '\r' },
        };

        enum RuntimeCodes : byte
        {
            Option1 = 0x01,
            Option2 = 0x02,
            Option3 = 0x03,
            Option4 = 0x04,
            Option5 = 0x05,
            Option6 = 0x06,
            Option7 = 0x07,
            Option8 = 0x08,
        }

        //Can control sound effects
        enum DialogCodes : byte
        {
            Toad_Normal = 0x01,
            Toad_Excite = 0x02,
            Toad_Disappoint = 0x03,
            Goomba_Normal = 0x04,
            Goomba_Excite = 0x05,
            Goomba_Disappoint = 0x06,
            Shyguy_Normal = 0x07,
            Shyguy_Excite = 0x08,
            Shyguy_Disappoint = 0x09,
            Boo_Normal = 0x0A,
            Boo_Excite = 0x0B,
            Boo_Disappoint = 0x0C,
            Koopa_Normal = 0x0D,
            Koopa_Excite = 0x0E,
            Koopa_Disappoint = 0x0F,
            Bowser = 0x10,
            KoopaKid = 0x11,
            Thwomp = 0x13,
            Whomp = 0x14,
        }

        enum IconCodes : byte
        {
            ControlStick = 0x01,
            A = 0x03,
            B = 0x04,
            X = 0x05,
            Y = 0x06,
            R = 0x07,
            L = 0x09,
            Z = 0x0C,
            Coin = 0x13,
        }

        enum ColorCodes : byte
        {
            BLUE = 0x02,
            RED = 0x03,
            GREEN = 0x05,
            YELLOW = 0x07,
            END = 0x08,
        }

        public enum FileListMP4
        {
            IndexTable = 0,
            CharacterList = 1,
            HiddenBlock = 2,
            BattleSpace = 3,
            BowserSpace = 4,
            WrapSpace = 5,
            ItemSpace = 6,
            Lottery = 7,
            BooHouse = 8,
            ItemList = 9,
            DiceRollMenu = 10,
            Toad_MerryGoGame = 11,
            Toad_SpaceRocketGame = 12,
            Toad_Star = 13,
            Toad_RollerCoaster = 14,
            Toad_HostBoardDialog = 15,
            Toad_ItemShop = 16,
            System = 17,
            MainMenu = 18,
            ItemInfo = 19,
            Goomba_Roulette = 20,
        }

        public class MessageData
        {
            public List<MessageEntry> Entries = new List<MessageEntry>();
            public byte[] Data;
        }

        public class MessageEntryV2 : MessageEntry
        {
            public uint ID { get; set; }

            public MessageEntryV2(uint id, string value) : base(value)
            {
                Value = value;
                ID = id;
            }
        }

        public class MessageEntry
        {
            public string Value { get; set; }

            public MessageEntry(string value)
            {
                Value = value;
            }
        }
    }
}
