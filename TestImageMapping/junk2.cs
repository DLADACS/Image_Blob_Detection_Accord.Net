//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Numerics;
//using System.Threading.Tasks;
//using Accord.Imaging;
//using Accord.Imaging.Filters;

//class Program
//{

//    static void Main()
//    {
//        // Load the sample image
//        Bitmap sampleImage = new Bitmap("sample.tif");

//        // Load the template image
//        Bitmap templateImage = new Bitmap("template.tif");

//        // Define the scaling factor (0 to 1, where 1 is original size)
//        float scaleFactor = 0.3f;

//        Bitmap resizedSampleImage = ResizeImage(sampleImage, (int)(sampleImage.Width * scaleFactor), (int)(sampleImage.Height * scaleFactor));
//        Bitmap resizedTemplateImage = ResizeImage(templateImage, (int)(templateImage.Width * scaleFactor), (int)(templateImage.Height * scaleFactor));


//        //// Define the region size and overlap
//        //Size regionSize = new Size(templateImage.Width, templateImage.Height);
//        //int overlap = regionSize.Height / 2;

//        // Divide the sample image into overlapping regions
//        // Define padding percentage
//        int paddingPercentage = 20;

//        // Divide the sample image into overlapping regions
//        List<Rectangle> regions = DivideIntoOverlappingRegions(resizedSampleImage, resizedTemplateImage, paddingPercentage);


//        // Split the image into separate Bitmaps for each region
//        List<Bitmap> regionImages = SplitImage(resizedSampleImage, regions);

//        // Perform template matching on each region
//        List<TemplateMatch> matches = new List<TemplateMatch>();
//        List<TemplateMatch> adjustedMatches2 = new List<TemplateMatch>();


//        Parallel.ForEach(regionImages, (regionImage, _, index) =>
//        {

//            Console.WriteLine($"Processing image at index {index} on thread {System.Threading.Thread.CurrentThread.ManagedThreadId}");
//            Bitmap clonedTemplate;
//            using (Bitmap clonedImage = new Bitmap(regionImage))
//            {
//                lock (resizedTemplateImage)
//                {
//                    clonedTemplate = new Bitmap(resizedTemplateImage);
//                }
//                List<TemplateMatch> regionMatches = PerformTemplateMatchingOnRegion(EnsureGrayscale(clonedImage), EnsureGrayscale(clonedTemplate));

//                DrawMatchesOnImage(clonedImage, regionMatches);
//                clonedImage.Save($"RegionImageWithMatches_{index}.png");

//                Rectangle regionRect = regions[(int)index];

//                // Adjust match location to map back to large image coordinates
//                List<TemplateMatch> adjustedMatches = new List<TemplateMatch>();

//                foreach (var match in regionMatches)
//                {

//                    //var adjustedRectangle = new Rectangle(match.Rectangle.X, match.Rectangle.Y + regionRect.Y, match.Rectangle.Width, match.Rectangle.Height);

//                    var adjustedRectangle = new Rectangle(
//                        match.Rectangle.X + regionRect.X, // Adjust X coordinate
//                        match.Rectangle.Y + regionRect.Y, // Adjust Y coordinate
//                        match.Rectangle.Width,
//                        match.Rectangle.Height
//                    );

//                    adjustedMatches.Add(new TemplateMatch(adjustedRectangle, match.Similarity));
//                    Console.WriteLine($"Region: {regionRect}");
//                    Console.WriteLine($"Original Match: {match.Rectangle}");
//                    Console.WriteLine($"Adjusted Match: {adjustedRectangle}");
//                }

//                // Add matches to the global list
//                lock (matches)
//                {
//                    matches.AddRange(adjustedMatches);
//                }

//            }
//            clonedTemplate.Dispose();

//        });






//        // Output the results
//        Console.WriteLine("Found matches:");
//        foreach (var match in matches)
//        {
//            var adjustedRectangle = new Rectangle((int)(match.Rectangle.X / scaleFactor), (int)(match.Rectangle.Y / scaleFactor), (int)(match.Rectangle.Width / scaleFactor), (int)(match.Rectangle.Height / scaleFactor));
//            TemplateMatch AjustedMatchs = new TemplateMatch(adjustedRectangle, match.Similarity);
//            adjustedMatches2.Add(AjustedMatchs);
//            Console.WriteLine($"X: {AjustedMatchs.Rectangle.X}, Y: {AjustedMatchs.Rectangle.Y}, Similarity: {AjustedMatchs.Similarity}");
//        }




