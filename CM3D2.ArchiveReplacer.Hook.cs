/*
 * コンパイルしてManagedフォルダ内へ置いてください
 * csc /t:library /r:Assembly-CSharp-firstpass.dll CM3D2.ArchiveReplacer.Hook.cs
 * CM3D2_KAIZOU\_Data フォルダに追加・置換したいファイルを置いてください
 */
// @AB_addarg /r:Assembly-CSharp-firstpass.dll
// @AB_addarg /r:UnityEngine.dll
// @AB_addarg /lib:%managed%
// @AB_install %managed%

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

[assembly: AssemblyTitle("CM3D2.ArchiveReplacer.Hook")]
[assembly: AssemblyDescription("FileSystemArchiveのProxyClass")]
[assembly: AssemblyProduct("CM3D2.ArchiveReplacer")]
[assembly: AssemblyCopyright("Copyright © asm__ 2015")]
[assembly: AssemblyVersion("2015.9.21.1")]

namespace CM3D2.ArchiveReplacer.Hook {
  public class AFile : AFileBase {
    protected FileStream fs;
    protected override void Dispose(bool is_release_managed_code) {
      fs.Dispose();
      fs = null;
    }

    public override int GetSize() {
      return (int)fs.Length;
    }

    public override bool IsValid() {
      return fs != null;
    }

    public override int Read(ref byte[] f_byBuf , int f_nReadSize) {
      return fs.Read(f_byBuf , 0 , f_nReadSize);
    }

    public override byte[] ReadAll() {
      int len = (int)fs.Length;
      byte[] buf = new byte[len];
      fs.Read(buf , 0 , len);
      return buf;
    }

    public override int Seek(int f_unPos , bool absolute_move) {
      return (int)fs.Seek(f_unPos , absolute_move ? SeekOrigin.Begin : SeekOrigin.Current);
    }

    public override int Tell() {
      return (int)fs.Position;
    }

    public override DLLFile.Data object_data {
      get { throw new NotImplementedException(); }
    }
    public AFile(string path) {
      fs = File.OpenRead(path);
    }
  }
  #region thx 高精細・肌テクスチャの人
  // from http://jbbs.shitaraba.net/bbs/read.cgi/game/55179/1441620833/448-454
  public class APngFile : AFile {
    protected byte[] texHeader = new byte[] {
            0x09,
            (byte)'C', (byte)'M', (byte)'3', (byte)'D', (byte)'2', (byte)'_', (byte)'T', (byte)'E', (byte)'X',
            (1000 & 0xFF), (1000 >> 8), 0x00, 0x00,
            0x00,
            0x00, 0x00, 0x00, 0x00
        };
    protected int position;

    public APngFile(string locationPath) : base(locationPath) {
      if(fs != null) {
        long fileSize = fs.Length;
        if(fileSize > int.MaxValue) {
          throw new Exception("Too large PNG file size. maximum size is 2GB.");
        }
        uint size = (uint)fileSize;
        texHeader[15] = (byte)size;
        texHeader[16] = (byte)(size >> 8);
        texHeader[17] = (byte)(size >> 16);
        texHeader[18] = (byte)(size >> 24);
      }
      position = 0;
    }

    public override int GetSize() {
      return texHeader.Length + (int)fs.Length;
    }

    public override int Read(ref byte[] f_byBuf , int f_nReadSize) {
      int len;

      if(position < texHeader.Length) {
        int calcPos = position + f_nReadSize;
        if(calcPos <= texHeader.Length) {
          Array.Copy(texHeader , position , f_byBuf , 0 , f_nReadSize);
          position = calcPos;
          return f_nReadSize;
        } else {
          len = texHeader.Length - position;
          Array.Copy(texHeader , position , f_byBuf , 0 , len);
          len += fs.Read(f_byBuf , len , f_nReadSize - len);
        }
      } else {
        len = fs.Read(f_byBuf , 0 , f_nReadSize);
      }
      position += len;
      return len;
    }

    public override byte[] ReadAll() {
      int len = (int)fs.Length;
      position = texHeader.Length + len;
      byte[] buf = new byte[position];
      Array.Copy(texHeader , 0 , buf , 0 , texHeader.Length);
      fs.Seek(0 , SeekOrigin.Begin);
      fs.Read(buf , texHeader.Length , len);
      return buf;
    }

    public override int Seek(int f_unPos , bool absolute_move) {
      int calcPos = (absolute_move ? texHeader.Length : position) + f_unPos;
      if(calcPos >= texHeader.Length) {
        position = texHeader.Length + (int)fs.Seek(calcPos - texHeader.Length , SeekOrigin.Begin);
      } else {
        position = calcPos;
      }
      return position;
    }

    public override int Tell() {
      return position;
    }
  }
  #endregion
  public class HookArchive : FileSystemArchive {
    string path;
    Dictionary<string , string> locations;
    public HookArchive() {
      path = Path.Combine(System.Environment.CurrentDirectory , "_Data");
      //ファイル収集
      string[] list = Directory.GetFiles(path , "*" , SearchOption.AllDirectories);
      locations = new Dictionary<string , string>(list.Length);
      foreach(string item in list) {
        string name = Path.GetFileName(item).ToLower();
        switch(Path.GetExtension(name)) {
        case ".png":
          // pngはtexとしても登録する
          string alt_name = Path.ChangeExtension(name , ".tex");
          // 警告は表示しない
          locations[alt_name] = item;
          break;
        case ".tex":
          // もし、同じ階層に同名.pngが存在したらそっちを優先する
          if(locations.ContainsValue(Path.ChangeExtension(item , ".png")))
            continue;
          break;
        default:
          break;
        }
        if(!Regex.IsMatch(name , @"readme\.txt$" , RegexOptions.IgnoreCase)) {
          if(locations.ContainsKey(name)) {
            NDebug.Warning(string.Format("{0}と{1}が干渉しています\n{1}で上書きします" , locations[name] , item));
            locations[name] = item;
          } else {
            locations.Add(name , item);
          }
        }
      }
    }
    //Debugビルド時のみログを出力する
    [Conditional("DEBUG")]
    private void DebugLogPrint(object s) {
      UnityEngine.Debug.Log(string.Format("AchiveReplacer : {0}" , s).TrimEnd());
    }
    public override bool IsExistentFile(string file_name) {
      DebugLogPrint("IsExistentFile <- " + file_name);
      return base.IsExistentFile(file_name);
    }
    public override AFileBase FileOpen(string file_name) {
      DebugLogPrint("FileOpen <- " + file_name);
      var name = file_name.ToLower();
      string val;
      locations.TryGetValue(name , out val);
      if(Path.GetExtension(name) == ".tex" &&
        Path.GetExtension(val) == ".png")
        return new APngFile(val);
      if(!string.IsNullOrEmpty(val))
        return new AFile(val);
      return base.FileOpen(file_name);
    }
    public override string[] GetList(string f_str_path , ListType type) {
      DebugLogPrint(string.Format("List <- {0} / {1}" , f_str_path , type));
      string[] list = base.GetList(f_str_path , type);
      HashSet<string> isuniq = new HashSet<string>();
      foreach(var item in list)
        isuniq.Add(Path.GetFileName(item).ToLower());
      if(type == ListType.AllFile) {
        var ll = from p in locations
                 where Regex.IsMatch(p.Key , string.Format("\\.{0}$" , f_str_path)) && isuniq.Add(p.Key)
                 select p.Key;
        return ll.Concat(list).ToArray();
      }
      return list;
    }
  }
}
