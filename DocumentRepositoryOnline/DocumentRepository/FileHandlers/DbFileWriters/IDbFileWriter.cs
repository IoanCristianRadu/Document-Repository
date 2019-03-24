using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace DocumentRepositoryOnline.DocumentRepository.FileHandlers
{
    public interface IDbFileWriter
    {
        void WriteToDb(FileDetails fileData);
    }
}