//        SaveMatchedTemplates(sampleImage, adjustedMatches2);
//        Console.WriteLine("This is the end");
//    }


//    static Bitmap ResizeImage(Bitmap image, int width, int height)
//    {
//        Bitmap resizedImage = new Bitmap(width, height);
//        using (Graphics graphics = Graphics.FromImage(resizedImage))
//        {
//            graphics.DrawImage(image, 0, 0, width, height);
//        }
//        return resizedImage;
//    }



//    static List<Bitmap> SplitImage(Bitmap image, List<Rectangle> regions)
//    {
//        List<Bitmap> regionImages = new List<Bitmap>();

//        foreach (var region in regions)
//        {
//            Bitmap regionImage = new Bitmap(region.Width, region.Height, PixelFormat.Format24bppRgb);

//            using (Graphics g = Graphics.FromImage(regionImage))
//            {
//                g.DrawImage(image, new Rectangle(0, 0, region.Width, region.Height), region, GraphicsUnit.Pixel);
//            }

//            regionImages.Add(regionImage);
//        }

//        return regionImages;
//    }

//    static Bitmap EnsureGrayscale(Bitmap image)
//    {
//        if (image.PixelFormat != PixelFormat.Format8bppIndexed)
//        {
//            // Convert the temporary image to grayscale using AForge.NET
//            Grayscale grayscaleFilter = Grayscale.CommonAlgorithms.BT709;
//            Bitmap grayscaleImage = grayscaleFilter.Apply(image);
//            return grayscaleImage;
//        }
//        return image;
//    }


//    static List<TemplateMatch> PerformTemplateMatchingOnRegion(Bitmap regionImage, Bitmap templateImage)
//    {
//        ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0.9f); // Adjust the similarity threshold as needed
//        TemplateMatch[] regionMatches = tm.ProcessImage(regionImage, templateImage);

//        return new List<TemplateMatch>(regionMatches);
//    }

//    static List<Rectangle> DivideIntoOverlappingRegions(Bitmap image, Bitmap templateImage, int paddingPercentage)
//    {
//        List<Rectangle> regions = new List<Rectangle>();

//        int templateWidth = templateImage.Width;
//        int templateHeight = templateImage.Height;

//        // Calculate padding based on the template size and the padding percentage
//        int paddingWidth = (int)(templateWidth * (paddingPercentage / 100.0));
//        int paddingHeight = (int)(templateHeight * (paddingPercentage / 100.0));

//        // Ensure the region size is at least as big as the template size plus padding
//        int regionWidth = Math.Max(image.Width, templateWidth + paddingWidth);
//        int regionHeight = Math.Max(templateHeight + paddingHeight, templateHeight);

//        // Calculate overlap
//        int overlap = regionHeight / 6;

//        for (int y = 0; y <= image.Height - regionHeight; y += regionHeight - overlap)
//        {
//            regions.Add(new Rectangle(0, y, regionWidth, regionHeight));
//        }
//        return regions;
//    }





//    static void SaveMatchedTemplates(Bitmap originalImage, List<TemplateMatch> matches)
//    {
//        for (int i = 0; i < matches.Count; i++)
//        {
//            var match = matches[i];
//            var cropArea = match.Rectangle;

//            Console.WriteLine($"Saving match {i + 1}: X: {match.Rectangle.X}, Y: {match.Rectangle.Y}, Similarity: {match.Similarity}");

//            // Crop the matched template from the original image
//            using (Bitmap matchedTemplate = originalImage.Clone(cropArea, originalImage.PixelFormat))
//            {
//                // Save the matched template with a unique filename
//                string filename = $"MatchedTemplate_{match.Rectangle.X}_{match.Rectangle.Y}_{i + 1}.png";
//                matchedTemplate.Save(filename);
//                Console.WriteLine($"Saved {filename}");
//            }
//        }
//    }

//    static void DrawMatchesOnImage(Bitmap image, List<TemplateMatch> matches)
//    {
//        using (Graphics g = Graphics.FromImage(image))
//        {
//            foreach (var match in matches)
//            {
//                g.DrawRectangle(new Pen(Color.Red, 2), match.Rectangle);
//            }
//        }
//    }




//}
