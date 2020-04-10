using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace MPLibrary.MP10
{
    public class MP10Board
    {
        public static MP10BoardParams ParseBoard(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(MP10BoardParams));
            using (var reader = new StringReader(xml)) {
                return (MP10BoardParams)serializer.Deserialize(reader);
            }
        }
    }

    [Serializable, XmlRoot("root")]
    public class MP10BoardParams
    {
        [XmlElement]
        public XmlFile XmlFile;
        [XmlElement]
        public Version Version;
    ///   [XmlElement]
    //    public MasuDataList MasuDataList;

        public MasuData[] MasuDataList;
    }

    public class XmlFile
    {
        [XmlText]
        public string Value;
    }

    public class Version
    {
        [XmlText]
        public float Value;
    }

    public class MasuDataList : ItemCollection
    {

    }

    public class MasuData
    {
        [XmlElement("No")]
        public int ID;

        [XmlElement("Area")]
        public int Area;

        [XmlElement("NodeName")]
        public string Name;

        [XmlElement("MasuName")]
        public string Type;

        [XmlElement("Param")]
        public int Param;

        [XmlElement("Uncountble")]
        public int Uncountble;

        [XmlElement("OneWay")]
        public int OneWay;

        [XmlElement("JumpStart")]
        public int JumpStart;

        [XmlElement("JumpEnd")]
        public int JumpEnd;

        [XmlElement("PunishNotReturn")]
        public int PunishNotReturn;

        [XmlElement("NextNoList")]
        public NextNoList NextSpaceList;

        [XmlElement("PrevNoList")]
        public PrevNoList PrevSpaceList;

        [XmlElement]
        public Vector3XML Position;

        [XmlElement]
        public Vector4XML Quaternion;
    }

    public class NextNoList : ItemCollection
    {
        [XmlArray("NextNo")]
        public PathLink[] NextSpaces;
    }

    public class PrevNoList : ItemCollection
    {
        [XmlArray("PrevNo")]
        public PathLink[] PreviusSpaces;
    }

    public class PathLink
    {
        [XmlAttribute]
        public int Index;

        [XmlText]
        public int Value;
    }

    public class EventNameList : ItemCollection
    {
        [XmlArray("PrevNo")]
        public PathLink[] PreviusSpaces;
    }

    public class EventName : ItemCollection
    {
        [XmlAttribute]
        public int Index;

        [XmlText]
        public string MoveGesso;
    }

    public class ItemCollection
    {
        [XmlAttribute]
        public int Size;
    }

    public class Vector3XML
    {
        [XmlElement]
        public float X;

        [XmlElement]
        public float Y;

        [XmlElement]
        public float Z;
    }

    public class Vector4XML
    {
        [XmlElement]
        public float X;

        [XmlElement]
        public float Y;

        [XmlElement]
        public float Z;

        [XmlElement]
        public float W;
    }
}
