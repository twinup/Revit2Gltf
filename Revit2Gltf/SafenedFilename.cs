using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Revit2Gltf
{
    /// <summary>
    /// Removes whitespace and special characters
    /// from a filename to make it safe for upload and
    /// access in storage
    /// </summary>
    public class SafenedFilename
    {

        private static readonly Regex sWhitespace = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex sSpecialChars = new Regex("[^a-zA-Z0-9_.]+", RegexOptions.Compiled);

        public readonly string FileLocation;
        public readonly string SafeFileName;

        public SafenedFilename(string path)
        {
            FileLocation = path;
            SafeFileName = RemoveSpecialCharacters(Path.GetFileNameWithoutExtension(path), Path.GetExtension(path));
        }

        static string RemoveSpecialCharacters(string fileName, string ext)
        {
            string output;
            output = sSpecialChars.Replace(fileName, "");
            output = sWhitespace.Replace(output, "");
            output = Path.GetFileNameWithoutExtension(output);
            output = output.Replace(".", "_") + ext;
            return output;
        }
    }
}
