using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RootFinder.Data
{
    public class FileProp
    {
        internal string FileName { get; set; }
        internal bool IsPassingFile { get; set; }

        public FileProp(string fileName, bool isPassingFile)
        {
            FileName = fileName;
            IsPassingFile = isPassingFile;
        }
    }
}
