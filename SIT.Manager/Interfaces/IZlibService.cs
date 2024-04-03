using System.Text;

namespace SIT.Manager.Interfaces;

public interface IZlibService
{
    public byte[] CompressToBytes(string data, ZlibCompression compressionProfile, Encoding? encoding = null);
    public string Decompress(byte[] data, Encoding? encoding = null);
}

public enum ZlibCompression
{
    BestSpeed = 1,
    BestCompression = 9
}
