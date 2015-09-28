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
[assembly: AssemblyVersion("15.9.23.0")]

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
  public class HookArchive : FileSystemArchive {
    Dictionary<string , Func<PluginSDK.ConvertPluginBase>> locOpener;
    string basePath;
    public HookArchive() {
      basePath = Path.Combine(System.Environment.CurrentDirectory , "_Data");
      //ファイル収集
      string[] list = Directory.GetFiles(basePath , "*" , SearchOption.AllDirectories);
      locOpener = PluginSDK.ConvertPluginManager.createFactoryList(list);
    }
    /// <summary>
    /// Debugビルド時のみログを出力する
    /// </summary>
    /// <param name="s">出力したい情報</param>
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
      Func<PluginSDK.ConvertPluginBase> open;
      if(locOpener.TryGetValue(name , out open)) {
        DebugLogPrint("found");
        return open();
      } else {
        DebugLogPrint("not found");
        return base.FileOpen(name);
      }
    }
    public override string[] GetList(string f_str_path , ListType type) {
      DebugLogPrint(string.Format("List <- {0} / {1}" , f_str_path , type));
      string[] list = base.GetList(f_str_path , type);
      HashSet<string> isuniq = new HashSet<string>();
      foreach(var item in list)
        isuniq.Add(Path.GetFileName(item).ToLower());
      if(type == ListType.AllFile) {
        var ll = from p in locOpener
                 where Regex.IsMatch(p.Key , string.Format("\\.{0}$" , f_str_path)) && isuniq.Add(p.Key)
                 select p.Key;
        return ll.Concat(list).ToArray();
      }
      return list;
    }
  }
}
namespace CM3D2.ArchiveReplacer.PluginSDK {
  public static class ConvertPluginManager {
    protected struct PluginPair {
      public IConvertDescription desc;
      public Func<string , ConvertPluginBase> ctor;
      public PluginPair(IConvertDescription desc , Func<string , ConvertPluginBase> ctor) {
        this.desc = desc;
        this.ctor = ctor;
      }
    }
    //とりあえず初期容量8
    static List<PluginPair> catalog = new List<PluginPair>(8);
    static ConvertPluginManager() {
      var maintypes = Assembly.GetExecutingAssembly().GetTypes();
      foreach(var klass in maintypes) {
        foreach(var tag in klass.GetCustomAttributes(true)) {
          if(tag is ConvertPluginEnumAttribute) {
            ConvertPluginEnumAttribute mytag = tag as ConvertPluginEnumAttribute;
            if(mytag.autoRegister) {
              var register = klass.GetMethod("Register" , BindingFlags.Static | BindingFlags.Public);
              register.Invoke(null , null);
            }
          }
        }
      }
    }
    public static void Register(IConvertDescription desc , Func<string , ConvertPluginBase> ctor) {
      var pair = new PluginPair(desc , ctor);
      catalog.Add(pair);
    }
    public static Dictionary<string , Func<ConvertPluginBase>> createFactoryList(string[] paths) {
      var result = new Dictionary<string , Func<ConvertPluginBase>>(paths.Length);
      var chk_samepath = new Dictionary<string , string>(paths.Length);
      foreach(var path in paths) {
        bool found = false;
        // pathのブロックは1st foreachの１つ上なので使い回しされるので保存する
        var savedpath = path.ToLower();
        string name = Path.GetFileName(savedpath);
        foreach(var plgin in catalog) {
          var plgindesc = plgin.desc;
          if(plgindesc.isSrcFile(savedpath)) {
            string registname = plgindesc.Src2Dst(name);
            result[registname] = () => plgin.ctor(savedpath);
            chk_samepath[registname] = savedpath;
            found = true;
            break;
          }
          if(plgindesc.isDstFile(savedpath)) {
            if(chk_samepath.ContainsValue(plgindesc.Dst2Src(savedpath)))
              continue;
          }
        }
        if(!found) {
          if(name == "readme.txt") {
            continue;
          }
          string prevpath;
          if(chk_samepath.TryGetValue(name , out prevpath)) {
            UnityEngine.Debug.Log($"{prevpath}が{path}で上書きされます");
          }
          result[name] = () => new AFile(savedpath);
        }
      }
      return result;
    }
  }

