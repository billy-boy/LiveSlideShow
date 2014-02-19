using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Data;

using log4net;
using log4net.Config;

using matIT.Generic.Config.AppConfig;
using matIT.SlideShow;
using matIT.SlideShow.ClassTypes;

namespace matIT.SlideShow.DB
{
    public class SDB : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private OdbcConnection _databaseConnection;

        #region Konstruktor & Dispose

        public SDB()
        {
            _databaseConnection = new OdbcConnection(   @"DRIVER={" + MAppConfig.getInstance().getConfigValue("databaseAccess", "odbcDriver", "MySQL ODBC 5.1 Driver") + "};" +
                                                        "SERVER=" + MAppConfig.getInstance().getConfigValue("databaseAccess", "odbcHost", "localhost") + ";" +
                                                        "DATABASE=" + MAppConfig.getInstance().getConfigValue("databaseAccess", "odbcDatabase", "SlideShow") + ";" +
                                                        "UID=" + MAppConfig.getInstance().getConfigValue("databaseAccess", "odbcUser", "root") + ";" +
                                                        "PWD=" + MAppConfig.getInstance().getConfigValue("databaseAccess", "odbcPassword", "markusen") + ";");
        }
        ~SDB()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool safe)
        {
            closeDB();
        }

        #endregion

        #region DB-Handling

        private void openDB()
        {
            if(_databaseConnection.State == ConnectionState.Closed || _databaseConnection.State == ConnectionState.Broken)
                try
                {
                    _databaseConnection.Open();
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled) log.Error("Failed to connect to DB: "+e.Message);
                }
        }

        private void closeDB()
        {
            if (_databaseConnection.State != ConnectionState.Closed)
                try
                {
                    _databaseConnection.Close();
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled) log.Error("Failed to close DB session: " + e.Message);
                }
        }

        #endregion

        #region DB-Write

        public void insertImage(CImage image)
        {
            String databaseSQL =    "INSERT INTO tblImages "+
                                    "(imagePath,importDate) " +
                                    "VALUES('" + escapeEscapes(image.FilePath) + "',STR_TO_DATE('" + DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss") + "', '%d.%m.%Y %h:%i:%s' )) ";
            try
            {
                openDB();

                OdbcCommand databaseCommand = new OdbcCommand(databaseSQL, _databaseConnection);
                databaseCommand.ExecuteNonQuery();

                closeDB();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Failed to write SQL '"+databaseSQL+"' to DB: " + e.Message);
            }
        }

        public void updateImage(CImage image)
        {
            String databaseSQL = "UPDATE tblImages " +
                                 "SET imagePath = '" + escapeEscapes(image.FilePath) + "' ";
            if (image.Imported)
                databaseSQL += ", importDate = STR_TO_DATE('" + image.ImportedDate.ToString("dd.MM.yyyy HH:mm:ss") + "', '%d.%m.%Y %H:%i:%s' ) ";
            if (image.Viewed)
              databaseSQL += ", viewDate = STR_TO_DATE('" + image.ViewedDate.ToString("dd.MM.yyyy HH:mm:ss") + "', '%d.%m.%Y %H:%i:%s' ) ";
            if (image.ViewCount != 0)
                databaseSQL += ", viewCount = " + image.ViewCount.ToString() + " ";
            if (image.Uploaded)
                databaseSQL += ", uploadDate = STR_TO_DATE('" + image.UploadedDate.ToString("dd.MM.yyyy HH:mm:ss") + "', '%d.%m.%Y %H:%i:%s' ) ";
            databaseSQL += "WHERE imageID = " + image.FileId;

            try
            {
                openDB();

                OdbcCommand databaseCommand = new OdbcCommand(databaseSQL, _databaseConnection);
                databaseCommand.ExecuteNonQuery();

                closeDB();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Failed to write SQL '" + databaseSQL + "' to DB: " + e.Message);
            }
        }

        #endregion

        #region DB-Read

        #region Statistic-Querys
        
        public int getViewedCount()
        {
            int viewCount = 0;

            String databaseSQL = "SELECT COUNT(*) " +
                                    "FROM tblImages " +
                                    "WHERE NOT viewDate IS NULL ";
            try
            {
                openDB();

                OdbcCommand databaseCommand = new OdbcCommand(databaseSQL, _databaseConnection);
                OdbcDataReader databaseReader = databaseCommand.ExecuteReader();

                if (databaseReader.Read())
                    viewCount = databaseReader.GetInt32(0);

                closeDB();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Couldn't execute SQL-Query: '"+databaseSQL+"' Exception: '"+e.Message+"'");
            }

            return viewCount;
        }

        public int getNonViewedCount()
        {
            int viewCount = 0;

            String databaseSQL = "SELECT COUNT(*) " +
                                    "FROM tblImages " +
                                    "WHERE viewDate IS NULL ";
            try
            {
                openDB();

                OdbcCommand databaseCommand = new OdbcCommand(databaseSQL, _databaseConnection);
                OdbcDataReader databaseReader = databaseCommand.ExecuteReader();

                if (databaseReader.Read())
                    viewCount = databaseReader.GetInt32(0);

                closeDB();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Couldn't execute SQL-Query: '" + databaseSQL + "' Exception: '" + e.Message + "'");
            }

            return viewCount;
        }

        public int getUploadedCount()
        {
            int viewCount = 0;

            String databaseSQL = "SELECT COUNT(*) " +
                                    "FROM tblImages " +
                                    "WHERE NOT uploadDate IS NULL ";
            try
            {
                openDB();

                OdbcCommand databaseCommand = new OdbcCommand(databaseSQL, _databaseConnection);
                OdbcDataReader databaseReader = databaseCommand.ExecuteReader();

                if (databaseReader.Read())
                    viewCount = databaseReader.GetInt32(0);

                closeDB();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Couldn't execute SQL-Query: '" + databaseSQL + "' Exception: '" + e.Message + "'");
            }

            return viewCount;
        }

        public int getNonUploadedCount()
        {
            int viewCount = 0;

            String databaseSQL = "SELECT COUNT(*) " +
                                    "FROM tblImages " +
                                    "WHERE uploadDate IS NULL ";
            try
            {
                openDB();

                OdbcCommand databaseCommand = new OdbcCommand(databaseSQL, _databaseConnection);
                OdbcDataReader databaseReader = databaseCommand.ExecuteReader();

                if (databaseReader.Read())
                    viewCount = databaseReader.GetInt32(0);

                closeDB();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Couldn't execute SQL-Query: '" + databaseSQL + "' Exception: '" + e.Message + "'");
            }

            return viewCount;
        }

        #endregion

        #region Operational-Querys

        private CImage getImage(String databaseSQL)
        {
            CImage image = null;

            try
            {
                openDB();

                OdbcCommand databaseCommand = new OdbcCommand(databaseSQL, _databaseConnection);
                OdbcDataReader databaseReader = databaseCommand.ExecuteReader();

                if (databaseReader.Read())
                {
                    image = new CImage();
                    image.FileId = databaseReader.GetInt32(0);
                    image.FilePath = databaseReader.GetString(1);
                    //Replacing
                    String filePathReplace_Replace = MAppConfig.getInstance().getConfigValue("databaseAccess", "replaceFilePath_Replace", "");
                    String filePathReplace_New = MAppConfig.getInstance().getConfigValue("databaseAccess", "replaceFilePath_New", "");
                    if (filePathReplace_Replace != "")
                    {
                        if (log.IsInfoEnabled) log.Info("Replacing file path!");
                        if (log.IsInfoEnabled) log.Info("-> Old file path is: '"+image.FilePath+"'.");
                        image.FilePath = image.FilePath.Replace(filePathReplace_Replace,filePathReplace_New);
                        if (log.IsInfoEnabled) log.Info("-> New file path is: '"+image.FilePath+"'.");
                    }
                    image.Imported = true;
                    if (!databaseReader.GetValue(2).Equals(DBNull.Value))
                        image.ImportedDate = databaseReader.GetDateTime(2);
                    if (!databaseReader.GetValue(3).Equals(DBNull.Value))
                    {
                        image.Viewed = true;
                        image.ViewedDate = databaseReader.GetDateTime(3);
                    }
                    if (!databaseReader.GetValue(4).Equals(DBNull.Value))
                    {
                        image.Uploaded = true;
                        image.UploadedDate = databaseReader.GetDateTime(4);
                    }
                    if (!databaseReader.GetValue(5).Equals(DBNull.Value))
                    {
                        image.ViewCount = databaseReader.GetInt32(5);
                    }
                }

                closeDB();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Failed to read SQL '" + databaseSQL + "' from DB: " + e.Message);
            }
            return image;
        }

        public CImage getNextViewImage()
        {
            String databaseSQL = "" +
                        "SELECT imageID, imagePath, importDate, viewDate, uploadDate, viewCount " +
                        "FROM tblImages " +
                        "WHERE viewDate IS NULL " +
                        "ORDER BY importDate ASC " +
                        "LIMIT 0,1";
            return getImage(databaseSQL);
        }

        public CImage getLastPickedUpImage()
        {
            String databaseSQL = "" +
                        "SELECT imageID, imagePath, importDate, viewDate, uploadDate, viewCount " +
                        "FROM tblImages " +
                        "ORDER BY importDate DESC " +
                        "LIMIT 0,1";
            return getImage(databaseSQL);
        }

        public CImage getRandomImage()
        {
            String databaseSQL = "" +
                        "SELECT imageID, imagePath, importDate, viewDate, uploadDate, viewCount " +
                        "FROM `tblImages` " +
                        "WHERE imageID >= (SELECT FLOOR( MAX(imageID) * RAND()) FROM `tblImages` ) " +
                        "ORDER BY imageID " +
                        "LIMIT 1";
            return getImage(databaseSQL);
        }
        
        public List<CImage> getNonViewedImages()
        {
            List<CImage> ret = new List<CImage>();

            String databaseSQL =    "SELECT imageID,imagePath,importDate,viewDate,uploadDate,viewCount "+
                                    "FROM tblImages "+
                                    "WHERE viewDate IS NULL "+
                                    "ORDER BY importDate DESC";
            try
            {
                openDB();

                OdbcCommand databaseCommand = new OdbcCommand(databaseSQL, _databaseConnection);
                OdbcDataReader databaseReader = databaseCommand.ExecuteReader();

                // [0] = ID
                // [1] = Path
                // [2] = ImportDate
                // [3] = Viewed
                // [4] = ViewDate
                // [5] = Uploaded
                // [6] = UploadDate
                // [7] = ViewCount
                while (databaseReader.Read())
                {
                    CImage image = new CImage();
                    image.FileId = databaseReader.GetInt32(0);
                    image.FilePath = databaseReader.GetString(1);
                    image.Imported = true;
                    if (!databaseReader.GetValue(2).Equals(DBNull.Value))
                        image.ImportedDate = databaseReader.GetDateTime(2);
                    if (!databaseReader.GetValue(3).Equals(DBNull.Value))
                    {
                        image.Viewed = true;
                        image.ViewedDate = databaseReader.GetDateTime(3);
                    }
                    if (!databaseReader.GetValue(4).Equals(DBNull.Value))
                    {
                        image.Uploaded = true;
                        image.UploadedDate = databaseReader.GetDateTime(4);
                    }
                    if (!databaseReader.GetValue(5).Equals(DBNull.Value))
                    {
                        image.ViewCount = databaseReader.GetInt32(5);
                    }
                }
                databaseReader.Close();

                closeDB();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Failed to read SQL '" + databaseSQL + "' from DB: " + e.Message);
            }
            return ret;
        }

        public List<CImage> getNonUploadedImages()
        {
            List<CImage> ret = new List<CImage>();

            //Achtung: Erst viewen, dann uploaden (because of silly I/O Exception stuff)
            String databaseSQL = "SELECT imageID,imagePath,importDate,viewDate,uploadDate,viewCount " +
                                    "FROM tblImages " +
                                    "WHERE uploadDate IS NULL " +
                                    "AND NOT viewDate IS NULL " +
                                    "ORDER BY importDate ASC";
            try
            {
                openDB();

                OdbcCommand databaseCommand = new OdbcCommand(databaseSQL, _databaseConnection);
                OdbcDataReader databaseReader = databaseCommand.ExecuteReader();

                // [0] = ID
                // [1] = Path
                // [2] = ImportDate
                // [3] = Viewed
                // [4] = ViewDate
                // [5] = Uploaded
                // [6] = UploadDate
                // [7] = ViewCount
                while (databaseReader.Read())
                {
                    CImage image = new CImage();
                    image.FileId = databaseReader.GetInt32(0);
                    image.FilePath = databaseReader.GetString(1);
                    image.Imported = true;
                    if (!databaseReader.GetValue(2).Equals(DBNull.Value))
                        image.ImportedDate = databaseReader.GetDateTime(2);
                    if (!databaseReader.GetValue(3).Equals(DBNull.Value))
                    {
                        image.Viewed = true;
                        image.ViewedDate = databaseReader.GetDateTime(3);
                    }
                    if (!databaseReader.GetValue(4).Equals(DBNull.Value))
                    {
                        image.Uploaded = true;
                        image.UploadedDate = databaseReader.GetDateTime(4);
                    }
                    if (!databaseReader.GetValue(5).Equals(DBNull.Value))
                    {
                        image.ViewCount = databaseReader.GetInt32(5);
                    }
                    ret.Add(image);
                }
                databaseReader.Close();

                closeDB();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Failed to read SQL '" + databaseSQL + "' from DB: " + e.Message);
            }
            return ret;
        }

        #endregion

        #endregion

        private String escapeEscapes(String sql)
        {
            return sql.Replace("\\", "\\\\");
        }

    }
}
