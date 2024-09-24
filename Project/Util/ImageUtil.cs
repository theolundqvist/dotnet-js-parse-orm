using System.Drawing;
using System.Runtime.CompilerServices;
using ImageMagick;
namespace Project.Util;

public static class ImageUtil
{
    public static MagickImage Crop(MagickImage img, int width, int height, int x, int y)
    {
        var clone = img.Clone();
        clone.Crop(new MagickGeometry(x, y, width, height));
        return (MagickImage)clone;
    }

   public static MagickImage ClampSize(MagickImage img, int maxSize=256)
   {
       var w = img.Width;
       var h = img.Height;

       var size = 256;
       var nw = Math.Clamp(w, 0, 256);
       var nh = Math.Clamp(h, 0, 256);
       if (nw < 1 || nh < 1) throw new Exception("Image has 0 width or height");
       return ResizeImage(img, size, size);
   }
   
   /// <summary>
   /// Resize the image to the specified width and height.
   /// </summary>
   /// <param name="image">The image to resize.</param>
   /// <param name="width">The width to resize to.</param>
   /// <param name="height">The height to resize to.</param>
   /// <returns>The resized image.</returns>
   public static MagickImage ResizeImage(MagickImage image, int width, int height)
   {
       var clone = image.Clone();
       clone.Resize(width, height);
       return (MagickImage)clone;
   }
   
   public static MagickImage LoadFromFile(string path)
   {
       return new MagickImage(path);
   }
   
   public static MagickImage LoadFromData(byte[] data)
   {
       return new MagickImage(data);
   }

   public static byte[] GetData(MagickImage img)
   {
       return img.ToByteArray();
   }
}