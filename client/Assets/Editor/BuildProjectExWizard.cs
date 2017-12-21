using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using UnityEditor;
using UnityEngine;
using Mogo.Util;
using System.Security.Cryptography;
using Debug = UnityEngine.Debug;

public class BuildProjectExWizard : EditorWindow
{
    public const string ExportFilesPath = "/ExportedFiles";//不能直接在这里后面加斜杠，build assetbundle用到这个参数，打包场景文件的时候会导致导出失败
    public Vector2 scrollPosition;
    public string m_currentVersion = "0.0.0.0";
    public string m_newVersion = "0.0.0.1";
    public string m_newVersionFolder;
    private bool m_selectAll = true;
    /// <summary>
    /// 导出资源配置数据。
    /// </summary>
    private List<BuildResourcesInfo> m_buildResourcesInfoList;
    /// <summary>
    /// 拷贝资源配置数据。
    /// </summary>
    private List<CopyResourcesInfo> m_copyResourcesInfoList;
    /// <summary>
    /// 当前版本中资源的版本信息。
    /// </summary>
    private Dictionary<string, VersionInfo> m_fileVersions
    {
        get
        {
            return ExportScenesManager.FileVersions;
        }
        set
        {
            ExportScenesManager.FileVersions = value;
        }
    }
    /// <summary>
    /// 存储有更新的文件。
    /// </summary>
    private Dictionary<string, VersionInfo> m_updatedFiles = new Dictionary<string, VersionInfo>();

    [MenuItem("MogoEx/Build Resources")]
    public static void ShowWindow()
    {
        var wizard = EditorWindow.GetWindow(typeof(BuildProjectExWizard)) as BuildProjectExWizard;
        ExportScenesManager.AutoSwitchTarget();
        wizard.m_buildResourcesInfoList = ExportScenesManager.LoadBuildResourcesInfo();
        wizard.m_copyResourcesInfoList = ExportScenesManager.LoadCopyResourcesInfo();
        if (wizard.m_buildResourcesInfoList == null || wizard.m_copyResourcesInfoList == null)
            return;
        var currentVersion = GetNewVersionCode();
        wizard.m_currentVersion = currentVersion.GetLowerVersion();
        wizard.m_newVersion = currentVersion.ToString();
        wizard.m_newVersionFolder = GetVersionFolder(wizard.m_newVersion);
    }

    public static void CopyResources(string exportRootPath)
    {
        var copyResourcesInfoList = ExportScenesManager.LoadCopyResourcesInfo();
        if (copyResourcesInfoList == null)
            return;
        foreach (var item in copyResourcesInfoList)
        {
            if (item.check)
            {
                ExportScenesManager.CopyFolder(Path.Combine(exportRootPath, item.targetPath), Application.dataPath + item.sourcePath, item.extention);
            }
        }
    }

    [MenuItem("Assetbundle/Build All")]
    public static void ConsoleBuildAll()
    {
        ConsoleBuild();

    }

    [MenuItem("Assetbundle/Build")]
    public static void ConsoleBuild()
    {
        ExportScenesManager.InitMsg();
        ExportScenesManager.AutoSwitchTarget();
        var buildResourcesInfoList = ExportScenesManager.LoadBuildResourcesInfo();
        if (buildResourcesInfoList == null)
            return;

        var newVersion = GetNewVersion();
        var newVersionFolder = GetVersionFolder(newVersion);
        var root = Application.dataPath;
        var dataPath = new DirectoryInfo(Application.dataPath).Parent.FullName.Replace("\\", "/") + "/";
        BuildAssetVersion(root, dataPath, newVersion, newVersionFolder);

        //var sw = new Stopwatch();
        //sw.Start();
        var exportRootPath = newVersionFolder + ExportFilesPath;
        foreach (var item in buildResourcesInfoList)
        {
            if (item.check)
            {
                if (item.packLevel != null && item.packLevel.Length > 0)
                {
                    ExportScenesManager.LogDebug(item.packLevel.PackArray());
                    ResourceManager.PackedExportableFileTypes = item.packLevel;
                }
                BuildAssetBundleMainAsset(item, exportRootPath, item.isMerge > 0 ? true : false);
            }
        }
        CopyResources(exportRootPath);
        //sw.Stop();
        //ExportScenesManager.LogDebug("完整打包 time: " + sw.ElapsedMilliseconds);
        var path = ExportScenesManager.GetFolderPath() + "//ConsoleBuildTime.txt";
        //Mogo.Util.XMLParser.SaveText(path.Replace("\\", "/"), "完整打包 time: " + sw.ElapsedMilliseconds);
        ExportScenesManager.CloseMsg();
        //ShutDown.PowerOff();
    }

