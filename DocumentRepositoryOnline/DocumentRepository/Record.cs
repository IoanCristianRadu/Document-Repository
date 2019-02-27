using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentRepositoryOnline.DocumentRepository
{
    public class Record
    {
        private String continut;
        private int part_number;
        private String last_modified;
        private String date_created;
        private int filesize;
        private String extension;
        private String author;
        private String title;
        private int pages;
        private String local_path;
        private int mostRelevantPage;
        private String relevantContent;
        public bool isCorrectPath;

        public string Continut
        {
            get
            {
                return continut;
            }

            set
            {
                continut = value;
            }
        }

        public int Part_number
        {
            get
            {
                return part_number;
            }

            set
            {
                part_number = value;
            }
        }

        public string Last_modified
        {
            get
            {
                return last_modified;
            }

            set
            {
                last_modified = value;
            }
        }

        public string Date_created
        {
            get
            {
                return date_created;
            }

            set
            {
                date_created = value;
            }
        }

        public int Filesize
        {
            get
            {
                return filesize;
            }

            set
            {
                filesize = value;
            }
        }

        public string Extension
        {
            get
            {
                return extension;
            }

            set
            {
                extension = value;
            }
        }

        public string Author
        {
            get
            {
                return author;
            }

            set
            {
                author = value;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }

            set
            {
                title = value;
            }
        }

        public int Pages
        {
            get
            {
                return pages;
            }

            set
            {
                pages = value;
            }
        }

        public string Local_path
        {
            get
            {
                return local_path;
            }

            set
            {
                local_path = value;
            }
        }

        public int MostRelevantPage
        {
            get
            {
                return mostRelevantPage;
            }

            set
            {
                mostRelevantPage = value;
            }
        }

        public string RelevantContent
        {
            get
            {
                return relevantContent;
            }

            set
            {
                relevantContent = value;
            }
        }

        public Record(string continut, int part_number, string last_modified, string date_created, int filesize, string extension, string author, string title, int pages, string local_path)
        {
            this.Continut = continut;
            this.Part_number = part_number;
            this.Last_modified = last_modified;
            this.Date_created = date_created;
            this.Filesize = filesize;
            this.Extension = extension;
            this.Author = author;
            this.Title = title;
            this.Pages = pages;
            this.Local_path = local_path;
            this.isCorrectPath = true;
        }

        public Record(string last_modified, string date_created, int filesize, string extension, string author, string title, int pages, string local_path)
        {
            this.Last_modified = last_modified;
            this.Date_created = date_created;
            this.Filesize = filesize;
            this.Extension = extension;
            this.Author = author;
            this.Title = title;
            this.Pages = pages;
            this.Local_path = local_path;
            this.isCorrectPath = true;
        }

        public Record(string last_modified, string date_created, int filesize, string extension, string author, string title, int pages)
        {
            this.Last_modified = last_modified;
            this.Date_created = date_created;
            this.Filesize = filesize;
            this.Extension = extension;
            this.Author = author;
            this.Title = title;
            this.Pages = pages;
            this.isCorrectPath = true;
        }
    }
}

