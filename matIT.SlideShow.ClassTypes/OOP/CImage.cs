using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace matIT.SlideShow.ClassTypes
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
        private int _viewCount = 0;

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

        public int ViewCount
        {
            get { return _viewCount; }
            set { _viewCount = value; }
        }

        #region Static Helpers

        // Match the orientation code to the correct rotation:
        public static RotateFlipType OrientationToFlipType(string orientation)
        {
            switch (int.Parse(orientation))
            {
                case 1:
                    return RotateFlipType.RotateNoneFlipNone;
                    break;
                case 2:
                    return RotateFlipType.RotateNoneFlipX;
                    break;
                case 3:
                    return RotateFlipType.Rotate180FlipNone;
                    break;
                case 4:
                    return RotateFlipType.Rotate180FlipX;
                    break;
                case 5:
                    return RotateFlipType.Rotate90FlipX;
                    break;
                case 6:
                    return RotateFlipType.Rotate90FlipNone;
                    break;
                case 7:
                    return RotateFlipType.Rotate270FlipX;
                    break;
                case 8:
                    return RotateFlipType.Rotate270FlipNone;
                    break;
                default:
                    return RotateFlipType.RotateNoneFlipNone;
            }
        }

        //Read operation through memory stream
        public static MemoryStream LoadFileToMemoryStream(String fileName)
        {
            MemoryStream memoryStream = new MemoryStream();
            if (File.Exists(fileName))
            {              
                byte[] fileBytes = File.ReadAllBytes(fileName);
                memoryStream.Write(fileBytes, 0, fileBytes.Length);
                memoryStream.Position = 0;
            }
            return memoryStream;
        }

        //Gets encoder GUID
        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        #endregion
    }
}
