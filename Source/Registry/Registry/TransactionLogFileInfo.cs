using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registry
{
   public class TransactionLogFileInfo
    {
        public TransactionLogFileInfo(string fileName, byte[] fileBytes)
        {
            FileName = fileName;
            FileBytes = fileBytes;
        }

        public string FileName { get; }
        public byte[] FileBytes { get; }


    }
}
