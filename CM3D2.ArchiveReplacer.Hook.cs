/*
 * コンパイルしてManagedフォルダ内へ置いてください
 * csc /t:library /r:Assembly-CSharp-firstpass.dll CM3D2.ArchiveReplacer.Hook.cs
 * CM3D2_KAIZOU\_Data フォルダ直下にディレクトリ構造無視して、追加・置換したいファイルを置いてください
 *                            ~~~~  ~~~~~~~~~~~~~~~~~~~~
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        public HookArchive()
        {
            path = Path.Combine(System.Environment.CurrentDirectory, "_Data");
        }
        private void LogPrint(object s){
            Console.WriteLine(string.Format("AchiveReplacer : {0}",s));
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
            string _path = Path.Combine(path, file_name);
            if(File.Exists(_path)){
                return new AFile(_path);
            }
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
                var ll = Directory.GetFiles(path, "*." + f_str_path);
                foreach (var item in ll)
                {
                    LogPrint(item);
                    list.Concat(new string[]{ Path.GetFileName(item)});
                }
                return ll.Concat(list).ToArray();
            }
            return list;
        }
    }
}