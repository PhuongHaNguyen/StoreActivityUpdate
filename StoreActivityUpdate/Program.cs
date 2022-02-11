using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StoreActivityUpdate
{
    class Program
    {
        private static ServiceController service = null;
        static void Main(string[] args)
        {
            //String ServicesName = "AxInstSV";
            //String SourcePath = @"D:\HaNguyen\Source2";
            //String TargetPath = @"D:\HaNguyen\Target2";
            //String VersionUpgrade = "2.0";

            String ServicesName = string.Empty;
            String SourcePath = string.Empty;
            String TargetPath = string.Empty;
            String VersionUpgrade = string.Empty;
            Log log = new Log();
            try
            {

                if (args == null || args.Length == 0)
                {
                    log.logError("ServicesNamem,SourcePath,TargetPath is null");
                    return;
                }
                if (!String.IsNullOrEmpty(args[0]))
                {
                    ServicesName = args[0];
                }
                log.logInfo("ServicesName:" + ServicesName);
                if (!String.IsNullOrEmpty(args[1]))
                {
                    SourcePath = args[1];
                }
                log.logInfo("SourcePath:" + SourcePath);
                if (!String.IsNullOrEmpty(args[2]))
                {
                    TargetPath = args[2];
                }
                log.logInfo("TargerPath:" + TargetPath);
                if (!String.IsNullOrEmpty(args[3]))
                {
                    VersionUpgrade = args[3];
                }
                log.logInfo("VersionUpgrade:" + VersionUpgrade);

                // check service exist
                if (!checkServiceExists(ServicesName))
                {
                    log.logError("Services " + ServicesName + " not found");
                    return;
                }
                service = new ServiceController(ServicesName);

                if (service.Status.Equals(ServiceControllerStatus.Running))
                {
                    service.Stop();
                    log.logInfo("Stop services success");
                }
                if (System.IO.Directory.Exists(SourcePath))
                {

                    // Now Create all of the directories
                    foreach (string dirPath in Directory.GetDirectories(SourcePath, "*", SearchOption.AllDirectories))
                    {
                        Directory.CreateDirectory(dirPath.Replace(SourcePath, TargetPath));
                    }
                    // Copy all the files & Replaces any files with the same name
                    foreach (string newPath in Directory.GetFiles(SourcePath, "*.*", SearchOption.AllDirectories))
                    {
                        File.Copy(newPath, newPath.Replace(SourcePath, TargetPath), true);
                    }

                    string[] fileEntries = Directory.GetFiles(TargetPath);
                    foreach (string fileName in fileEntries)
                    {
                        // check extension & unzip
                        if (".zip".Equals(Path.GetExtension(fileName)))
                        {
                            UnzipFile(TargetPath, fileName);
                        }
                    }
                }
                else
                {
                    log.logInfo("Source path " + SourcePath + " does not exist!");
                    log.logInfo("Process fails. Press any key to exit.");
                }
                service = new ServiceController(ServicesName);
                if ((service.Status.Equals(ServiceControllerStatus.Stopped)))
                {
                    service.Start();
                    log.logInfo("Start services success");
                    writeUpgradeVersionToFile(TargetPath, VersionUpgrade,"OK");
                }
                log.logInfo("Process success.");
            }
            catch (Exception ex)
            {
                if ((service.Status.Equals(ServiceControllerStatus.Stopped)))
                {
                    service.Start();
                }
                writeUpgradeVersionToFile(TargetPath, VersionUpgrade, "ERROR");
                log.logError("Error Exception:" + ex.Message);
                log.logError("Exception full stack:" + ex.ToString());
                log.logError("Process fails.");
            }
        }

        public static void writeUpgradeVersionToFile(string folderSaveFile, string VersionUpgrade, string statusUpgrade)
        {
            var obj = new
            {
                currentVersion = VersionUpgrade,
                status = statusUpgrade
            };
            var json = JsonConvert.SerializeObject(obj);
            string NameFile = "UpgradeVersion.json";
            string fullPath = folderSaveFile + "\\" + NameFile;
            File.WriteAllText(fullPath, json);
        }

        public static void UnzipFile(String pathUnzip, String pathFile)
        {
            FastZip fastZip = new FastZip();
            // = null => Will always overwrite if target filenames already exist
            fastZip.ExtractZip(pathFile, pathUnzip, null);
        }

        public static bool checkServiceExists(string ServiceName)
        {
            return ServiceController.GetServices().Any(serviceController => serviceController.ServiceName.Equals(ServiceName));
        }
    }
}
