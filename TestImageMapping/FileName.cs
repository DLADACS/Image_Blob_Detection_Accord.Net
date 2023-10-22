using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestImageMapping
{
    internal class FileName
    {




        //static void Main2()
        //{
        //    // Load the sample image
        //    Bitmap sampleImage = new Bitmap("sample.tif");

        //    // Load the template image
        //    Bitmap templateImage = new Bitmap("template.tif");

        //    // Perform template matching
        //    Size regionSize = new Size(templateImage.Width, templateImage.Height);

        //    // Define the overlap between regions (this can be adjusted as needed)
        //    int overlap = regionSize.Height / 10;

        //    // Divide the sample image into overlapping regions
        //    List<Bitmap> regions = DivideIntoOverlappingRegions2(sampleImage, regionSize, overlap);

        //    // Perform template matching on each region and combine results
        //    List<Point> globalMatches = new List<Point>();
        //    foreach (var region in regions)
        //    {
        //        List<Point> localMatches = PerformTemplateMatchingOnRegion(region, templateImage);
        //        // Assuming the regions are in row-major order
        //        int regionX = (region.Width - overlap) * (globalMatches.Count % regions.Count);
        //        int regionY = (region.Height - overlap) * (globalMatches.Count / regions.Count);
        //        List<Point> adjustedMatches = CombineResults(localMatches, regionX, regionY);
        //        globalMatches.AddRange(adjustedMatches);
        //    }

        //    List<Bitmap> matchedTemplates = ExtractMatchedTemplates(sampleImage, templateImage, globalMatches);
        //    // Save the matched templates
        //    for (int i = 0; i < matchedTemplates.Count; i++)
        //    {
        //        matchedTemplates[i].Save($"MatchedTemplate{i + 1}.jpg");
        //        matchedTemplates[i].Dispose(); // Dispose of the bitmap after saving it
        //    }

        //    Console.WriteLine("Processing complete!");
        //}

        //static List<Bitmap> ExtractMatchedTemplates(Bitmap originalImage, Bitmap templateImage, List<Point> globalMatches)
        //{
        //    List<Bitmap> matchedTemplates = new List<Bitmap>();

        //    foreach (Point location in globalMatches)
        //    {
        //        // Define the region to crop based on the template size and the match location
        //        Rectangle cropArea = new Rectangle(location, templateImage.Size);

        //        // Crop the region from the original image and add it to the list
        //        Bitmap matchedTemplate = new Crop(cropArea).Apply(originalImage);
        //        matchedTemplates.Add(matchedTemplate);
        //    }

        //    return matchedTemplates;
        //}


        //static List<Point> CombineResults(List<Point> localMatches, int regionX, int regionY)
        //{
        //    List<Point> globalMatches = new List<Point>();
        //    foreach (var match in localMatches)
        //    {
        //        globalMatches.Add(new Point(match.X + regionX, match.Y + regionY));
        //    }
        //    return globalMatches;
        //}


        //static List<Bitmap> DivideIntoOverlappingRegions2(Bitmap image, Size regionSize, int overlap)
        //{
        //    if (regionSize.Height <= 0)
        //        throw new ArgumentException("Region height must be positive");

        //    if (regionSize.Height > image.Height)
        //        throw new ArgumentException("Region height must be smaller than or equal to the image height");

        //    List<Bitmap> regions = new List<Bitmap>();

        //    for (int y = 0; y <= image.Height - regionSize.Height; y += regionSize.Height - overlap)
        //    {
        //        // Always use the full width of the image
        //        int regionWidth = image.Width;

        //        Rectangle regionRect = new Rectangle(0, y, regionWidth, regionSize.Height);
        //        Bitmap region = new Bitmap(width:regionRect.Width, height:regionRect.Height);
        //        //Bitmap region = new Bitmap(width: 4734, height: 925);

        //        using (Graphics g = Graphics.FromImage(region))
        //        {
        //            g.DrawImage(image, 0, 0, regionRect, GraphicsUnit.Pixel);
        //        }


        //        regions.Add(EnsureGrayscale(region));
        //    }

        //    return regions;
        //}




        //static Bitmap EnsureGrayscale(Bitmap image)
        //{
        //    if (image.PixelFormat != PixelFormat.Format8bppIndexed)
        //    {
        //        // Convert the temporary image to grayscale using AForge.NET
        //        Grayscale grayscaleFilter = Grayscale.CommonAlgorithms.BT709;
        //        Bitmap grayscaleImage = grayscaleFilter.Apply(image);
        //        return grayscaleImage;
        //    }
        //    return image;
        //}

        //static List<Point> PerformTemplateMatchingOnRegion(Bitmap region, Bitmap template)
        //{
        //    // Ensure the images are in grayscale
        //    Bitmap regionGray = EnsureGrayscale(region);
        //    Bitmap templateGray = EnsureGrayscale(template);

        //    Bitmap sampleGrayPad = PadToPowerOfTwo(regionGray);

        //    // Apply the Fast Matched Filter
        //    Bitmap correlationMap = FastMatchedFilter(sampleGrayPad, templateGray);

        //    // Locate peaks in the correlation map
        //    List<Point> peakLocations = FindPeaks(correlationMap);

        //    // Since we are working with a region, we don't need to extract the matched templates here
        //    // We just return the peak locations
        //    return peakLocations;
        //}



        //static List<Bitmap> PerformTemplateMatching(Bitmap sample, Bitmap template)
        //{

        //    Bitmap sampleGray = EnsureGrayscale(sample);
        //    Bitmap templateGray = EnsureGrayscale(template);

        //    Bitmap sampleGrayPad = PadToPowerOfTwo(sampleGray);
        //    sampleGray.Dispose();

        //    // Apply the Fast Matched Filter
        //    Bitmap correlationMap = FastMatchedFilter(sampleGrayPad, templateGray);
        //    sampleGrayPad.Dispose();

        //    // Locate peaks in the correlation map
        //    List<Point> peakLocations = FindPeaks(correlationMap);

        //    // Extract matched templates from the original image
        //    List<Bitmap> matchedTemplates = new List<Bitmap>();
        //    foreach (Point location in peakLocations)
        //    {
        //        Bitmap matchedTemplate = new Accord.Imaging.Filters.Crop(new Rectangle(location, template.Size)).Apply(sample);
        //        matchedTemplates.Add(matchedTemplate);
        //    }

        //    return matchedTemplates;
        //}
        //static Bitmap FastMatchedFilter(Bitmap sample, Bitmap template)
        //{
        //    // Pad the template image to match the size of the sample image
        //    Bitmap paddedTemplate = PadToPowerOfTwoV2(template, sample.Width, sample.Height);

        //    // Convert bitmaps to complex image format
        //    ComplexImage sampleComplex = ComplexImage.FromBitmap(sample);
        //    ComplexImage templateComplex = ComplexImage.FromBitmap(paddedTemplate);

        //    // Apply Fourier Transform
        //    sampleComplex.ForwardFourierTransform();
        //    templateComplex.ForwardFourierTransform();

        //    // Check dimensions
        //    if (sampleComplex.Width != templateComplex.Width || sampleComplex.Height != templateComplex.Height)
        //    {
        //        throw new InvalidOperationException("Dimensions of sample and template complex images do not match.");
        //    }
        //    var Width = templateComplex.Data.GetLength(1);
        //        var Height = templateComplex.Data.GetLength(0);

        //    if (Width != templateComplex.Width || Height != templateComplex.Height)
        //    {
        //        throw new InvalidOperationException("Dimensions of the templateComplex data array do not match its width and height.");
        //    }

        //    // Conjugate the template image in the frequency domain
        //    for (int y = 0; y < templateComplex.Width; y++)
        //    {
        //        for (int x = 0; x < templateComplex.Height; x++)
        //        {
        //            templateComplex.Data[x, y] = Complex.Conjugate(templateComplex.Data[x, y]);
        //        }
        //    }

        //    // Perform element-wise multiplication in the frequency domain
        //    ComplexImage resultComplex = ComplexImage.FromBitmap(new Bitmap(sampleComplex.Width, sampleComplex.Height,PixelFormat.Format8bppIndexed));
        //    for (int y = 0; y < sampleComplex.Width; y++)
        //    {
        //        for (int x = 0; x < sampleComplex.Height; x++)
        //        {
        //            resultComplex.Data[x, y] = sampleComplex.Data[x, y] * templateComplex.Data[x, y];
        //        }
        //    }

        //    // Apply Inverse Fourier Transform
        //    resultComplex.BackwardFourierTransform();

        //    // Convert back to bitmap
        //    Bitmap resultBitmap = resultComplex.ToBitmap();

        //    return resultBitmap;
        //}








        //static Bitmap PadToPowerOfTwo(Bitmap image)
        //{
        //    int width = NextPowerOfTwo(image.Width);
        //    int height = NextPowerOfTwo(image.Height);

        //    // Create a temporary image for drawing
        //    Bitmap tempImage = new Bitmap(width, height);

        //    // Draw the original image onto the temporary image
        //    using (Graphics g = Graphics.FromImage(tempImage))
        //    {
        //        g.DrawImage(image, 0, 0);
        //    }

        //    // Convert the temporary image to grayscale using AForge.NET
        //    Grayscale grayscaleFilter = Grayscale.CommonAlgorithms.BT709;
        //    Bitmap grayscaleImage = grayscaleFilter.Apply(tempImage);

        //    // Convert the grayscale image to 8bpp indexed format
        //    //Bitmap resultImage = Accord.Imaging.Image.Clone(grayscaleImage, PixelFormat.Format8bppIndexed);

        //    return grayscaleImage;
        //}


        //static Bitmap PadToPowerOfTwoV2(Bitmap image, int width, int height)
        //{

        //    // Create a temporary image for drawing
        //    Bitmap tempImage = new Bitmap(width: width, height: height);

        //    // Draw the original image onto the temporary image
        //    using (Graphics g = Graphics.FromImage(tempImage))
        //    {
        //        g.DrawImage(image, 0, 0);
        //    }

        //    // Convert the temporary image to grayscale using AForge.NET
        //    Grayscale grayscaleFilter = Grayscale.CommonAlgorithms.BT709;
        //    Bitmap grayscaleImage = grayscaleFilter.Apply(tempImage);

        //    // Convert the grayscale image to 8bpp indexed format
        //    //Bitmap resultImage = Accord.Imaging.Image.Clone(grayscaleImage, PixelFormat.Format8bppIndexed);

        //    return grayscaleImage;
        //}





        //static int NextPowerOfTwo(int value)
        //{
        //    int power = 1;
        //    while (power < value) power <<= 1;
        //    return power;
        //}

        //static Bitmap PadToSize(Bitmap image, int width, int height)
        //{
        //    // Create a temporary image for drawing
        //    Bitmap tempImage = new Bitmap(width, height);

        //    // Draw the original image onto the temporary image
        //    using (Graphics g = Graphics.FromImage(tempImage))
        //    {
        //        g.DrawImage(image, 0, 0);
        //    }

        //    // Convert the temporary image to grayscale using AForge.NET
        //    Grayscale grayscaleFilter = Grayscale.CommonAlgorithms.BT709;
        //    Bitmap grayscaleImage = grayscaleFilter.Apply(tempImage);

        //    return grayscaleImage;
        //}



        ////static List<Point> FindPeaks(Bitmap correlationMap, double threshold = 0.9)
        ////{
        ////    // Convert the correlation map to a grayscale image
        ////    Accord.Imaging.Filters.Grayscale grayscaleFilter = new Accord.Imaging.Filters.Grayscale(0.2125, 0.7154, 0.0721);
        ////    Bitmap grayMap = grayscaleFilter.Apply(correlationMap);

        ////    // Find peaks
        ////    List<Point> peakLocations = new List<Point>();
        ////    for (int y = 0; y < grayMap.Height; y++)
        ////    {
        ////        for (int x = 0; x < grayMap.Width; x++)
        ////        {
        ////            // Get the pixel intensity
        ////            Color pixel = grayMap.GetPixel(x, y);
        ////            double intensity = pixel.GetBrightness();

        ////            // If intensity is above the threshold, add to peaks
        ////            if (intensity > threshold)
        ////            {
        ////                peakLocations.Add(new Point(x, y));
        ////            }
        ////        }
        ////    }

        ////    return peakLocations;
        ////}

        //static List<Point> FindPeaks(Bitmap correlationMap, double threshold = 0.9)
        //{

        //    Bitmap grayMap = EnsureGrayscale(correlationMap);

        //    // Lock the bits for fast pixel manipulation
        //    BitmapData data = grayMap.LockBits(new Rectangle(0, 0, grayMap.Width, grayMap.Height), ImageLockMode.ReadOnly, grayMap.PixelFormat);

        //    // Find peaks
        //    List<Point> peakLocations = new List<Point>();

        //    unsafe
        //    {
        //        byte* ptr = (byte*)data.Scan0.ToPointer();
        //        int stride = data.Stride;

        //        for (int y = 0; y < data.Height; y++)
        //        {
        //            for (int x = 0; x < data.Width; x++)
        //            {
        //                byte intensity = ptr[y * stride + x];

        //                // Convert byte intensity to double (0 to 1)
        //                double normalizedIntensity = intensity / 255.0;

        //                // If intensity is above the threshold, add to peaks
        //                if (normalizedIntensity > threshold)
        //                {
        //                    peakLocations.Add(new Point(x, y));
        //                }
        //            }
        //        }
        //    }

        //    // Unlock the bits
        //    grayMap.UnlockBits(data);

        //    return peakLocations;
        //}
    }
}
