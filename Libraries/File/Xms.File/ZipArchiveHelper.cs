using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Xms.Logging.AppLog;

namespace Xms.File
{
    public class ZipArchiveHelper 
    {

        #region  Methods

        /// <summary>
        /// 创建 zip 存档，该文档包含指定目录的文件和子目录。
        /// </summary>
        /// <param name="sourceDirectoryName">将要压缩存档的文件目录的路径，可以为相对路径或绝对路径。 相对路径是指相对于当前工作目录的路径。</param>
        /// <param name="destinationArchiveFileName">将要生成的压缩包的存档路径，可以为相对路径或绝对路径。相对路径是指相对于当前工作目录的路径。</param>
        /// <param name="compressionLevel">指示压缩操作是强调速度还是强调压缩大小的枚举值</param>
        /// <param name="includeBaseDirectory">压缩包中是否包含父目录</param>
        public static  bool CreatZip(ILogService logService,string sourceDirectoryName, string destinationArchiveFileName,
            CompressionLevel compressionLevel = CompressionLevel.NoCompression, bool includeBaseDirectory = false)
        {
            try
            {
                if (Directory.Exists(sourceDirectoryName))
                    if (!System.IO.File.Exists(destinationArchiveFileName))
                    {
                        ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory);
                    }
                    else
                    {
                        var toZipFileDictionaryList = GetAllDirList(sourceDirectoryName, includeBaseDirectory);
                        using (var archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Update))
                        {
                            foreach (var toZipFileKey in toZipFileDictionaryList.Keys)
                                if (toZipFileKey != destinationArchiveFileName)
                                {
                                    var toZipedFileName = Path.GetFileName(toZipFileKey);
                                    var toDelArchives = new List<ZipArchiveEntry>();
                                    foreach (var zipArchiveEntry in archive.Entries)
                                        if (toZipedFileName != null && (zipArchiveEntry.FullName.StartsWith(toZipedFileName) || toZipedFileName.StartsWith(zipArchiveEntry.FullName)))
                                            toDelArchives.Add(zipArchiveEntry);
                                    foreach (var zipArchiveEntry in toDelArchives)
                                        zipArchiveEntry.Delete();
                                    archive.CreateEntryFromFile(toZipFileKey, toZipFileDictionaryList[toZipFileKey], compressionLevel);
                                }
                        }
                    }
                else if (System.IO.File.Exists(sourceDirectoryName))
                    if (!System.IO.File.Exists(destinationArchiveFileName))
                        ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, false);
                    else
                        using (var archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Update))
                        {
                            if (sourceDirectoryName != destinationArchiveFileName)
                            {
                                var toZipedFileName = Path.GetFileName(sourceDirectoryName);
                                var toDelArchives = new List<ZipArchiveEntry>();
                                foreach (var zipArchiveEntry in archive.Entries)
                                    if (toZipedFileName != null && (zipArchiveEntry.FullName.StartsWith(toZipedFileName) || toZipedFileName.StartsWith(zipArchiveEntry.FullName)))
                                        toDelArchives.Add(zipArchiveEntry);
                                foreach (var zipArchiveEntry in toDelArchives)
                                    zipArchiveEntry.Delete();
                                archive.CreateEntryFromFile(sourceDirectoryName, toZipedFileName, compressionLevel);
                            }
                        }
                else
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                logService.Error("The error of creating zip occurence happened", ex);
                return false;
            }
        }

        /// <summary>
        /// 创建 zip 存档，该存档包含指定目录的文件和目录。
        /// </summary>
        /// <param name="sourceDirectoryName">将要压缩存档的文件目录的路径，可以为相对路径或绝对路径。 相对路径是指相对于当前工作目录的路径。</param>
        /// <param name="destinationArchiveFileName">将要生成的压缩包的存档路径，可以为相对路径或绝对路径。 相对路径是指相对于当前工作目录的路径。</param>
        /// <param name="compressionLevel">指示压缩操作是强调速度还是强调压缩大小的枚举值</param>
        public static  bool CreatZip(ILogService logService, Dictionary<string, string> sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel = CompressionLevel.NoCompression)
        {
            try
            {
                using (FileStream zipToOpen = new FileStream(destinationArchiveFileName, FileMode.OpenOrCreate))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        foreach (var toZipFileKey in sourceDirectoryName.Keys)
                            if (toZipFileKey != destinationArchiveFileName)
                            {
                                var toZipedFileName = Path.GetFileName(toZipFileKey);
                                var toDelArchives = new List<ZipArchiveEntry>();
                                foreach (var zipArchiveEntry in archive.Entries)
                                    if (toZipedFileName != null && (zipArchiveEntry.FullName.StartsWith(toZipedFileName) || toZipedFileName.StartsWith(zipArchiveEntry.FullName)))
                                        toDelArchives.Add(zipArchiveEntry);
                                foreach (var zipArchiveEntry in toDelArchives)
                                    zipArchiveEntry.Delete();
                                archive.CreateEntryFromFile(toZipFileKey, sourceDirectoryName[toZipFileKey], compressionLevel);
                            }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                logService.Error("The error of creating zip occurence happened", ex);
                return false;
            }
        }

        /// <summary>
        ///     递归删除磁盘上的指定文件夹目录及文件
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <returns></returns>
        public static bool DeleteFolder(string baseDirectory)
        {
            var successed = true;
            try
            {
                if (Directory.Exists(baseDirectory)) //如果存在这个文件夹删除之 
                {
                    foreach (var directory in Directory.GetFileSystemEntries(baseDirectory))
                        if (System.IO.File.Exists(directory))
                            System.IO.File.Delete(directory); //直接删除其中的文件  
                        else
                            successed = DeleteFolder(directory); //递归删除子文件夹 
                    Directory.Delete(baseDirectory); //删除已空文件夹     
                }
            }
            catch (Exception ex)
            {
                //LogHelper.Error("Error! ", ex);
                successed = false;
            }
            return successed;
        }

        /// <summary>
        /// 调用CMD，命令行删除磁盘上的指定目录，以防止系统底层的异步删除机制
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        private static bool DeleteDirectoryWithCmd(string dirPath)
        {
            var process = new Process();
            //ProcessStartInfo构造方法的第二个参数是执行第一个命令的所需要传入的参数，以空格分隔各个参数
            var processStartInfo = new ProcessStartInfo("CMD.EXE", "/C rd /S /Q \"" + dirPath + "\"") { UseShellExecute = false, RedirectStandardOutput = true };
            process.StartInfo = processStartInfo;
            process.Start();
            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
            if (string.IsNullOrWhiteSpace(output))
                return true;
            return false;
        }

        /// <summary>
        /// 调用CMD，命令行删除磁盘上的指定目录，以防止系统底层的异步删除机制
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static bool DelFileWithCmd(string filePath)
        {
            var process = new Process();
            //ProcessStartInfo构造方法的第二个参数是执行第一个命令的所需要传入的参数，以空格分隔各个参数
            var processStartInfo = new ProcessStartInfo("CMD.EXE", "/C del /F /S /Q \"" + filePath + "\"") { UseShellExecute = false, RedirectStandardOutput = true };
            process.StartInfo = processStartInfo;
            process.Start();
            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
            if (output.Contains(filePath))
                return true;
            return false;
        }

        /// <summary>
        /// 递归获取磁盘上的指定目录下所有文件的集合，返回类型是：字典[文件名，要压缩的相对文件名]
        /// </summary>
        /// <param name="strBaseDir"></param>
        /// <param name="includeBaseDirectory"></param>
        /// <param name="namePrefix"></param>
        /// <returns></returns>
        private static Dictionary<string, string> GetAllDirList(string strBaseDir, bool includeBaseDirectory = false, string namePrefix = "")
        {
            var resultDictionary = new Dictionary<string, string>();
            var directoryInfo = new DirectoryInfo(strBaseDir);
            var directories = directoryInfo.GetDirectories();
            var fileInfos = directoryInfo.GetFiles();
            if (includeBaseDirectory)
                namePrefix += directoryInfo.Name + "\\";
            foreach (var directory in directories)
                resultDictionary = resultDictionary.Concat(GetAllDirList(directory.FullName, true, namePrefix)).ToDictionary(k => k.Key, k => k.Value); //FullName是某个子目录的绝对地址，
            foreach (var fileInfo in fileInfos)
                if (!resultDictionary.ContainsKey(fileInfo.FullName))
                    resultDictionary.Add(fileInfo.FullName, namePrefix + fileInfo.Name);
            return resultDictionary;
        }

        /// <summary>
        /// 解压Zip文件，并覆盖保存到指定的目标路径文件夹下
        /// </summary>
        /// <param name="zipFilePath">将要解压缩的zip文件的路径</param>
        /// <param name="unZipDir">解压后将zip中的文件存储到磁盘的目标路径</param>
        /// <returns></returns>
        public static bool UnZip(string zipFilePath, string unZipDir)
        {
            bool resualt;
            try
            {
                unZipDir = unZipDir.EndsWith(@"\") ? unZipDir : unZipDir + @"\";
                var directoryInfo = new DirectoryInfo(unZipDir);
                if (!directoryInfo.Exists)
                    directoryInfo.Create();
                var fileInfo = new FileInfo(zipFilePath);
                if (!fileInfo.Exists)
                    return false;
                using (var zipToOpen = new FileStream(zipFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                {
                    using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                    {
                        foreach (var zipArchiveEntry in archive.Entries)
                            if (!zipArchiveEntry.FullName.EndsWith("/"))
                            {
                                var entryFilePath = Regex.Replace(zipArchiveEntry.FullName.Replace("/", @"\"), @"^\\*", "");
                                var filePath = directoryInfo + entryFilePath; //设置解压路径
                                var content = new byte[zipArchiveEntry.Length];
                                zipArchiveEntry.Open().Read(content, 0, content.Length);

                                // mq 2019-06-14: 注销下面的代码，全部覆盖现有文件
                                //if (File.Exists(filePath) && content.Length == new FileInfo(filePath).Length)
                                //{
                                //    continue; //跳过相同的文件，否则覆盖更新
                                //}
                                //var sameDirectoryNameFilePath = new DirectoryInfo(filePath);
                                //if (sameDirectoryNameFilePath.Exists)
                                //{
                                //    sameDirectoryNameFilePath.Delete(true);
                                //    DeleteDirectoryWithCmd(filePath);
                                //}
                                //var sameFileNameFilePath = new FileInfo(filePath);
                                //if (sameFileNameFilePath.Exists)
                                //{
                                //    sameFileNameFilePath.Delete();
                                //    DelFileWithCmd(filePath);
                                //    /*if (!DelFileWithCmd(filePath))
                                //    {
                                //        Console.WriteLine(filePath + "删除失败");
                                //        resualt = false;
                                //        break;
                                //    }*/
                                //}

                                var greatFolder = Directory.GetParent(filePath);
                                if (!greatFolder.Exists)
                                    greatFolder.Create();
                                System.IO.File.WriteAllBytes(filePath, content);
                            }
                    }
                }
                resualt = true;
            }
            catch (Exception ex)
            {
                //LogHelper.Error("Error! ", ex);
                resualt = false;
            }
            return resualt;
        }

        /// <summary>
        /// 获取Zip压缩包中的文件列表
        /// </summary>
        /// <param name="zipFilePath">Zip压缩包文件的物理路径</param>
        /// <returns></returns>
        public static List<string> GetZipFileList(string zipFilePath)
        {
            List<string> fList = new List<string>();
            if (!System.IO.File.Exists(zipFilePath))
                return fList;
            try
            {
                using (var zipToOpen = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                    {
                        foreach (var zipArchiveEntry in archive.Entries)
                            if (!zipArchiveEntry.FullName.EndsWith("/"))
                                fList.Add(Regex.Replace(zipArchiveEntry.FullName.Replace("/", @"\"), @"^\\*", ""));
                    }
                }
            }
            catch (Exception ex)
            {
                //LogHelper.Error("Error! ", ex);
            }
            return fList;
        }


        #endregion
    }
}
