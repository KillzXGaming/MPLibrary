using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPLibrary.Switch.SMP
{
    public class Board
    {
        public List<SpaceNode> Spaces = new List<SpaceNode>();

        public MapParams(byte[] data)
        {
            Spaces.Clear();
            using (var reader = new StreamReader(
                new MemoryStream(data), Encoding.GetEncoding(932)))
            {
                while (true)
                {
                    //Skip first line
                    if (Spaces.Count == 0)
                        reader.ReadLine();

                    if (reader.EndOfStream)
                        break;

                    Spaces.Add(new SpaceNode(reader.ReadLine()));
                }
            }
        }
    }
}
