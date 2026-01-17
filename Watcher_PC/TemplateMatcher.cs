using System;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Watcher_PC
{
    public class TemplateMatcher
    {
        private Mat? _template;

        public void LoadTemplate(string templatePath)
        {
            if (System.IO.File.Exists(templatePath))
            {
                _template = Cv2.ImRead(templatePath, ImreadModes.Color);
            }
        }

        public System.Drawing.Rectangle? FindTemplate(Bitmap screenImage, double threshold = 0.8)
        {
            if (_template == null || _template.Empty()) return null;

            using var screenMat = BitmapConverter.ToMat(screenImage);

            // Fix: Ensure screen capture is 3-channel BGR to match the template (usually loaded as BGR)
            // System.Drawing.Bitmap is often 32bppArgb (4 channels), while ImRead matches files which are usually 3 channels.
            if (screenMat.Channels() == 4)
            {
                Cv2.CvtColor(screenMat, screenMat, ColorConversionCodes.BGRA2BGR);
            }

            using var result = new Mat();

            // Perform template matching
            Cv2.MatchTemplate(screenMat, _template, result, TemplateMatchModes.CCoeffNormed);

            // Normalize result (optional, but good for visualization if debugging)
            // Cv2.Normalize(result, result, 0, 1, NormTypes.MinMax, -1);

            // Find best match
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal >= threshold)
            {
                return new System.Drawing.Rectangle(maxLoc.X, maxLoc.Y, _template.Width, _template.Height);
            }

            return null;
        }
    }
}
