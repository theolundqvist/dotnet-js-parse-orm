using System.Drawing;
using System.Text;
using Project.Util;
using Project.Util.WebUtil;
using Parse;
using Parse.Abstractions.Infrastructure;
using RestSharp;
using RSG;

namespace Project.ParseORM.Data;

public class DbFile
{
    private Parse.ParseFile _file;
    internal ParseFile Underlying => _file;
    private Progress<IDataTransferLevel> _progress = new();

    public struct MimeType
    {
        public const string PNG = "image/png";
        public const string JPEG = "image/jpeg";
        public const string BINARY = "application/octet-stream";
        public const string TEXT = "text/plain";
    }

    public enum FileType
    {
        Avatar,
        Binary
    }

    private string FixName(string name, string mimeType)
    {
        var n = name.Replace("/", "-");
        if (mimeType == "text/plain" && !name.EndsWith(".txt"))
        {
            n += ".txt";
        }
        return n;   
    }
    public DbFile(string name, Stream stream, string mimeType=null)
    {
        _file = new ParseFile(FixName(name, mimeType),stream, mimeType);
    }
    
    public DbFile(string name, byte[] data, string mimeType=null)
    {
        _file = new ParseFile(FixName(name, mimeType), data, mimeType);
    }

    public DbFile(string name, string data, string mimeType = MimeType.TEXT)
    {
        _file = new ParseFile(FixName(name, mimeType), Encoding.UTF8.GetBytes(data), mimeType);
    }

    public DbFile(string name, ImageMagick.MagickImage img, string mimeType=null)
    {
        _file = new ParseFile(FixName(name, mimeType), ImageUtil.GetData(img), mimeType);
    }
    internal DbFile(ParseFile pFile)
    {
        _file = pFile;
    }

    public string Name => _file.Name;
    public System.Uri Url => _file.Url;
    public string GetMimeType => _file.MimeType;
    public bool IsSaved => !_file.IsDirty;

    public IPromise<byte[]> DownloadAsync() => new WebRequest().SetEndpoint(Url.ToString()).SetContentType(GetMimeType).GET().Then(x => x.RawBytes);
    public IPromise<DbFile> SaveAsync(Action<double> onProgress=null)
    {
        if (onProgress != null)
        {
            UploadProgress.ProgressChanged += (sender, level) => onProgress(level.Amount);
        }
        return Database.Client.SaveFileAsync(_file, _progress).ToPromise().Then(() => this);
    }

    public Progress<IDataTransferLevel> UploadProgress => _progress;


}