namespace DocumentRepositoryOnline.DocumentRepository.FileHandlers.DbFileWriters
{
    public interface IDbFileWriter
    {
        void WriteToDb(FileDetails fileData);
    }
}