    [MenuItem("Assetbundle/Switch2IosTarget")]
    public static void Switch2IosTarget()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.iOS);
    }
    [MenuItem("Assetbundle/Switch2AndroidTarget")]
    public static void Switch2AndroidTarget()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.Android);
    }
    public static void ConsoleBuildByIndex(int index)
    {
        ExportScenesManager.InitMsg();
        ExportScenesManager.AutoSwitchTarget();
        var buildResourcesInfoList = ExportScenesManager.LoadBuildResourcesInfo(index);
        if (buildResourcesInfoList == null)
            return;
        CreateUpVersionFolder();
        var newVersion = GetNewVersion();
        var newVersionFolder = GetVersionFolder(newVersion);
        var root = Application.dataPath;
        var dataPath = new DirectoryInfo(Application.dataPath).Parent.FullName.Replace("\\", "/") + "/";
        BuildAssetVersion(root, dataPath, newVersion, newVersionFolder);

        var exportRootPath = newVersionFolder + ExportFilesPath;
        foreach (var item in buildResourcesInfoList)
        {
            if (item.check)
            {
                if (item.packLevel != null && item.packLevel.Length > 0)
                {
                    ExportScenesManager.LogDebug(item.packLevel.PackArray());
                    ResourceManager.PackedExportableFileTypes = item.packLevel;
                }
                BuildAssetBundleMainAsset(item, exportRootPath, item.isMerge > 0 ? true : false);
            }
        }
        CopyResources(exportRootPath);
        ExportScenesManager.CloseMsg();
    }

    public class CopyScrDst
    {
        public CopyScrDst(string _src, string _dst, bool _bsub)
        {
            SrcDir = _src;
            DstDir = _dst;
            CopySubDir = _bsub;
        }
        public string SrcDir;
        public string DstDir;
        public bool CopySubDir = false;
    }
    #region 新的资源打包，多进程打包
    [MenuItem("Assetbundle/NewConsoleBuild")]
    public static void NewConsoleBuild()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        /*把client项目拷贝四份到同目录下，名字叫“原项目文件夹名_temp1”“原项目文件夹名_temp2”“原项目文件夹名_temp3”“原项目文件夹名_temp4”
         * 然后启动四个unity分别打包不同资源，并行的启动四个unity,
         * 主unity会每隔15秒检查一下打包进程是否存在，如果四个进程都不在了都表示所有任务完成
         * 全部打包完成后拷贝对应资源回到主client项目
        */
        CreateUpVersionFolder();
        //const string taskmgr = @"C:\WINDOWS\system32\taskmgr.exe";
        //Process.Start(taskmgr);
        #region 拷贝四份，拷贝完成的项目启动打包
        //client项目的目录
        string clientDir = new DirectoryInfo(Application.dataPath).Parent.FullName.Replace("\\", "/");
        //四个对应的临时项目文件夹
        List<string> tempClientDirs = new List<string>() {
            clientDir + "_temp1", 
            clientDir + "_temp2", 
            clientDir + "_temp3", 
            clientDir + "_temp4" };
        //主client文件中需要拷贝的文件夹列表 
        List<CopyScrDst> copyList = new List<CopyScrDst>();
        copyList.Add(new CopyScrDst(clientDir, "", false));
        copyList.Add(new CopyScrDst(clientDir + "/Assets", "/Assets", true));
        copyList.Add(new CopyScrDst(clientDir + "/Library", "/Library", true));
        copyList.Add(new CopyScrDst(clientDir + "/ProjectSettings", "/ProjectSettings", true));
        copyList.Add(new CopyScrDst(clientDir + "/ResourceDef", "/ResourceDef", true));
        copyList.Add(new CopyScrDst(clientDir + "/Shader_PC", "/Shader_PC", true));

        //一个临时项目文件拷贝完成就启动对应的批处理
        List<string> buildList = new List<string>() { 
            tempClientDirs[0] + "/r1.bat", 
            tempClientDirs[1] + "/r2.bat",
            tempClientDirs[2] + "/r3.bat", 
            tempClientDirs[3] + "/r4.bat" };
        //进程和对应的索引，就是对应项目的索引1-4，方便统计各个项目打包的时间
        Dictionary<Process, int> processes = new Dictionary<Process, int>();

        for (int i = 0; i < 4; i++)
        {
            string tempDir = tempClientDirs[i];

            if (Directory.Exists(tempDir))
            {
                //如果已经有文件夹了，暂时不拷贝，以后改为从新拷贝,只拷贝Assets/Editor
                CopyScrDst csd = new CopyScrDst(clientDir + "/Assets/Editor", "/Assets/Editor", true);
                ExportScenesManager.DirectoryCopy(csd.SrcDir, tempDir + csd.DstDir, csd.CopySubDir, true);
                ExportScenesManager.LogDebug("启动进程：" + buildList[i]);
                processes.Add(Process.Start(buildList[i]), i);
                System.Threading.Thread.Sleep(15 * 1000);
            }
            else
            {
                ExportScenesManager.LogDebug("创建临时项目文件夹：" + tempDir);
                foreach (var item in copyList)
                {
                    ExportScenesManager.DirectoryCopy(item.SrcDir, tempDir + item.DstDir, item.CopySubDir, true);
                }
                ExportScenesManager.LogDebug("临时项目文件夹拷贝完成,拷贝时间：" + sw.ElapsedMilliseconds / (1000 * 60));

                //启动这个临时项目的打包批处理
                ExportScenesManager.LogDebug("启动进程：" + buildList[i]);
                processes.Add(Process.Start(buildList[i]), i);
                System.Threading.Thread.Sleep(15 * 1000);
            }
        }
        #endregion
        #region 等待四个打包进程完成
        ExportScenesManager.LogDebug("文件拷贝总时间：" + sw.ElapsedMilliseconds / (1000 * 60));
        //循环检查是否完成，直到最终完成
        while (true)
        {
            if (processes.Count == 0) break;
            foreach (var proc in processes)
            {
                if (proc.Key.HasExited)
                {
                    ExportScenesManager.LogDebug("项目" + proc.Value + "完成，时间：" + sw.ElapsedMilliseconds / (1000 * 60));
                    processes.Remove(proc.Key);
                }
                else
                {
                    System.Threading.Thread.Sleep(10 * 1000);
                }
                break;
            }
        }
        ExportScenesManager.LogDebug("打包完成总时间：" + sw.ElapsedMilliseconds / (1000 * 60));
        #endregion
        #region  拷贝打包好的资源回主项目
        //打包资源全部完成，拷贝对应资源到主client项目
        //收集所有需要拷贝回主client的资源文件
        //key源文件，value是目标文件
        Dictionary<string, string> allSrc2DstAssets = new Dictionary<string, string>();
        foreach (var item in tempClientDirs)
        {
            var newVersionFolder = item + "/Export/" + GetNewVersionByTempClientPath(item + "/Export");
            var fileList = Directory.GetFiles(newVersionFolder, "*.*", SearchOption.AllDirectories);
            foreach (var s in fileList)
            {
                var srcFile = s.Replace("\\", "/");
                var relateFile = srcFile.Replace(newVersionFolder, "");
                var dstFile = clientDir + "/Export/" + GetNewVersionByTempClientPath(clientDir + "/Export") + relateFile;
                allSrc2DstAssets.Add(srcFile, dstFile);
            }
        }
        //是否使用MD5来验证新文件和原文件是不是相同的
        bool bUseMd5 = false;
        ExportScenesManager.LogDebug("拷贝的总数：" + allSrc2DstAssets.Count);
        foreach (var src2dst in allSrc2DstAssets)
        {
            //ExportScenesManager.LogDebug("源路径："+src2dst.Key+"目标路径："+src2dst.Value);
            if (File.Exists(src2dst.Key))
            {
                //获得目标文件夹路径
                var dstDir = src2dst.Value.Substring(0, src2dst.Value.LastIndexOf("/"));
                if (!Directory.Exists(dstDir))
                {
                    ExportScenesManager.LogDebug("创建文件夹：" + dstDir);
                    Directory.CreateDirectory(dstDir);
                }
                if (!bUseMd5)
                    File.Copy(src2dst.Key, src2dst.Value, true);
                else
                {
                    string srcMd5 = null, dstMd5 = null;
                    srcMd5 = Utils.BuildFileMd5(src2dst.Key);
                    if (File.Exists(src2dst.Value))
                        dstMd5 = Utils.BuildFileMd5(src2dst.Value);
                    if (srcMd5 != null && dstMd5 != null && srcMd5 != dstMd5)
                    {
                        ExportScenesManager.LogError("拷贝Md5不一致：" + src2dst.Value);
                    }
                }
            }
            else
            {
                ExportScenesManager.LogError("原文件不存在：" + src2dst.Key);
            }
        }
        ExportScenesManager.LogDebug("拷贝总时间：" + sw.ElapsedMilliseconds / (1000 * 60));
        #endregion
        sw.Stop();
    }
    //获得被拷贝出去的临时项目路径export下的最新导出版本,比如client_Temp1/Export/0.0.0.3
    public static string GetNewVersionByTempClientPath(string path)
    {
        var dirs = Directory.GetDirectories(path);
        var currentVersion = new VersionCodeInfo("0.0.0.1");
        foreach (var item in dirs)
        {
            var fileVersion = new VersionCodeInfo(new DirectoryInfo(item).Name);
            if (fileVersion.Compare(currentVersion) > 0)
                currentVersion = fileVersion;
        }
        return currentVersion.ToString();
    }

    //获得需要被导出资源，返回值第一个是绝对路径，第二个是相对路径
    public static Dictionary<string, string> GetAssetDictByPath(BuildResourcesInfo item, string rootdir)
    {
        var newVersion = GetNewVersionByTempClientPath(rootdir + "/Export");
        string ExportDir = rootdir + "/Export/" + newVersion + "/ExportedFiles";
        Dictionary<string, string> dict = new Dictionary<string, string>();
        foreach (var folder in item.folders)
        {
            var path = ExportDir + "/" + folder.path;
            //ExportScenesManager.LogDebug("获取原路径下文件：" + path);
            if (!Directory.Exists(path))
            {
                ExportScenesManager.LogError("源文件夹不存在：" + path);
                continue;
            }
            string[] uFiles = Directory.GetFiles(path, "*.u", folder.deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            string[] xmlFiles = Directory.GetFiles(path, "*.xml", folder.deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            List<string> files = new List<string>();
            files.AddRange(uFiles);
            files.AddRange(xmlFiles);
            foreach (var file in files)
            {
                string filePath = file.Replace("\\", "/");
                dict.Add(filePath, folder.path);
            }
        }
        return dict;
    }

    public static void ConsoleBuild1()
    {
        ConsoleBuildByIndex(1);
    }

    public static void ConsoleBuild2()
    {
        ConsoleBuildByIndex(2);
    }

    public static void ConsoleBuild3()
    {
        ConsoleBuildByIndex(3);
    }

    public static void ConsoleBuild4()
    {
        ConsoleBuildByIndex(4);
    }

    #endregion

    [MenuItem("Assetbundle/Cut FirstExport To pkg dir")]
    public static void CutFirstExportToPkgDir()
    {
        //把Export/0.0.0.3/ExportedFiles中的meta.xml、meta中对应的资源、data中的文件、mogores剪切到pkg文件夹
        //收集文件列表，以Export/0.0.0.3最为根路径，保存的是相对路径
        List<string> cutList = new List<string>();
        cutList.Add(ResourceManager.MetaFileName);
        cutList.Add("MogoRes");
        //根目录
        var newVersionCode = GetNewVersionCode();
        var newVersion = newVersionCode.ToString();
        var newVersionFolder = GetVersionFolder(newVersion);
        var rootPath = newVersionFolder + "/ExportedFiles";
        //收集meta中对应的文件
        var metapath = rootPath + "/Meta.xml";
        var children = XMLParser.LoadXML(FileAccessManager.LoadText(metapath));
        foreach (SecurityElement item in children.Children)
        {
            string path = item.Attribute("path");
            if (path != null)
                cutList.Add(path);
            else
                Debug.LogError("Path not exit in Meta.xml");
        }
        //收集data目录下的文件
        var mogoResFiles = Directory.GetFiles(rootPath + "/data", "*.*", SearchOption.AllDirectories);
        var mogoResList = (from f in mogoResFiles
                           let file = f.ReplaceFirst(rootPath, "").Replace("\\", "/").ReplaceFirst("/", "")
                           select file
                          ).ToList();

        cutList.AddRange(mogoResList);
        foreach (string item in cutList)
        {
            Debug.Log(item);
        }
        //目标目录
        var targetPkgDir = rootPath + "/../pkgtemp/";
        if (!Directory.Exists(targetPkgDir)) Directory.CreateDirectory(targetPkgDir);
        foreach (string filename in cutList)
        {
            var targetFile = targetPkgDir + filename;
            var targetDir = targetFile.Substring(0, targetFile.LastIndexOf("/"));
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);
            if (File.Exists(targetFile)) File.Delete(targetFile);
            if (File.Exists(rootPath + "/" + filename))
                File.Move(rootPath + "/" + filename, targetFile);
        }
    }

    [MenuItem("Assetbundle/Copy pkg to FirstExport")]
    public static void CutPkgToFirstExport()
    {
        //恢复数据到原来的位置
        List<string> cutList = new List<string>();
        //从pkgtemp目录拷贝回ExportedFiles
        //根目录
        var newVersionCode = GetNewVersionCode();
        var newVersion = newVersionCode.ToString();
        var newVersionFolder = GetVersionFolder(newVersion);
        var ExportedFilesPath = newVersionFolder + "/ExportedFiles";
        var pkgtemPath = newVersionFolder + "/pkgtemp";
        //搜索pkgtemp中所有文件
        var mogoResFiles = Directory.GetFiles(pkgtemPath, "*.*", SearchOption.AllDirectories);
        var mogoResList = (from f in mogoResFiles
                           let file = f.ReplaceFirst(pkgtemPath, "").Replace("\\", "/").ReplaceFirst("/", "")
                           select file
                          ).ToList();
        cutList.AddRange(mogoResList);
        foreach (string item in mogoResList)
        {
            string srcFile = pkgtemPath + "/" + item;
            string dstFile = ExportedFilesPath + "/" + item;

            var dstDir = dstFile.Substring(0, dstFile.LastIndexOf("/"));
            if (!Directory.Exists(dstDir))
                Directory.CreateDirectory(dstDir);
            if (!File.Exists(dstFile))
                File.Copy(srcFile, dstFile);
        }

    }
    [MenuItem("Assetbundle/Build File Index")]
    public static void ConsoleBuildFileIndex()
    {
        var newVersionCode = GetNewVersionCode();
        var newVersion = newVersionCode.ToString();
        var newVersionFolder = GetVersionFolder(newVersion);
        var targetPath = newVersionFolder + ExportFilesPath;
        ExportScenesManager.BuildFileIndexInfo(targetPath.Replace("\\", "/"), targetPath);
    }
    [MenuItem("Assetbundle/Build Pkg File Index")]
    public static void ConsoleBuildPkgFileIndex()
    {
        var newVersionCode = GetNewVersionCode();
        var newVersion = newVersionCode.ToString();
        var newVersionFolder = GetVersionFolder(newVersion);
        var targetPath = newVersionFolder + ExportFilesPath;
        ExportScenesManager.BuildFileIndexInfo(targetPath.Replace("\\", "/"), targetPath, false);
    }

    public static void ConsoleAdditiveBuild()
    {
        ExportScenesManager.InitMsg();
        ExportScenesManager.AutoSwitchTarget();
        var sw = new Stopwatch();
        sw.Start();
        var newVersionCode = GetNewVersionCode();
        var newVersion = newVersionCode.ToString();
        var newVersionFolder = GetVersionFolder(newVersion);
        var root = Application.dataPath;
        var dataPath = new DirectoryInfo(Application.dataPath).Parent.FullName.Replace("\\", "/") + "/";
        BuildAssetVersion(root, dataPath, newVersion, newVersionFolder);
        var diff = FindVersionDiff(GetVersionFolder(newVersionCode.GetLowerVersion()), newVersionFolder, "version");
        var path = ExportScenesManager.GetFolderPath() + "//diffList.txt";
        Mogo.Util.XMLParser.SaveText(path.Replace("\\", "/"), diff.PackArray('\n'));
        BuildAssetRef();
        var hashSet = new HashSet<string>();
        foreach (var item in BundleExporter.dependenciesDic)//先遍历获取所有有依赖更新资源的资源
        {
            foreach (var d in diff)
            {
                if (item.Value.Contains(d) || d == item.Key)
                {
                    hashSet.Add(item.Key);
                }
            }
        }
        var targets = new HashSet<string>();
        foreach (var hs in hashSet)//再次遍历所有取出的资源，排除掉非根资源
        {
            var isTarget = true;
            foreach (var item in BundleExporter.dependenciesDic)
            {
                if (item.Value.Contains(hs))
                {
                    isTarget = false;
                    break;
                }
            }
            if (isTarget)
                targets.Add(hs);
        }
        path = ExportScenesManager.GetFolderPath() + "//targetFiles.txt";
        Mogo.Util.XMLParser.SaveText(path.Replace("\\", "/"), targets.ToList().PackList('\n'));

        var targetPath = newVersionFolder + ExportFilesPath;
        BundleExporter.BuildBundleWithRoot(targets.ToArray(), targetPath);//导出更新文件

        var buildResourcesInfoList = ExportScenesManager.LoadBuildResourcesInfo();
        if (buildResourcesInfoList != null)
            BuildAssetBundleMainAsset(buildResourcesInfoList.First(t => t.name == "UI"), targetPath);//导出UI

        CopyResources(targetPath);//导出配置
        sw.Stop();
        ExportScenesManager.LogDebug("ConsoleAdditiveBuild: " + sw.ElapsedMilliseconds);
        ExportScenesManager.CloseMsg();
    }

    [MenuItem("Assetbundle/UpVersion")]
    public static void UpVersion()
    {
        ExportScenesManager.InitMsg();
        //var sw = new Stopwatch();
        //sw.Start();
        BundleExporter.ClearDependenciesDic();
        var currentVersionCode = GetNewVersionCode();
        var currentVersion = currentVersionCode.ToString();
        var newVersion = currentVersionCode.GetUpperVersion();
        var curVersionFolder = GetVersionFolder(currentVersion) + ExportFilesPath;
        var newVersionFolder = GetVersionFolder(newVersion) + ExportFilesPath;
        ExportScenesManager.DirectoryCopy(curVersionFolder, newVersionFolder, true);
        //sw.Stop();
        //ExportScenesManager.LogDebug("UpVersion time: " + sw.ElapsedMilliseconds);
        ExportScenesManager.CloseMsg();
    }

    [MenuItem("Assetbundle/Create Up Version Folder")]
    public static void CreateUpVersionFolder()
    {
        BundleExporter.ClearDependenciesDic();
        var currentVersionCode = GetNewVersionCode();
        //var currentVersion = currentVersionCode.ToString();
        var newVersion = currentVersionCode.GetUpperVersion();
        //var curVersionFolder = GetVersionFolder(currentVersion) + ExportFilesPath;
        var newVersionFolder = GetVersionFolder(newVersion) + ExportFilesPath;
        Directory.CreateDirectory(newVersionFolder);
    }

    public static void ConsoleAutoAdditiveBuild()
    {
        UpVersion();
        ConsoleAdditiveBuild();
    }

    [MenuItem("Assetbundle/Build Hmf")]
    public static void ConsoleBuildHmf()
    {
        var tool = Application.dataPath + "/../hmf/tool.py";
        var newVersionCode = GetNewVersionCode();
        var newVersion = newVersionCode.ToString();
        var newVersionFolder = GetVersionFolder(newVersion);
        var targetPath = newVersionFolder + ExportFilesPath;
        //ExportScenesManager.LogDebug(tool);
        if (!Directory.Exists(targetPath + "/data/xml"))
            Directory.CreateDirectory(targetPath + "/data/xml");
        var para = Application.dataPath + "/Resources/data/xml/" + " " + targetPath + "/data/xml/  E:/  E:/";
        ExportScenesManager.LogDebug(para);
        System.Diagnostics.Process.Start(tool, para);
        //string str2 = process.StandardOutput.ReadToEnd();var process = 
        //ExportScenesManager.LogDebug(str2);
    }

    [MenuItem("Assetbundle/Pack Updated Files")]
    public static void ConsolePackUpdatedFiles()
    {
        var newVersionCode = GetNewVersionCode();
        var newVersion = newVersionCode.ToString();
        var newVersionFolder = GetVersionFolder(newVersion);
        BuildExportedFilesVersion(newVersionCode.GetLowerVersion());
        BuildExportedFilesVersion(newVersionCode.ToString());

        var diff = FindVersionDiff(GetVersionFolder(newVersionCode.GetLowerVersion()), newVersionFolder, "ExportFileVersion");


        var targetPath = newVersionFolder + ExportFilesPath;

        var diffFiles = from d in diff
                        let df = String.Concat(targetPath, d)
                        select df;
        var tempExport = ExportScenesManager.GetFolderPath("tempExport");
        PackUpdatedFiles(targetPath, tempExport, newVersionFolder, diffFiles.ToList(), newVersionCode.GetLowerVersion(), newVersion);
    }

    [MenuItem("Assetbundle/Build Complete Pkg")]
    public static void ConsoleBuildCompletePkg()
    {
        var newVersionCode = GetNewVersionCode();
        var newVersion = newVersionCode.ToString();
        var newVersionFolder = GetVersionFolder(newVersion);
        var targetPath = newVersionFolder + ExportFilesPath;
        var fileName = VersionManager.Instance.GetPackageName("0.0.0.0", newVersion);
        ExportScenesManager.ZIPFileWithFileName(targetPath, newVersionFolder, fileName, 0);
        var mogoFileName = Path.Combine(newVersionFolder, MogoFileSystem.FILE_NAME);
        MogoFileSystem.Instance.FileFullName = mogoFileName;
        MogoFileSystem.Instance.Init(mogoFileName);

        ExportScenesManager.LogDebug(newVersionFolder + "/" + fileName);
        Utils.DecompressToMogoFile(newVersionFolder + "/" + fileName);
        MogoFileSystem.Instance.Close();
        ExportScenesManager.LogDebug("Build Complete Pkg finish.");
    }

    [MenuItem("Assetbundle/Build First Export")]
    public static void BuildFirstExport()
    {
        //目标文件夹，也是最新版本导出目录
        var newVersionCode = GetNewVersionCode();
        var newVersion = newVersionCode.ToString();
        var newVersionFolder = GetVersionFolder(newVersion);
        var targetPath = newVersionFolder + "/";
        var srcPath = newVersionFolder + ExportFilesPath + "/";
        //需要拷贝内容包括，ExportedFiles目录下的meta.xml、meta.xml中对应的文件、data，mogores
        List<string> firstExportList = new List<string>();
        firstExportList.Add(ResourceManager.MetaFileName);
#if !UNITY_IPHONE
        firstExportList.Add(Driver.FileName);
        var index = 0;
        var dllName = Driver.FileName + index;
        while (File.Exists(srcPath + dllName))
        {
            firstExportList.Add(dllName);
            index++;
            dllName = Driver.FileName + index;
        }

#endif
        //源目录0.0.0.2/ExportedFiles,目标路径是0.0.0.2
        var metapath = srcPath + ResourceManager.MetaFileName;
        var root = XMLParser.LoadXML(Utils.LoadFile(metapath));
        Debug.Log(metapath);
        foreach (SecurityElement item in root.Children)
        {
            string path = item.Attribute("path");
            if (path != null)
                firstExportList.Add(path);
            else
                Debug.LogError("Path not exit in Meta.xml");
        }
        var mogoResFiles = Directory.GetFiles(srcPath + "data", "*.*", SearchOption.AllDirectories);
        var mogoResList = (from f in mogoResFiles
                           let file = f.ReplaceFirst(srcPath, "").Replace("\\", "/")
                           select file
                          ).ToList();
        firstExportList.AddRange(mogoResList);
        Debug.Log("需要拷贝的数量：" + firstExportList.Count);
        //根据列表收集文件到一个临时文件夹
        var tempDir = targetPath + "FirstExportFiles/";
        if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
        foreach (string filename in firstExportList)
        {
            var targetFile = tempDir + filename;
            var targetDir = targetFile.Substring(0, targetFile.LastIndexOf("/"));
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
            if (File.Exists(targetFile)) File.Delete(targetFile);
            if (File.Exists(srcPath + filename))
                File.Copy(srcPath + filename, targetFile);
        }
        //生成一个pkg文件
        var zipPath = targetPath + "firstExport.zip";
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        ExportScenesManager.ZIPFileWithFileName(tempDir, targetPath, "firstExport.zip");
        var firstExportPkg = targetPath + MogoFileSystem.FILE_NAME;
        if (File.Exists(firstExportPkg)) File.Delete(firstExportPkg);
        MogoFileSystem.Instance.FileFullName = firstExportPkg;
        MogoFileSystem.Instance.Init(firstExportPkg);
        Utils.DecompressToMogoFile(zipPath);
        MogoFileSystem.Instance.Close();
        //删除临时文件
        Directory.Delete(tempDir, true);
    }
    [MenuItem("Assetbundle/Copy Resources To MogoResources")]
    public static void CopyResourcesToMogoResources()
    {
        var newVersionCode = GetNewVersionCode();
        var newVersion = newVersionCode.ToString();
        var newVersionFolder = GetVersionFolder(newVersion);
        var targetPath = newVersionFolder + ExportFilesPath;
        ExportScenesManager.DirectoryCopy(targetPath, ExportScenesManager.GetFolderPath(ExportScenesManager.SubMogoResources), true, true);
    }

    [MenuItem("Assetbundle/Copy Script For IOS Publish")]
    public static void CopyScript2IosPublish()
    {
        string dstDir = "../publish/Assets/Scripts";
        string root = (new DirectoryInfo(Application.dataPath).Parent).ToString().Replace("\\", "/") + "/BuildConfig.xml";
        System.Security.SecurityElement content = XMLParser.LoadXML(FileAccessManager.LoadText(root));
        foreach (System.Security.SecurityElement item in content.Children)
        {
            if (item.Tag == "PublishDir")
            {
                dstDir = item.Text.Replace("\\", "/") + "/Assets/Scripts";
                break;
            }
        }
        string srcDir = Application.dataPath + "/Scripts";
        ExportScenesManager.DirectoryCopy(srcDir, dstDir, true, true);
    }
    [MenuItem("Assetbundle/Copy Resources To Publish StreamingAsset")]
    public static void CopyResourceToPublishStreamingAsset()
    {
        string publishdir = "../publish/Assets/StreamingAssets";
        string root = (new DirectoryInfo(Application.dataPath).Parent).ToString().Replace("\\", "/") + "/BuildConfig.xml";
        System.Security.SecurityElement content = XMLParser.LoadXML(Utils.LoadFile(root));
        foreach (System.Security.SecurityElement item in content.Children)
        {
            if (item.Tag == "PublishDir")
            {
                publishdir = item.Text.Replace("\\", "/") + "/Assets/StreamingAssets";
                break;
            }
        }
        if (!Directory.Exists(publishdir))
            Directory.CreateDirectory(publishdir);
        //拷贝资源到publish streamingasset
        var newVersionCode = GetNewVersionCode();
        var newVersion = newVersionCode.ToString();
        var newVersionFolder = GetVersionFolder(newVersion);
        var targetPath = newVersionFolder + ExportFilesPath;
        var resourceindexfile = targetPath + "/ResourceIndexInfo.txt";
        File.Copy(resourceindexfile, publishdir + "/ResourceIndexInfo.txt", true);
        var pkgFile = newVersionFolder + "/" + MogoFileSystem.FILE_NAME;
        Debug.Log(pkgFile);
        File.Copy(pkgFile, publishdir + "/" + MogoFileSystem.FILE_NAME, true);
        //ExportScenesManager.DirectoryCopy(targetPath, publishdir, true, true);
        //只拷贝.u的资源
        var fileList = Directory.GetFiles(targetPath, "*.u", SearchOption.AllDirectories);
        foreach (string path in fileList)
        {
            string srcPath = path.Replace("\\", "/");
            string srcHalfDir = targetPath;
            string srcName = srcPath.ReplaceFirst(srcHalfDir, "");
            string dstPath = publishdir + "/" + srcName;
            string dstDir = dstPath.Substring(0, dstPath.LastIndexOf("/"));
            if (!Directory.Exists(dstDir))
                Directory.CreateDirectory(dstDir);
            if (File.Exists(dstPath))
                File.Delete(dstPath);
            File.Copy(srcPath, dstPath);
        }
        Debug.Log("拷贝完成，总数：" + fileList.Length);
    }
    [MenuItem("Assetbundle/Up BundleVersion")]
    public static void CopyVersionXml2Resources()
    {
        var newVersionCode = GetNewVersionCode();
        var newVersion = newVersionCode.ToString();
        var newVersionFolder = GetVersionFolder(newVersion);
        //生成version.xml文件，保存在Resource目录中
        string resourcpath = newVersionFolder + "/version.xml";
        StringBuilder versioncontent = new StringBuilder();
        versioncontent.AppendLine("<root>");
        versioncontent.AppendLine("<ProgramVersion>" + newVersion + "</ProgramVersion>");
        versioncontent.AppendLine("<ResouceVersion>" + newVersion + "</ResouceVersion>");
        versioncontent.AppendLine("</root>");
        Mogo.Util.XMLParser.SaveText(resourcpath, versioncontent.ToString());
    }
    private static void BuildExportedFilesVersion(string version)
    {
        var newVersionFolder = GetVersionFolder(version);
        var exportRootPath = newVersionFolder + ExportFilesPath;
        BuildExportedFilesVersion(exportRootPath, exportRootPath, version, newVersionFolder);
    }

    [MenuItem("Assetbundle/Export Mogo Lib")]
    public static void ExportMogoLib()
    {
#if !UNITY_IPHONE
        var sw = new Stopwatch();
        sw.Start();
        ExportLib("obj/Debug", "Assembly-CSharp.dll", Driver.FileName);

        var dlls = Directory.GetFiles(Application.dataPath + "/Scripts/libs", "*.dll");
        for (int i = 0; i < dlls.Length; i++)
        {
            var item = dlls[i];
            ExportLib(Path.GetDirectoryName(item), Path.GetFileName(item), Driver.FileName + i);
            //ExportScenesManager.LogDebug(Path.GetDirectoryName(item));
            ExportScenesManager.LogDebug(Path.GetFileName(item));
            //ExportScenesManager.LogDebug(item);
        }
        sw.Stop();
        ExportScenesManager.LogDebug(sw.ElapsedMilliseconds);

    }

    private static void ExportLib(string path, string fileName, string outputName)
    {
        var exportPath = ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath);
        var versionFolder = Path.Combine(exportPath, GetNewVersion());
        var targetPath = versionFolder + ExportFilesPath;
        if (!Directory.Exists(targetPath))
            Directory.CreateDirectory(targetPath);
        var sourcePath = ExportScenesManager.GetFolderPath(path);
        var zipPath = Path.Combine(sourcePath, Utils.GetFileNameWithoutExtention(fileName) + ".zip");
        Utils.PackFiles(zipPath, sourcePath, fileName);
        var libData = Utils.LoadByteFile(zipPath);
        var enData = DESCrypto.Encrypt(libData, Driver.Number);
        XMLParser.SaveBytes(Path.Combine(targetPath, outputName), enData);
#endif
    }

    /// <summary>
    /// Refresh the window on selection.
    /// </summary>
    void OnSelectionChange() { Repaint(); }

    /// <summary>
    /// Draw the custom wizard.
    /// </summary>
    void OnGUI()
    {
        EditorGUIUtility.LookLikeControls(80f);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.BeginHorizontal();
        GUILayout.Label("比对版本号：", GUILayout.Width(120f));
        m_currentVersion = GUILayout.TextField(m_currentVersion);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("目标版本号：", GUILayout.Width(120f));
        m_newVersion = GUILayout.TextField(m_newVersion);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("提升版本", GUILayout.Width(120f)))
        {
            BundleExporter.ClearDependenciesDic();
            m_currentVersion = new VersionCodeInfo(m_currentVersion).GetUpperVersion();
            m_newVersion = new VersionCodeInfo(m_newVersion).GetUpperVersion();
            var curVersionFolder = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), m_currentVersion).Replace("\\", "/");
            m_newVersionFolder = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), m_newVersion).Replace("\\", "/");
            ExportScenesManager.DirectoryCopy(curVersionFolder, m_newVersionFolder, true);
        }

        NGUIEditorTools.DrawSeparator();
        GUILayout.BeginHorizontal();
        GUILayout.Label("选择打包资源：", GUILayout.Width(120f));
        bool tempAll = m_selectAll;
        m_selectAll = GUILayout.Toggle(tempAll, "全选");
        GUILayout.EndHorizontal();
        if (m_buildResourcesInfoList != null)
            foreach (var item in m_buildResourcesInfoList)
            {
                NGUIEditorTools.DrawSeparator();
                GUILayout.BeginHorizontal();
                bool temp = item.check;
                bool hasChanged = false;
                if (m_selectAll != tempAll)
                    item.check = m_selectAll;
                item.check = GUILayout.Toggle(item.check, item.name);
                GUILayout.Label(item.type, GUILayout.Width(100f));
                foreach (var ex in item.extentions)
                {
                    GUILayout.Label(ex, GUILayout.Width(100f));
                }
                hasChanged = temp != item.check;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                foreach (var folder in item.folders)
                {
                    if (hasChanged)
                        folder.check = item.check;
                    folder.check = GUILayout.Toggle(folder.check, folder.path);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("导出资源EX", GUILayout.Width(120f)))
                {
                    var targetPath = m_newVersionFolder + ExportFilesPath;
                    BuildAssetBundleMainAsset(item, targetPath);
                }
                if (GUILayout.Button("生成资源版本", GUILayout.Width(120f)))
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var rootPath = Path.Combine(m_newVersionFolder, "version").Replace("\\", "/");
                    if (item.check)
                        BuildAssetVersion(item, rootPath);
                    sw.Stop();
                    Debug.Log("BuildAssetVersion time: " + sw.ElapsedMilliseconds);
                }

                GUILayout.EndHorizontal();
            }

        NGUIEditorTools.DrawSeparator();
        GUILayout.BeginHorizontal();
        GUILayout.Label("选择拷贝资源：", GUILayout.Width(120f));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (m_copyResourcesInfoList != null)
            foreach (var item in m_copyResourcesInfoList)
            {
                item.check = GUILayout.Toggle(item.check, item.sourcePath);
                if (item.check && GUILayout.Button("导出", GUILayout.Width(120f)))
                {
                    //var versionFolder = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), m_newVersion);
                    var targetPath = m_newVersionFolder + ExportFilesPath;
                    ExportScenesManager.CopyFolder(Path.Combine(targetPath, item.targetPath), Application.dataPath + item.sourcePath, item.extention);
                }
            }
        GUILayout.EndHorizontal();

        NGUIEditorTools.DrawSeparator();
        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("MogoLib", GUILayout.Width(120f)))
        {
            ExportMogoLib();
        }
        if (GUILayout.Button("压缩", GUILayout.Width(120f)))
        {
            Zip();
        }
        if (GUILayout.Button("完整打包", GUILayout.Width(120f)))
        {
            var sw = new Stopwatch();
            sw.Start();
            var targetPath = m_newVersionFolder + ExportFilesPath;
            foreach (var item in m_buildResourcesInfoList)
            {
                if (item.check)
                    BuildAssetBundleMainAsset(item, targetPath);
            }
            foreach (var item in m_copyResourcesInfoList)
            {
                if (item.check)
                {
                    ExportScenesManager.CopyFolder(Path.Combine(targetPath, item.targetPath), Application.dataPath + item.sourcePath, item.extention);
                }
            }
            sw.Stop();
            Debug.Log("完整打包 time: " + sw.ElapsedMilliseconds);
        }
        if (GUILayout.Button("生成资源版本", GUILayout.Width(120f)))
        {
            var root = Application.dataPath;
            var dataPath = new DirectoryInfo(Application.dataPath).Parent.FullName.Replace("\\", "/") + "/";
            System.Action action = () =>
            {
                BuildAssetVersion(root, dataPath, m_newVersion, m_newVersionFolder);
            };
            action.BeginInvoke(null, null);
        }
        if (GUILayout.Button("比对资源版本", GUILayout.Width(120f)))
        {
            var currentVersionFolder = GetVersionFolder(m_currentVersion);
            var diff = FindVersionDiff(currentVersionFolder, m_newVersionFolder, "version");
            LogDebug(diff.PackArray('\n'));
            diff = BundleExporter.FindDependencyRoot(diff);
            LogDebug("Root resource:\n" + diff.PackArray('\n'));
            var targetPath = m_newVersionFolder + ExportFilesPath;
            BundleExporter.BuildBundleWithRoot(diff, targetPath);
        }
        if (GUILayout.Button("清理MR", GUILayout.Width(120f)))
        {
            if (!EditorUtility.DisplayDialog("Conform", "确认清理？", "yes", "no"))
                return;
            var rootPath = ExportScenesManager.GetFolderPath(ExportScenesManager.SubMogoResources);
            Directory.Delete(rootPath, true);
            LogDebug("clean success.");
        }
        if (GUILayout.Button("拷贝MR", GUILayout.Width(120f)))
        {
            var targetPath = ExportScenesManager.GetFolderPath(ExportScenesManager.SubMogoResources);
            var sourcePath = m_newVersionFolder + ExportFilesPath;
            ExportScenesManager.DirectoryCopy(sourcePath, targetPath, true);
            LogDebug("copy success.");
        }

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 生成资源依赖
    /// </summary>
    [MenuItem("Assetbundle/Build Asset Ref")]
    public static void BuildAssetRef()
    {
        var sw = new Stopwatch();
        sw.Start();
        var buildResourcesInfoList = ExportScenesManager.LoadBuildResourcesInfo();
        if (buildResourcesInfoList == null)
            return;
        foreach (var item in buildResourcesInfoList)
        {
            var list = BuildProjectExWizard.GetAssetList(item).ToList();
            BundleExporter.BuildDependencyTree(list.ToArray());
            //LogError(list.PackList('\n'));
        }
        ExportScenesManager.LogDebug("BundleExporter.dependenciesDic.Count: " + BundleExporter.dependenciesDic.Count);
        sw.Stop();
        ExportScenesManager.LogDebug("BuildAssetRef: " + sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// 比对源资源变化
    /// </summary>
    /// <param name="currentVersionFolder"></param>
    /// <param name="newVersionFolder"></param>
    /// <returns></returns>
    private static string[] FindVersionDiff(string currentVersionFolder, string newVersionFolder, string versionFileName)
    {
        var currentVersionInfos = new Dictionary<string, VersionInfo>();
        var newVersionInfos = new Dictionary<string, VersionInfo>();

        ExportScenesManager.LoadVersionFile(versionFileName, currentVersionFolder, currentVersionInfos, "");
        ExportScenesManager.LoadVersionFile(versionFileName, newVersionFolder, newVersionInfos, "");

        var diff = (from newV in newVersionInfos
                    where !(currentVersionInfos.ContainsKey(newV.Key) && currentVersionInfos[newV.Key].MD5 == newV.Value.MD5)
                    select newV.Key).ToArray();

        return diff;
    }

    public static string GetNewVersion()
    {
        return GetNewVersionCode().ToString();
    }

    public static VersionCodeInfo GetNewVersionCode()
    {
        var dirs = Directory.GetDirectories(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath));
        var currentVersion = new VersionCodeInfo("0.0.0.1");
        foreach (var item in dirs)
        {
            var fileVersion = new VersionCodeInfo(new DirectoryInfo(item).Name);
            if (fileVersion.Compare(currentVersion) > 0)
                currentVersion = fileVersion;
        }
        return currentVersion;
    }

    public static string GetVersionFolder(string version)
    {
        return Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), version).Replace("\\", "/");
    }

    /// <summary>
    /// 生成源资源版本
    /// </summary>
    /// <param name="root"></param>
    /// <param name="dataPath"></param>
    /// <param name="newVersion"></param>
    /// <param name="newVersionPath"></param>
    private static void BuildAssetVersion(string root, string dataPath, string newVersion, string newVersionPath)
    {
        var sw = new Stopwatch();
        sw.Start();
        var allFiles = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);
        ExportScenesManager.LogDebug("total files count: " + allFiles.Length);
        var files = new Dictionary<string, VersionInfo>();
        foreach (var item in allFiles)
        {
            if (item.EndsWith(".meta") || item.EndsWith(".cs") || item.EndsWith(".xml")
                || item.Contains(".svn") || item.Contains("SpawnPointGearAgent") || item.Contains("TrapStudio")
                || item.Contains(@"Gear\Agent") || item.Contains(@"Gear\Example") || item.Contains(@"Assets\Scenes")
                || item.Contains(@"_s.unity"))
                continue;
            var fileName = item.Replace("\\", "/");
            var md5 = ExportScenesManager.GetFileMD5(fileName);
            var path = fileName.ReplaceFirst(dataPath, "");
            files.Add(fileName, new VersionInfo() { Path = path, MD5 = md5, Version = newVersion });
        }

        ExportScenesManager.SaveVersionFile("version", newVersionPath, files, "");
        sw.Stop();
        ExportScenesManager.LogDebug("BuildAssetVersion time: " + sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// 生成导出资源版本
    /// </summary>
    /// <param name="root"></param>
    /// <param name="dataPath"></param>
    /// <param name="newVersion"></param>
    /// <param name="newVersionPath"></param>
    private static void BuildExportedFilesVersion(string root, string dataPath, string newVersion, string newVersionPath)
    {
        var fileVersion = Path.Combine("ExportFileVersion", newVersionPath) + ".txt";
        if (File.Exists(fileVersion))
            return;

        var sw = new Stopwatch();
        sw.Start();
        var allFiles = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);
        ExportScenesManager.LogDebug("total files count: " + allFiles.Length);
        var files = new Dictionary<string, VersionInfo>();
        foreach (var item in allFiles)
        {
            var fileName = item.Replace("\\", "/");
            var md5 = ExportScenesManager.GetFileMD5(fileName);
            var path = fileName.ReplaceFirst(dataPath, "");
            files.Add(fileName, new VersionInfo() { Path = path, MD5 = md5, Version = newVersion });
        }

        ExportScenesManager.SaveVersionFile("ExportFileVersion", newVersionPath, files, "");
        sw.Stop();
        ExportScenesManager.LogDebug("BuildAssetVersion time: " + sw.ElapsedMilliseconds);
    }

    private void BuildAssetBundle(BuildResourcesInfo item)
    {
        List<string> list = new List<string>();
        foreach (var folder in item.folders)
        {
            var root = Application.dataPath + "/" + folder.path;
            LogDebug(root);
            foreach (var extention in item.extentions)
            {
                var sp = "*" + extention;
                LogDebug(sp);
                list.AddRange(Directory.GetFiles(root, sp, folder.deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
            }
        }
        LogDebug(list.Count);
        BuildResourceManually(new VersionCodeInfo(m_currentVersion), new VersionCodeInfo(m_newVersion), item.name, item.type, list, item.isPopInBuild, item.extentions);
    }

    private void BuildAssetBundleInfo(BuildResourcesInfo item)
    {
        List<string> list = new List<string>();
        foreach (var folder in item.folders)
        {
            var root = Application.dataPath + "/" + folder.path;
            LogDebug(root);
            foreach (var extention in item.extentions)
            {
                var sp = "*" + extention;
                LogDebug(sp);
                list.AddRange(Directory.GetFiles(root, sp, folder.deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
            }
        }
        LogDebug(list.Count);
        BuildResourceInfoManually(new VersionCodeInfo(m_currentVersion), new VersionCodeInfo(m_newVersion), item.name, item.type, list, item.isPopInBuild, item.extentions);
    }

    private static void BuildAssetBundleMainAsset(BuildResourcesInfo item, string exportRootPath, bool isMerge = false)
    {
        BundleExporter.BuildBundleWithRoot(GetAssetList(item).ToArray(), exportRootPath, isMerge);
    }

    private void BuildAssetVersion(BuildResourcesInfo item, string rootPath)
    {
        BundleExporter.BuildAssetVersion(GetAssetList(item).ToArray(), rootPath);
    }

    public static IEnumerable<string> GetAssetList(BuildResourcesInfo item)
    {
        List<string> list = new List<string>();
        foreach (var folder in item.folders)
        {
            var root = Application.dataPath + "/" + folder.path;
            ExportScenesManager.LogDebug("root: " + root);
            foreach (var extention in item.extentions)
            {
                var sp = "*" + extention;
                ExportScenesManager.LogDebug("sp: " + sp);
                list.AddRange(Directory.GetFiles(root, sp, folder.deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
            }
        }

        var newList = from fileName in list
                      select fileName.Replace("\\", "/").ReplaceFirst(Application.dataPath, "Assets");
        ExportScenesManager.LogDebug("list.Count: " + list.Count);
        return newList;
    }

    private void CopyAssetBundle(BuildResourcesInfo item)
    {
        //if (String.IsNullOrEmpty(item.copyOrder))
        //{
        PackManually(item.name, new VersionCodeInfo(m_newVersion));
        //}
        //else
        //{
        //    var exportPath = ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath);
        //    var versionFolder = Path.Combine(exportPath, m_newVersion);
        //    var uiRoot = Path.Combine(versionFolder, item.name) + "\\" + ExportScenesManager.SubMogoResources;
        //    LogDebug(uiRoot);
        //    var red = Path.Combine(ExportScenesManager.GetFolderPath("ResourceDef"), item.copyOrder);
        //    var files = Utils.LoadFile(red);
        //    var fs = files.Split('\n');
        //    var firstList = new List<DirectoryInfo>();
        //    for (int i = 0; i < fs.Length - 1; i++)
        //    {
        //        var fullName = uiRoot + "\\" + fs[i].Trim();
        //        var dir = new DirectoryInfo(fullName);
        //        if (dir.Exists)
        //            firstList.Add(dir);
        //    }
        //    LogDebug("firstList: " + firstList.Count);
        //    var left = new List<DirectoryInfo>();
        //    var af = Directory.GetDirectories(uiRoot);
        //    foreach (var a in af)
        //    {
        //        var fullName = a.Replace("/", "\\");
        //        var dir = new DirectoryInfo(fullName);
        //        if (firstList.Contains(dir))
        //            continue;
        //        else
        //            left.Add(dir);
        //    }
        //    LogDebug("left: " + left.Count);
        //    firstList.AddRange(left);
        //    PackManually(firstList, item.name, new VersionCodeInfo(m_newVersion));
        //}
    }

    public static void EmptyFunc()
    {
        //留给导出脚本之前，先由unity执行一次，生成最新的Assembly-CSharp.csproj
    }

    private void Packup()
    {
        var versionFolder = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), m_newVersion);
        var targetPath = versionFolder + ExportFilesPath;
        foreach (var item in m_buildResourcesInfoList)
        {
            var resourceFolder = Path.Combine(versionFolder, item.name) + "\\target";
            if (!Directory.Exists(resourceFolder))
                continue;
            var files = Directory.GetFiles(resourceFolder, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var subPath = file.ReplaceFirst(resourceFolder, "");
                var target = targetPath + subPath;
                if (!File.Exists(target))
                {
                    //LogDebug("target: " + target);
                    if (!Directory.Exists(Utils.GetDirectoryName(target.Replace("\\", "/"))))
                        Directory.CreateDirectory(Utils.GetDirectoryName(target.Replace("\\", "/")));
                    File.Copy(file, target);
                    //exported.Add(target);
                }
                //else
                //    LogDebug("Same File: " + subPath);
            }
        }
    }

    private void Zip()
    {
        m_updatedFiles.Clear();
        ExportScenesManager.LoadVersionFile("ExportFileVersion", ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), m_updatedFiles);
        //var curVerPath = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), currentVersion) + ExportFilesPath;
        //var orgFiles = Directory.GetFiles(curVerPath);
        //foreach (var item in orgFiles)
        //{
        //    var md5 = ExportScenesManager.GetFileMD5(item);
        //    var path = item.ReplaceFirst(curVerPath, "");
        //    var vi = new VersionInfo() { Path = path, MD5 = md5, Version = currentVersion };
        //    m_updatedFiles.Add(path, vi);
        //}
        var versionFolder = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), m_newVersion);
        var sourcePath = versionFolder + ExportFilesPath;
        var newFiles = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
        LogDebug(sourcePath);
        var updatedFiles = new List<string>();
        LogDebug(newFiles.Length);
        foreach (var item in newFiles)
        {
            var md5 = ExportScenesManager.GetFileMD5(item);
            var path = item.ReplaceFirst(sourcePath, "");
            if (m_updatedFiles.ContainsKey(path) && m_updatedFiles[path].MD5 == md5)
                continue;//没有变化的文件忽略
            m_updatedFiles[path] = new VersionInfo() { Path = path, MD5 = md5, Version = m_currentVersion };
            updatedFiles.Add(item);
        }
        LogDebug(m_updatedFiles.Count);
        ExportScenesManager.SaveVersionFile("ExportFileVersion", ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), m_updatedFiles, sourcePath);
        var targetPath = versionFolder;
        var tempExport = ExportScenesManager.GetFolderPath("tempExport");
        PackUpdatedFiles(sourcePath, tempExport, targetPath, updatedFiles, m_currentVersion, m_newVersion);
    }

    /// <summary>
    /// 打包更新的文件。
    /// </summary>
    /// <param name="saveFolder">存放文件夹。</param>
    /// <param name="fileList">更新文件列表。</param>
    /// <param name="currentVersion">当前版本号。</param>
    /// <param name="newVersion">目标版本号。</param>
    public static void PackUpdatedFiles(string saveFolder, string tempExport, string targetPath, List<string> fileList, string currentVersion, string newVersion)
    {
        foreach (var item in fileList)
        {
            var path = item.Replace(saveFolder, "");
            var newPath = string.Concat(tempExport, path);
            var di = Path.GetDirectoryName(newPath);
            if (!Directory.Exists(di))
                Directory.CreateDirectory(di);
            if (File.Exists(newPath))
                continue;
            File.Copy(item, newPath);
        }
        ExportScenesManager.ZIPFile(tempExport, targetPath, currentVersion, newVersion);
        Directory.Delete(tempExport, true);
    }

    /// <summary>
    /// 读取版本信息。
    /// </summary>
    /// <param name="version"></param>
    public void LoadVersion(string version)
    {
        m_fileVersions.Clear();
        ExportScenesManager.LoadVersionFile("version", ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath) + "/" + version, m_fileVersions);
        LogDebug("Load Version finished. " + version);
    }

    public void SaveVersion(string version)
    {
        ExportScenesManager.SaveVersionFile("version", ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath) + "/" + version, m_fileVersions);
    }

    private void BuildResourceManually(VersionCodeInfo currentVersion, VersionCodeInfo newVersion, string typeName, string resFolder, List<string> targets, bool isPopInBuild = false, params string[] extentions)
    {
        LoadVersion(currentVersion.ToString());
        //var newVersion = currentVersion.GetUpperVersion();
        var versionFolder = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), newVersion.ToString());
        var targetFolder = Path.Combine(versionFolder, typeName);
        var folder = Path.Combine(targetFolder, ExportScenesManager.SubMogoResources);//获取输出目录
        Stopwatch sw = new Stopwatch();
        sw.Start();
        //Object[] selection = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);//Selection.objects;
        var expordedFiles = new List<string>();
        foreach (var item in targets)
        {
            var path = "Assets" + item.ReplaceFirst(Application.dataPath, "");
            LogDebug(path);
            var o = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            //LogDebug(o);
            var target = folder + "/" + Path.GetFileNameWithoutExtension(item);
            expordedFiles.AddRange(ExportScenesManager.ExportResourcesEx(new UnityEngine.Object[] { o }, newVersion.ToString(), target, resFolder, isPopInBuild, extentions));//导出资源，并将重新输出的资源返回并记录
            //AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
        LogDebug("m_fileVersions.Count" + m_fileVersions.Count);
        ExportScenesManager.SaveExportedFileList(expordedFiles, ExportScenesManager.SubExportedFileList, targetFolder, targetFolder);//记录此次导出的文件信息。
        //LogError("SaveExportedFileList time: " + sw.ElapsedMilliseconds);
        //var uFiles = FindUpdatedFiles(folder);//获取有更新的资源。
        //var updatedFiles = uFiles.Keys.ToList();
        //LogError("FindUpdatedFiles time: " + sw.ElapsedMilliseconds);
        //LogError("PackUpdatedFiles time: " + sw.ElapsedMilliseconds);
        //LogError("Total time: " + sw.ElapsedMilliseconds);
        sw.Stop();
        SaveVersion(newVersion.ToString());
    }

    private void BuildResourceInfoManually(VersionCodeInfo currentVersion, VersionCodeInfo newVersion, string typeName, string resFolder, List<string> targets, bool isPopInBuild = false, params string[] extentions)
    {
        //LoadVersion(currentVersion.ToString());
        //var newVersion = currentVersion.GetUpperVersion();
        var versionFolder = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), newVersion.ToString());
        var targetFolder = Path.Combine(versionFolder, typeName);
        var folder = Path.Combine(targetFolder, ExportScenesManager.SubMogoResources);//获取输出目录
        Stopwatch sw = new Stopwatch();
        sw.Start();
        //Object[] selection = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);//Selection.objects;
        var expordedFiles = new List<string>();
        foreach (var item in targets)
        {
            var path = "Assets" + item.ReplaceFirst(Application.dataPath, "");
            LogDebug(path);
            var o = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            //LogDebug(o);
            var target = folder + "/" + Path.GetFileNameWithoutExtension(item);
            expordedFiles.AddRange(ExportScenesManager.ExportResourcesInfo(new UnityEngine.Object[] { o }, newVersion.ToString(), target, resFolder, isPopInBuild, extentions));//导出资源，并将重新输出的资源返回并记录
            //AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
        LogDebug("m_fileVersions.Count" + m_fileVersions.Count);
        //ExportScenesManager.SaveExportedFileList(expordedFiles, ExportScenesManager.SubExportedFileList, targetFolder, targetFolder);//记录此次导出的文件信息。
        //LogError("SaveExportedFileList time: " + sw.ElapsedMilliseconds);
        //var uFiles = FindUpdatedFiles(folder);//获取有更新的资源。
        //var updatedFiles = uFiles.Keys.ToList();
        //LogError("FindUpdatedFiles time: " + sw.ElapsedMilliseconds);
        //LogError("PackUpdatedFiles time: " + sw.ElapsedMilliseconds);
        //LogError("Total time: " + sw.ElapsedMilliseconds);
        sw.Stop();
        //SaveVersion(newVersion.ToString());
    }

    public void PackManually(List<DirectoryInfo> folders, string typeName, VersionCodeInfo newVersion)
    {
        var versionFolder = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), newVersion.ToString());
        var resourceFolder = Path.Combine(versionFolder, typeName);

        var targetPath = resourceFolder + "/target";
        var exported = new List<string>();
        foreach (var folderPath in folders)
        {
            LogDebug("folderPath: " + folderPath);
            var files = Directory.GetFiles(folderPath.FullName, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var subPath = file.ReplaceFirst(folderPath.FullName, "");
                var target = targetPath + subPath;
                if (!File.Exists(target))
                {
                    //LogDebug("target: " + target);
                    if (!Directory.Exists(Utils.GetDirectoryName(target.Replace("\\", "/"))))
                        Directory.CreateDirectory(Utils.GetDirectoryName(target.Replace("\\", "/")));
                    File.Copy(file, target);
                    exported.Add(target);
                }
                //else
                //    LogDebug("Same File: " + subPath);
            }
        }
        LogDebug("copy files: " + exported.Count);
    }

    public void PackManually(string typeName, VersionCodeInfo newVersion)
    {
        var versionFolder = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), newVersion.ToString());
        var resourceFolder = Path.Combine(versionFolder, typeName);
        var folder = Path.Combine(resourceFolder, ExportScenesManager.SubMogoResources);//获取输出目录

        var folders = new DirectoryInfo(folder).GetDirectories().OrderBy(t => t.CreationTime);
        PackManually(folders.ToList(), typeName, newVersion);
        //ExportScenesManager.PackUpdatedFiles(targetPath, exported, typeName, typeName);//打包更新的文件。
    }

    public static List<string> GetAllResources()
    {
        ExportScenesManager.InitMsg();
        ExportScenesManager.AutoSwitchTarget();
        var buildResourcesInfoList = ExportScenesManager.LoadBuildResourcesInfo();
        if (buildResourcesInfoList == null)
            return new List<string>();

        var newVersion = GetNewVersion();
        var newVersionFolder = GetVersionFolder(newVersion);
        var root = Application.dataPath;
        var dataPath = new DirectoryInfo(Application.dataPath).Parent.FullName.Replace("\\", "/") + "/";
        BuildAssetVersion(root, dataPath, newVersion, newVersionFolder);

        var sw = new Stopwatch();
        sw.Start();
        List<string> res = new List<string>();
        var exportRootPath = newVersionFolder + ExportFilesPath;
        foreach (var item in buildResourcesInfoList)
        {
            if (item.check)
            {
                if (item.packLevel != null && item.packLevel.Length > 0)
                {
                    ExportScenesManager.LogDebug(item.packLevel.PackArray());
                    ResourceManager.PackedExportableFileTypes = item.packLevel;
                }
                foreach (var v in GetAssetList(item).ToArray())
                {
                    res.Add(v);
                }
                //BuildAssetBundleMainAsset(item, exportRootPath, item.isMerge > 0 ? true : false);
            }
        }
        //CopyResources(exportRootPath);
        sw.Stop();
        ExportScenesManager.LogDebug("完整打包 time: " + sw.ElapsedMilliseconds);
        var path = ExportScenesManager.GetFolderPath() + "//ConsoleBuildTime.txt";
        Mogo.Util.XMLParser.SaveText(path.Replace("\\", "/"), "完整打包 time: " + sw.ElapsedMilliseconds);
        ExportScenesManager.CloseMsg();
        return res;
    }

    public void LogDebug(object content)
    {
        ExportScenesManager.LogDebug(content);
    }

    public void LogWarning(object content)
    {
        ExportScenesManager.LogWarning(content);
    }

    public void LogError(object content)
    {
        ExportScenesManager.LogError(content);
    }
}