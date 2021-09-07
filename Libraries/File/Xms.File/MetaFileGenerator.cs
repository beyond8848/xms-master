using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Xms.Context;
using Xms.Core.Data;
using Xms.Identity;
using Xms.Infrastructure.Utility;
using Xms.Sdk.Abstractions.Query;
using Xms.Sdk.Client;

namespace Xms.File
{
    /// <summary>
    /// 生成数据包
    /// </summary>
    public class MetaFileGenerator : AttachmentService, IMetaFileGenerator
    {
        private readonly IAppContext _appContext;
        private readonly IDataFinder _dataFinder;
        public string mainEntityName = "Reimbursement";
        public string subEntityName = "ReimbursedDetail";

        public string tempArchiveFolderPath = System.AppDomain.CurrentDomain.BaseDirectory + "\\_temp";
        public string destinationZipFilePath = "";

        public MetaFileGenerator(IAppContext appContext, IDataFinder dataFinder) :base(appContext)
        {
            _appContext = appContext;
            _dataFinder = dataFinder;
        }

        /// <summary>
        /// 把文件生XML，PDF成出来，并生成数据包，同时保存到ZIP文件中。
        /// </summary>
        /// <param name="mainEntityID"></param>
        public void  ZipFiles(Guid mainEntityID)
        {
            this.GenerateFileXML(mainEntityID);
            ZipArchiveHelper.CreatZip(tempArchiveFolderPath, destinationZipFilePath, CompressionLevel.Fastest);
        }

        public void GenerateFileXML(Guid entityId)
        {
            Entity mainEntity = _dataFinder.RetrieveById(mainEntityName,entityId);
            if(mainEntity!=null)
            {
                var query = new QueryExpression(subEntityName, _appContext.GetFeature<ICurrentUser>().UserSettings.LanguageId);
                query.ColumnSet.AddColumns("attachmentid", "cdnpath");
                query.Criteria.AddCondition("ReimbursedDetailId", ConditionOperator.Equal, entityId);
                List<Entity> subEntities = _dataFinder.RetrieveAll(query);
                ArchiveInstructions archiveInstructions = new ArchiveInstructions
                {
                    ArchiveItemInstance = new ArchiveInstructions.ArchiveItem
                    {
                        Claimer = mainEntity.GetStringValueExtension("Claimer"),
                        Amount =  mainEntity.GetDecimalValueExtension("MoneyAmount"),
                        ApplicationTime = mainEntity.GetDateValueExtension("Claimtime"),
                        Code = mainEntity.GetStringValueExtension("ClaimNo"),
                        Department = mainEntity.GetStringValueExtension("Department"),
                        Reason = mainEntity.GetStringValueExtension("reason"),
                        Title = mainEntity.GetStringValueExtension("name")
                    },
                    FilePackageInstance = new ArchiveInstructions.FilePackage
                    {
                         CreatedTime = System.DateTime.Now,
                         CreatedBy  = _appContext.GetFeature<ICurrentUser>().LoginName
                    }
                };

                this.CreateArchiveInstructionsXML(archiveInstructions);
                if(subEntities!=null && subEntities.Count>0)
                {
                    List<FileMetaItem> fileMetaItems = new List<FileMetaItem>();
                    foreach(var subEntity in subEntities)
                    {
                        FileMetaItem fileMetaItem = new FileMetaItem
                        {
                            Address = subEntity.GetStringValueExtension("Address"),
                            PieceNum = subEntity.GetIntValueExtension("Amount"),
                            InvoiceName = subEntity.GetStringValueExtension("Name"),
                            InvoiceType = subEntity.GetStringValueExtension("ReimbursedType"),
                            MoneyAmount = subEntity.GetIntValueExtension("MoneyAmount"),
                            StartTime = subEntity.GetDateValueExtension("FeeStartTime"),
                            EndTime = subEntity.GetDateValueExtension("FeeEndTime"),
                            UnitPrice = subEntity.GetDecimalValueExtension("UnitFee")
                        };
                    }
                    this.CreateFileMetaXML(fileMetaItems);
                }
            }
        }

