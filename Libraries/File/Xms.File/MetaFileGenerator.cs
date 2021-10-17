using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using Xms.Context;
using Xms.Core.Data;
using Xms.Flow.Abstractions;
using Xms.Flow.Core.Events;
using Xms.Flow.Domain;
using Xms.Identity;
using Xms.Infrastructure.Utility;
using Xms.Logging.AppLog;
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
        private readonly ILogService _logService;
        

        public string mainEntityName = "Reimbursement";
        public string subEntityName = "ReimbursedDetail";

        /// <summary>
        /// 报销档案数据包所临时存储初
        /// </summary>
        public string tempArchiveFolderPath  = System.AppDomain.CurrentDomain.BaseDirectory + @"_temp";
        /// <summary>
        /// 报销流程PDF文件
        /// </summary>
        public string workflowPDFFilePath        = System.AppDomain.CurrentDomain.BaseDirectory + @"_temp\报销流程信息.pdf";

        /// <summary>
        /// 报销凭证PDF文件
        /// </summary>
        public string rembursmentCertifyFilePath = System.AppDomain.CurrentDomain.BaseDirectory + @"_temp\报销凭证.pdf";
        /// <summary>
        /// 上传的发票所在临时位置
        /// </summary>
        public string attachmentFolderPath   = System.AppDomain.CurrentDomain.BaseDirectory + @"_temp\Attachments";
        /// <summary>
        /// 文件池
        /// </summary>
        public string ASIPFILEPOOLPath = System.AppDomain.CurrentDomain.BaseDirectory + @"ASIP_POOL_FILES";

        public MetaFileGenerator(IAppContext appContext, IDataFinder dataFinder,ILogService logService) :base(appContext)
        {
            _appContext = appContext;
            _dataFinder = dataFinder;
            _logService = logService;
        }


        /// <summary>
        /// 拷贝上传的PDF，JPG文件.
        /// </summary>
        /// <param name="fullPathFiles"></param>
        public void CopyInvoiceFileToAttachmentFolder(List<string> fullPathFiles)
        {
            //复制之前，清楚相关文件。
            if (!Directory.Exists(attachmentFolderPath))
                Directory.CreateDirectory(attachmentFolderPath);
            else
            {
                foreach (var directory in Directory.GetFileSystemEntries(attachmentFolderPath))
                    if (System.IO.File.Exists(directory))
                        System.IO.File.Delete(directory); //直接删除其中的文件  
            }
            //然后，开始拷贝文件
            int orderNo = 0;
            foreach (var file in fullPathFiles)
            {
                orderNo++;
                System.IO.File.Copy(file, attachmentFolderPath + "\\"+ GetFileNameBasedNameRule("",orderNo) + System.IO.Path.GetExtension(file));
            }
        }

        private string GetFileNameBasedNameRule(string fileName,int orderNo)
        {
            string fileCompletedName = string.Empty;
            if(!string.IsNullOrWhiteSpace(fileName))
            {
                //fileCompletedName = fileName;
                fileCompletedName = fileName + "-" + orderNo.ToString().PadLeft(4, '0');
            }
            else
            {
                fileCompletedName = orderNo.ToString().PadLeft(4, '0');
            }

            return fileCompletedName;
        }

       
        /// <summary>
        /// 把文件生XML，PDF成出来，并生成数据包，同时保存到ZIP文件中。
        /// </summary>
        /// <param name="mainEntityID"></param>
        public void  ZipFiles(Guid mainEntityID, List<WorkFlowInstance> workFlowInstances)
        {
            if(workFlowInstances.Count<=0)
            {
                _logService.Warning("输出工作流实例信息!");
            }

            try
            {
                //获取附件文件,需要从报销明细表里去获取，因为用户有可能删除报销明细
                List<string> fullPathFiles = this.GetPDFFiles(mainEntityID.ToString());
                if (fullPathFiles != null)
                    this.CopyInvoiceFileToAttachmentFolder(fullPathFiles);

                //附件流程信息包
                this.CreateWorkFlowPDFWithMultipleInstance(_logService,workFlowInstances);

                ArchiveInstructions archiveInstructions = null;

                //生成ASIP数据包中的XML文件
                string archiveNO = this.GenerateFileXML(mainEntityID, workFlowInstances[0].Steps, out archiveInstructions);

                //生成报销凭证
                this.CreateRebursmentCertify(_logService,archiveInstructions.ArchiveItemInstance);

                //设定压缩包文件名。
                string destinationZipFilePath = System.AppDomain.CurrentDomain.BaseDirectory + @"_tempZip\" + archiveNO + ".zzip";

                //构建文件目录结构xml文件
                List<FileInfo> files = this. GetAllFilesByDir(tempArchiveFolderPath);
                CenerateFileDirectoryStructure(files);

                //压缩文件，生成ASIP整个数据包。
                ZipArchiveHelper.CreatZip(_logService, tempArchiveFolderPath, destinationZipFilePath, CompressionLevel.Fastest);

                //拷贝压缩ASIP文件包到指定的位置。
                if (System.IO.File.Exists(destinationZipFilePath))
                {
                    if (!Directory.Exists(ASIPFILEPOOLPath))
                    {
                        Directory.CreateDirectory(ASIPFILEPOOLPath);
                    }

                    System.IO.File.Copy(destinationZipFilePath, ASIPFILEPOOLPath + @"\" + System.IO.Path.GetFileName(destinationZipFilePath), true);
                }
            }catch(Exception ex)
            {
                _logService.Error("Failed to Generate ASIP Data package", ex);
            }
        }


        /// <summary>
        /// 得到所有文件。
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        private List<string> GetPDFFiles(string objectId)
        {
            //var query = new QueryExpression("Attachment", _appContext.GetFeature<ICurrentUser>().UserSettings.LanguageId);
            //query.ColumnSet.AddColumns("cdnpath");
            //query.Criteria.AddCondition("ObjectId", ConditionOperator.Equal, objectId);
            //List<Entity> subEntities = _dataFinder.RetrieveAll(query);
            //string baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var query = new QueryExpression("ReimbursedDetail", _appContext.GetFeature<ICurrentUser>().UserSettings.LanguageId);
            query.ColumnSet.AddColumns("AssociatedFilePath");
            query.Criteria.AddCondition("ReimbursementId", ConditionOperator.Equal, objectId);
            List<Entity> subEntities = _dataFinder.RetrieveAll(query);
            List<string> files = new List<string>();
            string filePath = string.Empty;
            foreach (var entity in subEntities)
            {
                filePath = entity.GetStringValueExtension("AssociatedFilePath");
                if(System.IO.File.Exists(filePath))
                files.Add(filePath);
            }

            return files;
        }

        public string GetUserNameByUserGuid(Guid userGuid)
        {
            var query = new QueryExpression("SystemUser", _appContext.GetFeature<ICurrentUser>().UserSettings.LanguageId);
            query.ColumnSet.AddColumns("Name");
            query.Criteria.AddCondition("SystemUserId", ConditionOperator.Equal, userGuid);
            Entity entity = _dataFinder.Retrieve(query, true);
            string userName = string.Empty;
            if (entity != null)
            {
                userName = entity.GetStringValueExtension("Name");
            }
            return userName;
        }


        public void CreateRebursmentCertify(ILogService logService, ArchiveItem archiveItem)
        {
            PDFCreator<ArchiveItem> pDFCreator = new PDFCreator<ArchiveItem>();
            pDFCreator.CreateRebursmentCertifyPDF(_logService, rembursmentCertifyFilePath, archiveItem);
        }

        /// <summary>
        /// 生成工作流备忘PDF
        /// </summary>
        /// <param name="workFlowProcesses"></param>
        public void CreateWorkFlowPDFWithMultipleInstance(ILogService _logService,List<WorkFlowInstance> workflowInstances)
        {
            PDFCreator<WorkFlowTinyInfo> pDFCreator = new PDFCreator<WorkFlowTinyInfo>();

            Func<Guid, string> func = (s) =>
             {
                 return GetUserNameByUserGuid(s);
             };

            pDFCreator.CreateWorkFlowPDFForMultipleWorkFlowInsance(_logService,workflowPDFFilePath, workflowInstances,func);
        }

        /// <summary>
        /// 生成工作流备忘PDF
        /// </summary>
        /// <param name="workFlowProcesses"></param>
        public void CreateWorkFlowPDF(List<WorkFlowProcess> workFlowProcesses)
        {
            List<WorkFlowTinyInfo> workFlowTinyInfos = new List<WorkFlowTinyInfo>();
            foreach(var workflowinfo in workFlowProcesses)
            {
                WorkFlowTinyInfo workFlowTinyInfo = new WorkFlowTinyInfo
                {
                    Description = workflowinfo.Description,
                    HandleName = workflowinfo.Name,
                    HandlerIdName = workflowinfo.HandlerIdName,
                    Status = GetStatusDesc(workflowinfo.StateCode),
                    processedTime = workflowinfo.HandleTime.Value
                };

                workFlowTinyInfos.Add(workFlowTinyInfo);
            }

            PDFCreator<WorkFlowTinyInfo> pDFCreator = new PDFCreator<WorkFlowTinyInfo>();
            pDFCreator.CreateWorkFlowPDF<WorkFlowTinyInfo>(workflowPDFFilePath, workFlowTinyInfos, new float[] { 20, 35, 45, 50 });
        }

        private string GetStatusDesc(WorkFlowProcessState StateCode)
        {
            string statusDesc = "";
            if(StateCode == WorkFlowProcessState.Passed)
            {
                statusDesc = "通过";
            }
            else if(StateCode == WorkFlowProcessState.Canceled)
            {
                statusDesc = "取消";
            }
            else if (StateCode == WorkFlowProcessState.Disabled)
            {
                statusDesc = "禁用";
            }
            else if (StateCode == WorkFlowProcessState.Processing)
            {
                statusDesc = "处理中";
            }
            else if (StateCode == WorkFlowProcessState.UnPassed)
            {
                statusDesc = "未通过";
            }
            else if (StateCode == WorkFlowProcessState.Waiting)
            {
                statusDesc = "等待";
            }

            return statusDesc;
        }

        public void CenerateFileDirectoryStructure(List<FileInfo> fileInfos)
        {
            FilesDir filesDir = new FilesDir
            {
                FileCount = fileInfos.Count,
                Files = new List<OneFileDir>()
            };

            foreach(var fileinfo in fileInfos)
            {
                filesDir.Files.Add(
                     new OneFileDir
                     {
                         DirName = fileinfo.DirectoryName,
                         FilePath = fileinfo.FullName
                     }
                    );
            }

            CreateFileDirStructureXML(filesDir);
        }

        /// <summary>
        /// 生成文件包说明文件，目录信息文件和发票文件。
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="workFlowProcesses"></param>
        /// <returns></returns>
        public string GenerateFileXML(Guid entityId, List<WorkFlowProcess> workFlowProcesses,out ArchiveInstructions archiveInstructions)
        {
            string strArchiveNo = string.Empty;
            Entity mainEntity = _dataFinder.RetrieveById(mainEntityName,entityId);
            if(mainEntity!=null)
            {
                strArchiveNo = mainEntity.GetStringValueExtension("ClaimNo");
                var query = new QueryExpression(subEntityName, _appContext.GetFeature<ICurrentUser>().UserSettings.LanguageId);
                query.ColumnSet.AllColumns = true;
                query.Criteria.AddCondition("ReimbursementId", ConditionOperator.Equal, entityId);
                List<Entity> subEntities = _dataFinder.RetrieveAll(query);
                string departmentName = string.Empty;
                string departmentGuid = mainEntity.GetStringValueExtension("Department");
                if(!string.IsNullOrWhiteSpace(departmentGuid))
                {
                    Entity entity = _dataFinder.RetrieveById("BusinessUnit", new Guid(departmentGuid));
                    if(entity!=null)
                    {
                        departmentName = entity.GetStringValueExtension("Name");
                    }
                }
                
                archiveInstructions = new ArchiveInstructions
                {
                    ArchiveItemInstance = new ArchiveItem
                    {
                        Claimer = mainEntity.GetStringValueExtension("Claimer"),
                        Amount =  mainEntity.GetDecimalValueExtension("MoneyAmount"),
                        ApplicationTime = mainEntity.GetDateValueExtension("Claimtime"),
                        Code = mainEntity.GetStringValueExtension("ClaimNo"),
                        Department = departmentName,
                        Reason = mainEntity.GetStringValueExtension("reason"),
                        Title = mainEntity.GetStringValueExtension("name")
                    },
                    FilePackageInstance = new FilePackage
                    {
                         CreatedTime = System.DateTime.Now,
                         CreatedBy  = _appContext.GetFeature<ICurrentUser>().LoginName
                    }
                };

                this.CreateArchiveInstructionsXML(archiveInstructions);
                if(subEntities!=null && subEntities.Count>0)
                {
                    //List<FileMetaItem> fileMetaItems = new List<FileMetaItem>();
                    int orderNo = 0;
                    foreach(var subEntity in subEntities)
                    {
                        orderNo++;
                        FileMetaItem fileMetaItem = new FileMetaItem
                        {
                            Address = subEntity.GetStringValueExtension("Address"),
                            PieceNum = subEntity.GetIntValueExtension("Amount"),
                            InvoiceName = subEntity.GetStringValueExtension("Name"),
                            InvoiceType = subEntity.GetStringValueExtension("ReimbursedType"),
                            MoneyAmount = subEntity.GetDecimalValueExtension("MoneyAmount"),
                            StartTime = subEntity.GetDateValueExtension("FeeStartTime"),
                            EndTime = subEntity.GetDateValueExtension("FeeEndTime"),
                            UnitPrice = subEntity.GetDecimalValueExtension("UnitFee"),
                            InvoiceCode = subEntity.GetStringValueExtension("InvoiceCode"),
                            ArchiveNo = subEntity.GetStringValueExtension("ArchiveNo"),
                            FilePath = subEntity.GetStringValueExtension("AssociatedFilePath")
                        };
                        this.CreateSingleFileMetaXML(fileMetaItem, GetFileNameBasedNameRule("", orderNo));
                        //fileMetaItems.Add(fileMetaItem);
                    }
                    //this.CreateFileMetaXML(fileMetaItems);
                }
            }
            else
            {
                archiveInstructions = new ArchiveInstructions
                { };
            }
            return strArchiveNo;
        }

        /// <summary>
        /// 创建文件包目录
        /// </summary>
        /// <param name="archiveInstructions"></param>
        public void CreateArchiveInstructionsXML(ArchiveInstructions archiveInstructions)
        {
            if (archiveInstructions == null) return;
            if (!Directory.Exists(tempArchiveFolderPath))
                Directory.CreateDirectory(tempArchiveFolderPath);
            string sb = XmlSerializeHelper.XmlSerialize<ArchiveInstructions>(archiveInstructions);
            SaveToXMLFile(tempArchiveFolderPath+"\\案卷说明.xml", sb.ToString());
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
            sw.Close();
            fs.Close();
            sw.Dispose();
            fs.Dispose();
        }

        /// <summary>
        /// 创建文件目录
        /// </summary>
        /// <param name="filesDir"></param>
        public void CreateFileDirStructureXML(FilesDir filesDir)
        {
            string sb = XmlSerializeHelper.XmlSerialize<FilesDir>(filesDir);
            SaveToXMLFile(tempArchiveFolderPath+"\\目录结构.xml", sb.ToString());
        }


        public void CreateSingleFileMetaXML(FileMetaItem fileMetaItem,string fileName)
        {
            if (fileMetaItem == null) return;
            string sb = XmlSerializeHelper.XmlSerialize<FileMetaItem>(fileMetaItem);
            SaveToXMLFile(tempArchiveFolderPath + "\\Attachments\\"+fileName+".xml", sb.ToString());
        }

        /// <summary>
        /// 创建文件元数据
        /// </summary>
        /// <param name="fileMetaItems"></param>
        public void CreateFileMetaXML(List<FileMetaItem> fileMetaItems)
        {
            if (fileMetaItems == null) return;
            string sb = XmlSerializeHelper.XmlSerialize<List<FileMetaItem>>(fileMetaItems);
            SaveToXMLFile(tempArchiveFolderPath+ "\\Attachments\\fileMetaItems.xml", sb.ToString());
        }

        // <summary>
        /// 获得指定目录下的所有文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public List<FileInfo> GetFilesByDir(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);

            //找到该目录下的文件
            FileInfo[] fi = di.GetFiles();

            //把FileInfo[]数组转换为List
            List<FileInfo> list = fi.ToList<FileInfo>();
            return list;
        }

        /// <summary>
        /// 获得指定目录及其子目录的所有文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public List<FileInfo> GetAllFilesByDir(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);

            //找到该目录下的文件
            FileInfo[] fi = dir.GetFiles();

            //把FileInfo[]数组转换为List
            List<FileInfo> list = fi.ToList<FileInfo>();

            //找到该目录下的所有目录里的文件
            DirectoryInfo[] subDir = dir.GetDirectories();
            foreach (DirectoryInfo d in subDir)
            {
                List<FileInfo> subList = GetFilesByDir(d.FullName);
                foreach (FileInfo subFile in subList)
                {
                    list.Add(subFile);
                }
            }
            return list;
        }
    }


    #region 生成xml实体
    public class WorkFlowTinyInfo
    {
        public string HandleName { get; set; }

        public string HandlerIdName { get; set; }


        public string Description { get; set; }

        public string Status { get; set; }

        public DateTime processedTime { get; set; }

    }
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

    /// <summary>
    /// 文件目录
    /// </summary>
    public class FilesDir
    {
        public int FileCount { get; set; }

        public List<OneFileDir> Files { get; set; }
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

        public string FilePath { get; set; }

        public string ArchiveNo { get; set; }

        public string InvoiceCode { get; set; }
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
