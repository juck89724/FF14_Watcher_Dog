using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Watcher_PC
{
    public static class ImageHelper
    {
        public static Bitmap? CaptureWindow(IntPtr hwnd)
        {
            try
            {
                NativeMethods.GetClientRect(hwnd, out var rect);
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                if (width <= 0 || height <= 0) return null;

                var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var gfx = Graphics.FromImage(bmp))
                {
                    var hdcBitmap = gfx.GetHdc();
                    try
                    {
                        NativeMethods.PrintWindow(hwnd, hdcBitmap, NativeMethods.PW_CLIENTONLY);
                    }
                    finally
                    {
                        gfx.ReleaseHdc(hdcBitmap);
                    }
                }
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        // 影像預處理：4 倍放大、二值化、膨脹與寬邊距優化
        public static Bitmap PreProcessImage(Bitmap bitmap)
        {
            try
            {
                // 1. 轉換為 Mat
                using var src = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
                using var gray = new OpenCvSharp.Mat();
                using var resized = new OpenCvSharp.Mat();
                using var thresholded = new OpenCvSharp.Mat();
                using var inverted = new OpenCvSharp.Mat();
                using var dilated = new OpenCvSharp.Mat();
                using var final = new OpenCvSharp.Mat();

                // 1. 轉灰階
                OpenCvSharp.Cv2.CvtColor(src, gray, OpenCvSharp.ColorConversionCodes.BGR2GRAY);

                // 2. 放大圖片 (4.0 倍)
                OpenCvSharp.Cv2.Resize(gray, resized, new OpenCvSharp.Size(0, 0), 4.0, 4.0, OpenCvSharp.InterpolationFlags.Cubic);

                // 3. 二值化 (閾值調高至 170)
                OpenCvSharp.Cv2.Threshold(resized, thresholded, 100, 255, OpenCvSharp.ThresholdTypes.Binary);

                // 4. 反轉顏色 (變成白底黑字)
                OpenCvSharp.Cv2.BitwiseNot(thresholded, inverted);

                // 5. 形態學膨脹 (讓文字變粗，有助於連接斷筆)
                using var kernel = OpenCvSharp.Cv2.GetStructuringElement(OpenCvSharp.MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
                OpenCvSharp.Cv2.Dilate(inverted, dilated, kernel);

                // 6. 增加大量白邊 (50 像素)
                // 讓 OCR 引擎更容易識別行距與首尾
                OpenCvSharp.Cv2.CopyMakeBorder(dilated, final, 50, 50, 50, 50, OpenCvSharp.BorderTypes.Constant, new OpenCvSharp.Scalar(255));

                // 7. 轉回 Bitmap
                return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(final);
            }
            catch (Exception)
            {
                // Log 可以在外部處理，這裡僅返回 Clone
                return (Bitmap)bitmap.Clone();
            }
        }
    }
}
