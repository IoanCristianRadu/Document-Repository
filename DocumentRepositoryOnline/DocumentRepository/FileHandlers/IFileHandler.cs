using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace DocumentRepositoryOnline.DocumentRepository.FileHandlers
{
    interface IFileHandler
    {
        void ExtractContent();
        void WriteToDb(IDbFileWriter dbFileWriter);
        FileDetails getFileDetails();
    }
}
