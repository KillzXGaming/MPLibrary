using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolbox.Library
{
    public class FileManager
    {
        public FileManager()
        {

        }

        public static string GetSourcePath(IFileFormat fileFormat)
        {
            var info = fileFormat.IFileInfo;
            if (info != null && info.ArchiveParent != null)
                return GetSourcePath((IFileFormat)info.ArchiveParent);

            return fileFormat.FilePath;
        }

        private static void LoadCompressionFormats(Type[] Types, List<ICompressionFormat> Formats)
        {
            foreach (Type type in Types)
            {
                Type[] interfaces_array = type.GetInterfaces();
                for (int i = 0; i < interfaces_array.Length; i++)
                {
                    if (interfaces_array[i] == typeof(ICompressionFormat))
                    {
                        Formats.Add((ICompressionFormat)Activator.CreateInstance(type));
                    }
                }
            }
        }

        private static void LoadFileFormats(Type[] Types, List<IFileFormat> Formats)
        {
            foreach (Type type in Types)
            {
                Type[] interfaces_array = type.GetInterfaces();
                for (int i = 0; i < interfaces_array.Length; i++)
                {
                    if (interfaces_array[i] == typeof(IFileFormat))
                    {
                        Formats.Add((IFileFormat)Activator.CreateInstance(type));
                    }
                }
            }
        }
    }
}