  public class AFile : ConvertPluginBase {
    public AFile(string path) : base(path) {
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

    public override int GetSize() {
      return (int)fs.Length;
    }
  }

  public interface IConvertDescription {
    /// <summary>
    /// 元ファイルか確認する
    /// </summary>
    /// <param name="path">対象path</param>
    /// <returns></returns>
    bool isSrcFile(string path);
    /// <summary>
    /// 変換後のファイルパスか確認する
    /// </summary>
    /// <param name="path">対象path</param>
    /// <returns></returns>
    bool isDstFile(string path);
    /// <summary>
    /// 変換後のファイルパスを生成する
    /// </summary>
    /// <param name="path">対象path</param>
    /// <returns></returns>
    string Src2Dst(string path);
    /// <summary>
    /// 変換前のファイルパスを生成する
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    string Dst2Src(string path);
    /// <summary>
    /// 変換後のファイルの説明
    /// </summary>
    string dstFileDesc { get; }
    /// <summary>
    /// 変換前のファイルの説明
    /// </summary>
    string srcFileDesc { get; }
  }
  /// <summary>
  /// 簡単にIConvertDescripterを作る為の実装
  /// </summary>
  public class ExtConvertDesc : IConvertDescription {
    protected Regex _regexSrc;
    protected Regex _regexDst;
    protected string srcExt;
    protected string dstExt;
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="src_ext">変換元の拡張子 ex:.png</param>
    /// <param name="dst_ext">変換後の拡張子 ex:.tex</param>
    public ExtConvertDesc(string src_ext , string dst_ext) {
      srcFileDesc = srcExt = src_ext;
      dstFileDesc = dstExt = dst_ext;
      _regexSrc = new Regex(src_ext + "$");
      _regexDst = new Regex(dst_ext + "$");
    }
    #region IConvertDescripter
    public virtual string dstFileDesc { get; }
    public virtual string srcFileDesc { get; }
    public virtual bool isSrcFile(string path) {
      return _regexSrc.IsMatch(path);
    }
    public virtual bool isDstFile(string path) {
      return _regexDst.IsMatch(path);
    }
    public virtual string Src2Dst(string path) {
      return _regexSrc.Replace(path , dstExt);
    }
    public virtual string Dst2Src(string path) {
      return _regexDst.Replace(path , srcExt);
    }
    #endregion
  }
  [System.AttributeUsage(AttributeTargets.Class , Inherited = false , AllowMultiple = false)]
  sealed class ConvertPluginEnumAttribute : Attribute {
    public bool autoRegister { get; }

    public ConvertPluginEnumAttribute(bool autoRegister) {
      this.autoRegister = autoRegister;
    }
  }
  /// <summary>
  /// プラグインの親クラス
  /// コレに[ConvertPluginEnum(true)]属性付けてください
  /// </summary>
  public abstract class ConvertPluginBase : AFileBase {
    #region 共通そうなものをまとめておく
    protected FileStream fs;
    protected override void Dispose(bool is_release_managed_code) {
      fs.Dispose();
      fs = null;
    }
    public override bool IsValid() {
      return fs != null;
    }
    public override DLLFile.Data object_data {
      get { throw new NotImplementedException(); }
    }
    public ConvertPluginBase(string path) {
      fs = File.OpenRead(path);
    }
    #endregion
  }

  #region thx 高精細・肌テクスチャの人
  // from http://jbbs.shitaraba.net/bbs/read.cgi/game/55179/1441620833/448-454
  [ConvertPluginEnum(true)]
  public class APngFile : ConvertPluginBase {
    /// <summary>
    /// プラグインマネージャーへの登録
    /// </summary>
    public static void Register() {
      ConvertPluginManager.Register(new ExtConvertDesc(".png" , ".tex") , (string path) => new APngFile(path));
    }
    #region 後は従来のまま
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
    #endregion
  }
  #endregion
}