        /// <summary>
        /// 创建文件包目录
        /// </summary>
        /// <param name="archiveInstructions"></param>
        public void CreateArchiveInstructionsXML(ArchiveInstructions archiveInstructions)
        {
            if (archiveInstructions == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<ArchiveInstructions>");
            //增加文件包相关属性。
            sb.AppendLine("<FilePackage>");
            PropertyInfo[] propertyInfos = archiveInstructions.FilePackageInstance.GetType().GetProperties();
            foreach (var pi in propertyInfos)
            {
                sb.AppendLine("<" + pi.Name + ">");
                sb.AppendLine(pi.GetValue(archiveInstructions.FilePackageInstance).ToString());
                sb.AppendLine("</" + pi.Name + ">");
            }
            sb.AppendLine("</FilePackage>");
            sb.AppendLine("<ArchiveItem>");
            propertyInfos = archiveInstructions.ArchiveItemInstance.GetType().GetProperties();
            foreach (var pi in propertyInfos)
            {
                sb.AppendLine("<" + pi.Name + ">");
                sb.AppendLine(pi.GetValue(archiveInstructions.ArchiveItemInstance).ToString());
                sb.AppendLine("</" + pi.Name + ">");
            }
            sb.AppendLine("</ArchiveItem>");
            sb.AppendLine("</ArchiveInstructions>");

            SaveToXMLFile(tempArchiveFolderPath+"\\ArchiveInstructions.xml", sb.ToString());
        }

        /// <summary>
        /// 保存XML文件内容
        /// </summary>
        /// <param name="xmlfilePath"></param>
        /// <param name="xmlContent"></param>
        public void SaveToXMLFile(string xmlfilePath,string xmlContent)
        {
            FileStream fs = new FileStream(xmlfilePath,FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(xmlContent);
            sw.Flush();
        }

        /// <summary>
        /// 创建文件目录
        /// </summary>
        /// <param name="filesDir"></param>
        public void CreateFileDirStructureXML(FilesDir filesDir)
        {
            if (filesDir == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<FilesCatalog>");
            sb.AppendLine("<FileCount>" + filesDir.FileCount + "</FileCount>");
            sb.AppendLine("<Files>");
            foreach(var oneFile in filesDir.Files)
            {
                sb.AppendLine("<File>");
                sb.AppendLine("<Directory>" + oneFile.DirName + "</Directory>");
                sb.AppendLine("<FilePath>" + oneFile.FilePath + "</FilePath>");
                sb.AppendLine("</File>");
            }
            sb.AppendLine("</Files>");
            sb.AppendLine("</FilesCatalog>");
            SaveToXMLFile(tempArchiveFolderPath+"\\FilesCatalog.xml", sb.ToString());
        }

        /// <summary>
        /// 创建文件元数据
        /// </summary>
        /// <param name="fileMetaItems"></param>
        public void CreateFileMetaXML(List<FileMetaItem> fileMetaItems)
        {
            if (fileMetaItems == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<FileMeta>");
            foreach(var fileItem in fileMetaItems)
            {
                sb.AppendLine("<FileItem>");
                PropertyInfo[] propertyInfos = fileItem.GetType().GetProperties();
                foreach (var pi in propertyInfos)
                {
                    sb.AppendLine("<" + pi.Name + ">");
                    sb.AppendLine(pi.GetValue(fileItem).ToString());
                    sb.AppendLine("</" + pi.Name + ">");
                }
                sb.AppendLine("</FileItem>");

            }
            sb.AppendLine("</FileMeta>");
            SaveToXMLFile(tempArchiveFolderPath+"\\fileMetaItems.xml", sb.ToString());
        }
    }


    #region 生成xml实体
    /// <summary>
    /// 文件包说明
    /// </summary>
    public class ArchiveInstructions
    {
        public FilePackage FilePackageInstance
        {
            get;set;
        }

        public ArchiveItem ArchiveItemInstance
        {
            get;set;
        }

        public class FilePackage
        {
            public string size { get; set; }
            public DateTime CreatedTime { get; set; }
            public string CreatedBy { get; set; }
            public string FileName { get; set; }
        }
        public class ArchiveItem
        {
            public string Claimer { get; set; }
            public DateTime ApplicationTime { get; set; }
            public string Department { get; set; }
            public Decimal Amount { get; set; }
            public string Reason { get; set; }
            public string Title { get; set; }
            public string Code { get; set; }
        }
    }

    /// <summary>
    /// 文件目录
    /// </summary>
    public class FilesDir
    {
        public int FileCount { get; set; }

        public  List<OneFileDir> Files { get; set; }
    }

    /// <summary>
    /// 一个文件目录
    /// </summary>
    public class OneFileDir
    {
        public string DirName { get; set; }

        public string FilePath { get; set; }
    }

    /// <summary>
    /// 文件元数据
    /// </summary>
    public class FileMetaItem
    {
        public string InvoiceName { get; set; }

        public string Address { get; set; }

        public string InvoiceType { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int PieceNum { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal MoneyAmount { get; set; }
    }
    #endregion

    public static class EntityExtensions
    {
        public static T GetValueExtension<T>(this Entity entity, string attributeName, object defaultValue = null)
        {
            if (entity.ContainsKey(attributeName))
            {
                var value = entity[attributeName];
                if (value != null && value.ToString().IsNotEmpty())
                {
                    var newvalue = value.ChangeType(typeof(T));
                    return (T)newvalue;
                }
            }
            if (defaultValue == null)
            {
                return default(T);
            }
            else
            {
                return (T)defaultValue;
            }
        }

        public static string GetStringValueExtension(this Entity entity, string attributeName)
        {
            return entity.ContainsKey(attributeName) && entity[attributeName] != null ? entity[attributeName].ToString() : string.Empty;
        }

        public static int GetIntValueExtension(this Entity entity, string attributeName, int defaultValue = 0)
        {
            return entity.GetValueExtension<int>(attributeName, defaultValue);
        }

        public static bool GetBoolValueExtension(this Entity entity, string attributeName)
        {
            return entity.GetValueExtension<bool>(attributeName);
        }

        public static decimal GetDecimalValueExtension(this Entity entity, string attributeName)
        {
            return entity.GetValueExtension<decimal>(attributeName);
        }

        public static DateTime GetDateValueExtension(this Entity entity, string attributeName)
        {
            return entity.GetValueExtension<DateTime>(attributeName);
        }

        public static Guid GetGuidValueExtension(this Entity entity, string attributeName)
        {
            return entity.GetValueExtension<Guid>(attributeName);
        }
    }
}
