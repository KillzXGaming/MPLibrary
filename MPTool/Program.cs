using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPLibrary.GCN;

namespace MPTool
{
    class Program
    {
        static void Main(string[] args)
        {
            HsfFile hsf = new HsfFile("File_0.hsf");
            hsf.Save("new.hsf");
            Console.WriteLine("Saved HSF!");
            Console.Read();
        }
    }
}
