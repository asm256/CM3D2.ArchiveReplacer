/*
 * コンパイルしてManagedフォルダ内へ置いてください
 * csc /t:library /r:Assembly-CSharp-firstpass.dll CM3D2.ArchiveReplacer.Hook.cs
 * CM3D2_KAIZOU\_Data フォルダに追加・置換したいファイルを置いてください
 */
// @AB_addarg /r:Assembly-CSharp-firstpass.dll
// @AB_addarg /lib:%managed%
// @AB_install %managed%

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

[assembly: AssemblyTitle("CM3D2.ArchiveReplacer.Hook")]
[assembly: AssemblyDescription("FileSystemArchiveのProxyClass")]
[assembly: AssemblyProduct("CM3D2.ArchiveReplacer")]
[assembly: AssemblyCopyright("Copyright © asm__ 2015")]
[assembly: AssemblyVersion("2015.9.9.0")]

namespace CM3D2.ArchiveReplacer.Hook
{
    public class AFile : AFileBase
    {
        FileStream fs;
        protected override void Dispose(bool is_release_managed_code)
        {
            fs.Dispose();
            fs = null;
        }

        public override int GetSize()
        {
            return (int)fs.Length;
        }

        public override bool IsValid()
        {
            return fs != null;
        }

        public override int Read(ref byte[] f_byBuf, int f_nReadSize)
        {
            return fs.Read(f_byBuf,0, f_nReadSize);
        }

        public override byte[] ReadAll()
        {
            int len = (int)fs.Length;
            byte[] buf = new byte[len];
            fs.Read(buf, 0, len);
            return buf;
        }

        public override int Seek(int f_unPos, bool absolute_move)
        {

            return (int)fs.Seek(f_unPos, absolute_move ? SeekOrigin.Begin : SeekOrigin.Current);
        }

        public override int Tell()
        {
            return (int)fs.Position;
        }

        public override DLLFile.Data object_data
        {
            get { throw new NotImplementedException(); }
        }
        public AFile(string path)
        {
            fs = File.OpenRead(path);
        }
    }
    public class HookArchive : FileSystemArchive
    {
        string path;
        Dictionary<string, string> locations;
        public HookArchive()
        {
            path = Path.Combine(System.Environment.CurrentDirectory, "_Data");
            //ファイル収集
            string[] list = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            locations = new Dictionary<string, string>(list.Length);
            foreach (string item in list)
            {
                string name = Path.GetFileName(item).ToLower();
                if (!Regex.IsMatch(name, @"readme\.txt$", RegexOptions.IgnoreCase))
                {

                    //LogPrint(name.ToLower());
                    if (locations.ContainsKey(name))
                    {
                        NDebug.Warning(string.Format("{0}と{1}が干渉しています\n{1}で上書きします", locations[name], item));
                        locations[name] = item;
                    }
                    else
                    {
                        locations.Add(name.ToLower(), item);
                    }
                }
            }
        }
        private void LogPrint(object s){
            Console.Write(string.Format("AchiveReplacer : {0}\n",s));
        }
        public override bool IsExistentFile(string file_name)
        {
#if DEBUG
            LogPrint("IsExistentFile <- " + file_name);
#endif
            return base.IsExistentFile(file_name);
        }
        public override AFileBase FileOpen(string file_name)
        {
#if DEBUG
            LogPrint("FileOpen <- " + file_name);
#endif
            var name = file_name.ToLower();
            string val;
            locations.TryGetValue(name, out val);
            if(!string.IsNullOrEmpty(val))
                return new AFile(val);
            return base.FileOpen(file_name);
        }
        public override string[] GetList(string f_str_path, ListType type)
        {
#if DEBUG
            LogPrint(string.Format("List <- {0} / {1}", f_str_path, type));
#endif
            string[] list =  base.GetList(f_str_path, type);
            if (type == ListType.AllFile)
            {
                var ll = from p in locations
                         where Regex.IsMatch(p.Key,string.Format("\\.{0}$",f_str_path))
                         select p.Key;
                return ll.Concat(list).ToArray();
            }
            return list;
        }
    }
}
