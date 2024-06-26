﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.Collada;
using Toolbox.Core.IO;
using MPLibrary.GCN;
using MPLibrary;
using System.Reflection;

namespace HSFConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var arg in args)
            {
                if (Directory.Exists(arg))
                {
                    string folder = Path.GetFileName(arg);
                    string daeFile = $"{arg}/{folder}.dae";
                    var importModel = (DaeFile)STFileLoader.OpenFileFormat(daeFile);

                    HsfFile hsf = new HsfFile();
                    HSFModelImporter.Import(arg, hsf, new HSFModelImporter.ImportSettings());
                    hsf.Save($"{folder}.new.hsf");
                }
                else if (File.Exists(arg))
                {
                    string folder = Path.GetFileNameWithoutExtension(arg);
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    var file = STFileLoader.OpenFileFormat(arg);
                    Console.WriteLine($"file {file != null}");
                    DAE.Export($"{folder}/{folder}.dae", new DAE.ExportSettings()
                    {
                        ImageFolder = folder,
                    }, (IModelFormat)file);
                }
            }
        }
    }
}
