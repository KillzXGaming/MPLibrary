using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.IO;
using System.Xml;

namespace MPLibrary
{
    public class BinaryXML
    {
        public string Text;

        public BinaryXML(string fileName) {
            Read(new FileReader(fileName));
        }

        public BinaryXML(System.IO.Stream stream) {
            Read(new FileReader(stream));
        }

        public void Read(FileReader reader)
        {
            reader.SetByteOrder(true);
            ushort magic = reader.ReadUInt16();
            Text = ReadElements(reader);
            Text = PrintXML(Text);
            Console.WriteLine($"{Text}");
        }

        enum DataType
        {
            Uint32 = 0x4C,
            Uint16 = 0x53,
        }

        private string ReadElements(FileReader reader)
        {
            ushort flags = reader.ReadUInt16();
            DataType dataType = (DataType)(flags & 0xFF);

            ushort numRootElements = reader.ReadUInt16();
            ushort numTotalElements = reader.ReadUInt16();
            uint numElements = ReadValue(reader, dataType);

            uint pos = 0x0A;
            if (dataType == DataType.Uint32)
                pos = 0x0C;

            uint firstOffset = 0;
            using (reader.TemporarySeek(pos, System.IO.SeekOrigin.Begin)) {
                firstOffset = ReadValue(reader, dataType); 
            }

            string text = "";

            bool firstLine = true; ;

            Stack<string> TagStack = new Stack<string>();
            reader.SeekBegin(pos);
            while (!reader.EndOfStream)
            {
                string elementLine = "";
                string elementEnd = "";
                string elementStart = GetString(reader, ReadValue(reader, dataType));
                string elementValue = GetString(reader, ReadValue(reader, dataType));

                TagStack.Push(elementStart);

                if (reader.Position >= firstOffset)
                {
                    var endElements = TagStack.ToArray();
                    foreach (var item in endElements)
                    {
                        elementEnd = TagStack.Pop();
                        if (TagStack.Count > 0)
                            text += $"</{elementEnd}>";
                    }
                    break;
                }

                uint numItems = ReadValue(reader, dataType);

                for (int i = 0; i < numItems; i++)
                {
                    string name = GetString(reader, ReadValue(reader, dataType));
                    string elemValue = GetString(reader, ReadValue(reader, dataType));
                    elementLine += $" {name}=" + '"' + elemValue + '"';
                }

                //Go through all the max value offsets
                //These represent end tags which will pop our element stack
                bool firstPass = true;
                while (true)
                {
                    if (!IsEndElement(reader, dataType))
                    {
                        //Go back to read the proper offset
                        if (dataType == DataType.Uint32)
                            reader.Seek(-4);
                        else
                            reader.Seek(-2);

                        if (firstPass) //If first pass, write the element
                            elementLine = $"<{elementStart}{elementValue}{elementLine}>";
                        break;
                    }
                    else
                    {
                        elementEnd = TagStack.Pop();

                        if (firstLine) //Quick hack to fix the first line element
                            elementLine = $"<{elementStart}{elementLine}?> ";
                        else if (firstPass) //First pass write the end element on the same line
                            elementLine = $"<{elementStart}{elementLine}>{elementValue}</{elementEnd}>";
                        else //Start a new line for extra passes
                            elementLine += $"\n</{elementEnd}>";
                    }
                    firstPass = false;
                }

                text += $"{elementLine}\n";

                firstLine = false;
            }

            return text;
        }

        private bool IsEndElement(FileReader reader, DataType type)
        {
            if (type == DataType.Uint32)
                return reader.ReadUInt32() == uint.MaxValue;
            else
                return reader.ReadUInt16() == ushort.MaxValue;
        }

        private static uint ReadValue(FileReader reader, DataType tyoe)
        {
            if (tyoe == DataType.Uint32)
                return reader.ReadUInt32();
            else
                return reader.ReadUInt16();
        }

        private string GetString(FileReader reader, uint offset)
        {
            if (offset > reader.BaseStream.Length)
                return "";

            using (reader.TemporarySeek(offset, System.IO.SeekOrigin.Begin)) {
                return reader.ReadZeroTerminatedString();
            }
        }

        public void Save(Stream stream)
        {

        }

        public static string PrintXML(string xml)
        {
            string result = "";

            MemoryStream mStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode);
            XmlDocument document = new XmlDocument();

            try
            {
                // Load the XmlDocument with the XML.
                document.LoadXml(xml);

                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;

                // Write the XML into a formatting XmlTextWriter
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new StreamReader(mStream);

                // Extract the text from the StreamReader.
                string formattedXml = sReader.ReadToEnd();

                result = formattedXml;
            }
            catch (XmlException)
            {
                // Handle the exception
            }

            mStream.Close();
        //    writer.Close();

            return result;
        }
    }
}
