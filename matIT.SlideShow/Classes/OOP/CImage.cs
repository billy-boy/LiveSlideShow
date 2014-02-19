using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace matIT.SlideShow
{
    public class CImage
    {
        private String _filePath;
        private int _fileId;
        private bool _viewed = false;
        private DateTime _viewedDate;
        private bool _imported = false;
        private DateTime _importedDate;
        private bool _uploaded = false;
        private DateTime _uploadDate;

        public CImage()
        {

        }

        public String FilePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }

        public int FileId
        {
            get { return _fileId; }
            set { _fileId = value; }
        }

        public bool Viewed
        {
            get { return _viewed; }
            set { _viewed = value; }
        }

        public DateTime ViewedDate
        {
            get { return _viewedDate; }
            set { _viewedDate = value; }
        }

        public bool Imported
        {
            get { return _imported; }
            set { _imported = value; }
        }

        public DateTime ImportedDate
        {
            get { return _importedDate; }
            set { _importedDate = value; }
        }

        public bool Uploaded
        {
            get { return _uploaded; }
            set { _uploaded = value; }
        }

        public DateTime UploadedDate
        {
            get { return _uploadDate; }
            set { _uploadDate = value; }
        }
    }
}
