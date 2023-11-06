using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Math.Optimization.Losses;
using System.Linq;
using DACS.CBEDS.ENERGY;

class Program
{


    static void Main()
    {
        string inputImage = @"Kelly Image.tif";

        // 1. Instantiate DocumentTypeConfiguration and FormExtractorFactory
        DocumentTypeConfiguration documentTypeConfiguration = new DocumentTypeConfiguration();
        FormExtractorFactory formExtractorFactory = new FormExtractorFactory(documentTypeConfiguration);
        IFormExtractor formExtractor = formExtractorFactory.GetFormExtractor("Energy DD1898 Triple");

        // 4. Call ExtractForms on the form extractor instance Only if we get a FormExtractor
        
            List<string> extractedImages = formExtractor.ExtractForms(inputImage, @"C:\Users\chood\source\repos\TestImageMapping\TestImageMapping\bin\x64\Debug\TempResults\testingresult.tif");


            Console.WriteLine("Extracted images:");
            foreach (var image in extractedImages)
            {
                Console.WriteLine(image);
            }
            Console.WriteLine("Done");
        }


    static void MainFolderWorking()
        {
            string inputFolder = @"C:\Users\chood\source\repos\TestImageMapping\TestImageMapping\bin\x64\Debug\TempImages\";
            string outputFolder = @"C:\Users\chood\source\repos\TestImageMapping\TestImageMapping\bin\x64\Debug\TempResults\";

            // Check if output folder exists, if not, create it
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // Get all .tif files in the input folder
            string[] imageFiles = Directory.GetFiles(inputFolder, "*.tif");
            foreach (string imageFilePath in imageFiles)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imageFilePath);
                string outputSubFolder = Path.Combine(outputFolder, fileNameWithoutExtension);

                // Check if output subfolder exists, if not, create it
                if (!Directory.Exists(outputSubFolder))
                {
                    Directory.CreateDirectory(outputSubFolder);
                }

                Console.WriteLine($"Processing {imageFilePath}");

                // Load the sample image
                Bitmap sampleImage = new Bitmap(imageFilePath);

                // Convert the image to a non-indexed pixel format
                Bitmap nonIndexedImage = new Bitmap(sampleImage.Width, sampleImage.Height, PixelFormat.Format24bppRgb);
                nonIndexedImage.SetResolution(sampleImage.HorizontalResolution, sampleImage.VerticalResolution);
                using (Graphics g = Graphics.FromImage(nonIndexedImage))
                {
                    g.DrawImage(sampleImage, 0, 0);
                }

                // Convert the image to grayscale
                Grayscale grayscaleFilter = new Grayscale(0.2125, 0.7154, 0.0721);
                Bitmap grayscaleImage = grayscaleFilter.Apply(nonIndexedImage);

                // Apply a threshold to create a binary image
                Threshold thresholdFilter = new Threshold(100);
                Bitmap binaryImage = thresholdFilter.Apply(grayscaleImage);

                // Invert the image if necessary (make sure objects are white and background is black)
                Invert invertFilter = new Invert();
                invertFilter.ApplyInPlace(binaryImage);

                // Perform blob detection
                BlobCounter blobCounter = new BlobCounter
                {
                    FilterBlobs = true,
                    MinHeight = 300,
                    MinWidth = 750,
                    MaxHeight = 1000
                };
                blobCounter.ProcessImage(binaryImage);
                Blob[] blobs = blobCounter.GetObjectsInformation();

                // Draw rectangles around detected blobs and save each blob as an image
                Bitmap resultImage = (Bitmap)nonIndexedImage.Clone();
                using (Graphics g = Graphics.FromImage(resultImage))
                {
                    Pen redPen = new Pen(Color.Green, 10);
                    int padding = 10; // Set padding as required for top, left, and right
                    int bottomPadding = 75; // Set padding for bottom
                    int blobCounterint = 0;
                    foreach (Blob blob in blobs)
                    {
                        // Calculate the padded rectangle
                        Rectangle paddedRect = new Rectangle(
                            blob.Rectangle.X - padding,
                            blob.Rectangle.Y - padding,
                            Math.Min(blob.Rectangle.Width + 2 * padding, sampleImage.Width - blob.Rectangle.X + padding),
                            Math.Min(blob.Rectangle.Height + padding + bottomPadding, sampleImage.Height - blob.Rectangle.Y + padding)
                        );

                        // Draw rectangle around detected blob
                        g.DrawRectangle(redPen, paddedRect);

                        // Save each blob as an image
                        using (Bitmap blobImage = nonIndexedImage.Clone(paddedRect, nonIndexedImage.PixelFormat))
                        {
                            string outputFilePath = Path.Combine(outputSubFolder, $"Blob_{blobCounterint}.png");
                            blobImage.Save(outputFilePath);
                            Console.WriteLine($"Saved {outputFilePath}");
                        }
                        blobCounterint++;
                    }
                }

                // Save the result image
                string resultImagePath = Path.Combine(outputSubFolder, "ResultImageBlobDetection.png");
                resultImage.Save(resultImagePath);
                Console.WriteLine($"Result image saved as '{resultImagePath}'\n");
            }

            Console.WriteLine("Processing complete!");
        }


        static List<string> ExtractFormsFromImage2(string imagePath)
        {
            ConcurrentBag<string> extractedImagePaths = new ConcurrentBag<string>();

            using (Bitmap sampleImage = new Bitmap(imagePath))
            using (Bitmap nonIndexedImage = new Bitmap(sampleImage.Width, sampleImage.Height, PixelFormat.Format24bppRgb))
            {
                nonIndexedImage.SetResolution(sampleImage.HorizontalResolution, sampleImage.VerticalResolution);

                using (Graphics g = Graphics.FromImage(nonIndexedImage))
                {
                    g.DrawImage(sampleImage, 0, 0);
                }

                Threshold thresholdFilter = new Threshold(100);
                Grayscale grayscaleFilter = new Grayscale(0.2125, 0.7154, 0.0721);
                using (Bitmap grayscaleImage = grayscaleFilter.Apply(nonIndexedImage))
                using (Bitmap binaryImage = thresholdFilter.Apply(grayscaleImage))
                {
                    Invert invertFilter = new Invert();
                    invertFilter.ApplyInPlace(binaryImage);

                    BlobCounter blobCounter = new BlobCounter
                    {
                        FilterBlobs = true,
                        MinHeight = 300,
                        MinWidth = 750
                    };
                    blobCounter.ProcessImage(binaryImage);
                    Blob[] blobs = blobCounter.GetObjectsInformation();

                    int padding = 10; // Set padding as required for top, left, and right
                    int bottomPadding = 75; // Set padding for bottom

                    List<Bitmap> blobImages = new List<Bitmap>();
                    foreach (var blob in blobs)
                    {
                        int paddedX = Math.Max(blob.Rectangle.X - padding, 0);
                        int paddedY = Math.Max(blob.Rectangle.Y - padding, 0);
                        int paddedWidth = Math.Min(blob.Rectangle.Width + 2 * padding, sampleImage.Width - paddedX);
                        int paddedHeight = Math.Min(blob.Rectangle.Height + padding + bottomPadding, sampleImage.Height - paddedY);

                        Rectangle paddedRect = new Rectangle(paddedX, paddedY, paddedWidth, paddedHeight);
                        Bitmap blobImage = nonIndexedImage.Clone(paddedRect, nonIndexedImage.PixelFormat);
                        blobImages.Add(blobImage);
                    }

                    Parallel.ForEach(blobImages, blobImage =>
                    {
                        string tempFilePath = Path.GetTempFileName();
                        tempFilePath = Path.ChangeExtension(tempFilePath, "tif");
                        blobImage.Save(tempFilePath, ImageFormat.Tiff);
                        extractedImagePaths.Add(tempFilePath);
                        blobImage.Dispose();
                    });
                }
            }

            return extractedImagePaths.ToList();
        }



        static List<string> ExtractFormsFromImage(string imagePath)
        {
            List<string> extractedImagePaths = new List<string>();

            // Load the sample image
            Bitmap sampleImage = new Bitmap(imagePath);

            // Convert the image to a non-indexed pixel format
            Bitmap nonIndexedImage = new Bitmap(sampleImage.Width, sampleImage.Height, PixelFormat.Format24bppRgb);
            nonIndexedImage.SetResolution(sampleImage.HorizontalResolution, sampleImage.VerticalResolution);
            using (Graphics g = Graphics.FromImage(nonIndexedImage))
            {
                g.DrawImage(sampleImage, 0, 0);
            }

            // Convert the image to grayscale
            Grayscale grayscaleFilter = new Grayscale(0.2125, 0.7154, 0.0721);
            Bitmap grayscaleImage = grayscaleFilter.Apply(nonIndexedImage);

            // Apply a threshold to create a binary image
            Threshold thresholdFilter = new Threshold(100);
            Bitmap binaryImage = thresholdFilter.Apply(grayscaleImage);

            // Invert the image if necessary (make sure objects are white and background is black)
            Invert invertFilter = new Invert();
            invertFilter.ApplyInPlace(binaryImage);

            // Perform blob detection
            BlobCounter blobCounter = new BlobCounter
            {
                FilterBlobs = true,
                MinHeight = 300,
                MinWidth = 750
            };
            blobCounter.ProcessImage(binaryImage);
            Blob[] blobs = blobCounter.GetObjectsInformation();

            // Draw rectangles around detected blobs and save each blob as a temporary image file
            Bitmap resultImage = (Bitmap)nonIndexedImage.Clone();
            using (Graphics g = Graphics.FromImage(resultImage))
            {
                Pen redPen = new Pen(Color.Green, 10);
                int padding = 10; // Set padding as required for top, left, and right
                int bottomPadding = 75; // Set padding for bottom
                int blobCounterint = 0;
                foreach (Blob blob in blobs)
                {
                    // Calculate the padded rectangle
                    Rectangle paddedRect = new Rectangle(
                        blob.Rectangle.X - padding,
                        blob.Rectangle.Y - padding,
                        Math.Min(blob.Rectangle.Width + 2 * padding, sampleImage.Width - blob.Rectangle.X + padding),
                        Math.Min(blob.Rectangle.Height + padding + bottomPadding, sampleImage.Height - blob.Rectangle.Y + padding)
                    );

                    // Draw rectangle around detected blob
                    g.DrawRectangle(redPen, paddedRect);

                    // Save each blob as a temporary image file
                    using (Bitmap blobImage = nonIndexedImage.Clone(paddedRect, nonIndexedImage.PixelFormat))
                    {
                        string tempFilePath = Path.GetTempFileName();
                        tempFilePath = Path.ChangeExtension(tempFilePath, "tif");
                        blobImage.Save(tempFilePath, ImageFormat.Png);
                        extractedImagePaths.Add(tempFilePath);
                    }
                    blobCounterint++;
                }
            }

            // Optionally, save the result image with drawn rectangles
            //string resultImagePath = Path.GetTempFileName();
            //resultImagePath = Path.ChangeExtension(resultImagePath, "png");
            //resultImage.Save(resultImagePath, ImageFormat.Png);
            //extractedImagePaths.Add(resultImagePath);

            return extractedImagePaths;
        }


        //static void Main()
        //{

        //    // Load the sample image
        //    Bitmap sampleImage = new Bitmap("sample.tif");

        //    // Convert the image to a non-indexed pixel format
        //    Bitmap nonIndexedImage = new Bitmap(sampleImage.Width, sampleImage.Height, PixelFormat.Format24bppRgb);
        //    nonIndexedImage.SetResolution(sampleImage.HorizontalResolution, sampleImage.VerticalResolution);
        //    using (Graphics g = Graphics.FromImage(nonIndexedImage))
        //    {
        //        g.DrawImage(sampleImage, 0, 0);
        //    }

        //    // Convert the image to grayscale
        //    Grayscale grayscaleFilter = new Grayscale(0.2125, 0.7154, 0.0721);
        //    Bitmap grayscaleImage = grayscaleFilter.Apply(nonIndexedImage);

        //    // Apply a threshold to create a binary image
        //    Threshold thresholdFilter = new Threshold(100);
        //    Bitmap binaryImage = thresholdFilter.Apply(grayscaleImage);

        //    // Invert the image if necessary (make sure objects are white and background is black)
        //    Invert invertFilter = new Invert();
        //    invertFilter.ApplyInPlace(binaryImage);

        //    // Perform blob detection
        //    BlobCounter blobCounter = new BlobCounter();
        //    blobCounter.FilterBlobs = true;
        //    blobCounter.MinHeight = 300; // Set minimum height for blobs
        //    blobCounter.MinWidth = 750;  // Set minimum width for blobs
        //    blobCounter.ProcessImage(binaryImage);
        //    Blob[] blobs = blobCounter.GetObjectsInformation();

        //    // Draw rectangles around detected blobs and save each blob as an image
        //    Bitmap resultImage = (Bitmap)nonIndexedImage.Clone();
        //    using (Graphics g = Graphics.FromImage(resultImage))
        //    {
        //        Pen redPen = new Pen(Color.Green, 10);
        //        int padding = 10; // Set padding as required for top, left, and right
        //        int bottomPadding = 75; // Set padding for bottom
        //        int blobCounterint = 0;
        //        foreach (Blob blob in blobs)
        //        {
        //            // Calculate the padded rectangle
        //            Rectangle paddedRect = new Rectangle(
        //                blob.Rectangle.X - padding,
        //                blob.Rectangle.Y - padding,
        //                Math.Min(blob.Rectangle.Width + 2 * padding, sampleImage.Width - blob.Rectangle.X + padding),
        //                Math.Min(blob.Rectangle.Height + padding + bottomPadding, sampleImage.Height - blob.Rectangle.Y + padding)
        //            );

        //            // Draw rectangle around detected blob
        //            g.DrawRectangle(redPen, paddedRect);

        //            // Save each blob as an image
        //            using (Bitmap blobImage = nonIndexedImage.Clone(paddedRect, nonIndexedImage.PixelFormat))
        //            {
        //                blobImage.Save($"Blob_{blobCounterint}.png");
        //            }
        //            blobCounterint++;
        //        }
        //    }

        //    // Save the result image
        //    resultImage.Save("ResultImageBlobDetection.png");
        //    Console.WriteLine("Result image saved as 'ResultImageBlobDetection.png'");
        //}






        //static void Main()
        //{
        //    // Load the sample image
        //    Bitmap sampleImage = new Bitmap("sample.tif");

        //    // Convert the image to a non-indexed pixel format
        //    Bitmap nonIndexedImage = new Bitmap(sampleImage.Width, sampleImage.Height, PixelFormat.Format24bppRgb);
        //    nonIndexedImage.SetResolution(sampleImage.HorizontalResolution, sampleImage.VerticalResolution);
        //    using (Graphics g = Graphics.FromImage(nonIndexedImage))
        //    {
        //        g.PageUnit = GraphicsUnit.Pixel;
        //        g.DrawImage(sampleImage, 0, 0);
        //    }
        //    nonIndexedImage.Save("NoneIndexed.tif");

        //    // Convert the image to grayscale
        //    Bitmap grayscaleImage = EnsureGrayscale(nonIndexedImage);
        //    grayscaleImage.Save("GrayScale.tif");

        //    // Enhance contrast
        //    ContrastStretch filter = new ContrastStretch();
        //    filter.ApplyInPlace(sampleImage);

        //    // Apply Gaussian smoothing
        //    GaussianBlur blur = new GaussianBlur(2.0);
        //    Bitmap smoothedImage = blur.Apply(sampleImage);

        //    // Apply the Canny edge detector
        //    CannyEdgeDetector cannyFilter = new CannyEdgeDetector();
        //    Bitmap edgeImage = cannyFilter.Apply(smoothedImage);
        //    // Invert the colors of the edge image
        //    edgeImage.Save("EdgeImage.tif");



        //    // Optional: You can apply a threshold after edge detection if needed
        //    Threshold thresholdFilter = new Threshold(100);
        //    Bitmap binaryEdgeImage = thresholdFilter.Apply(edgeImage);
        //    binaryEdgeImage.Save("BinaryEdgeImage.tif");

        //    // Perform blob detection on the edge image
        //    BlobCounter blobCounter = new BlobCounter();
        //    blobCounter.FilterBlobs = true;
        //    blobCounter.MinHeight = 50; // Set minimum height for blobs
        //    blobCounter.MinWidth = 50;  // Set minimum width for blobs
        //    blobCounter.MaxWidth = 2200;
        //    blobCounter.MaxHeight = 1183;
        //    blobCounter.ProcessImage(edgeImage);
        //    Blob[] blobs = blobCounter.GetObjectsInformation();

        //    // Draw rectangles around detected blobs
        //    Bitmap resultImage = (Bitmap)nonIndexedImage.Clone();
        //    using (Graphics g = Graphics.FromImage(resultImage))
        //    {
        //        Pen redPen = new Pen(Color.Green, 20);
        //        foreach (Blob blob in blobs)
        //        {
        //            g.DrawRectangle(redPen, blob.Rectangle);
        //        }
        //    }

        //    // Save the result image
        //    resultImage.Save("ResultImageEdgeDetection.png");
        //    Console.WriteLine("Result image saved as 'ResultImageEdgeDetection.png'");
        //}



        //static void Main()
        //{

        //    // Load the sample image
        //    Bitmap sampleImage = new Bitmap("sample.tif");

        //    // Convert the image to a non-indexed pixel format
        //    Bitmap nonIndexedImage = new Bitmap(sampleImage.Width, sampleImage.Height, PixelFormat.Format24bppRgb);
        //    nonIndexedImage.SetResolution(sampleImage.HorizontalResolution, sampleImage.VerticalResolution);
        //    using (Graphics g = Graphics.FromImage(nonIndexedImage))
        //    {
        //        g.PageUnit = GraphicsUnit.Pixel;
        //        g.DrawImage(sampleImage, 0, 0);
        //    }
        //    nonIndexedImage.Save("NoneIndexed.tif");

        //    Bitmap grayscaleImage = EnsureGrayscale(nonIndexedImage);
        //    grayscaleImage.Save("GrayScale.tif");
        //    // Apply a threshold to create a binary image
        //    Threshold thresholdFilter = new Threshold(100);
        //    Bitmap binaryImage = thresholdFilter.Apply(grayscaleImage);
        //    binaryImage.Save("binaryImage.tif");

        //    //// Invert the image if necessary (make sure objects are white and background is black)
        //    Invert invertFilter = new Invert();
        //    invertFilter.ApplyInPlace(binaryImage);

        //    // Perform blob detection
        //    BlobCounter blobCounter = new BlobCounter();
        //    blobCounter.FilterBlobs = true;
        //    blobCounter.MinHeight = 350; // Set minimum height for blobs
        //    blobCounter.MinWidth = 600;  // Set minimum width for blobs
        //    blobCounter.MaxWidth = 2200;
        //    blobCounter.MaxHeight = 1183;
        //    blobCounter.ProcessImage(binaryImage);
        //    Blob[] blobs = blobCounter.GetObjectsInformation();

        //    // Draw rectangles around detected blobs
        //    Bitmap resultImage = (Bitmap)nonIndexedImage.Clone();
        //    using (Graphics g = Graphics.FromImage(resultImage))
        //    {
        //        Pen redPen = new Pen(Color.Green, 20);
        //        foreach (Blob blob in blobs)
        //        {
        //            g.DrawRectangle(redPen, blob.Rectangle);
        //        }
        //    }

        //    // Save the result image
        //    resultImage.Save("ResultImageBlobDetection.png");
        //    Console.WriteLine("Result image saved as 'ResultImageBlobDetection.png'");
        //}


        //    static void MainOld()
        //    {
        //        // Load the sample image
        //        Bitmap sampleImage = new Bitmap("sample.tif");

        //        // Load the template image
        //        Bitmap templateImage = new Bitmap("templatev2.tif");

        //        // Define the scaling factor (0 to 1, where 1 is original size)
        //        float scaleFactor = 0.1f;

        //        Bitmap resizedSampleImage = ResizeImage(sampleImage, (int)(sampleImage.Width * scaleFactor), (int)(sampleImage.Height * scaleFactor));
        //        Bitmap resizedTemplateImage = ResizeImage(templateImage, (int)(templateImage.Width * scaleFactor), (int)(templateImage.Height * scaleFactor));

        //        List<TemplateMatch> matches = new List<TemplateMatch>();

        //        // Perform template matching on the resized images
        //        Bitmap clonedTemplate;
        //        using (Bitmap clonedImage = new Bitmap(resizedSampleImage))
        //        {
        //            clonedTemplate = new Bitmap(resizedTemplateImage);
        //            clonedTemplate.Save("ResizedImageTemplate.png");
        //            clonedImage.Save("ResizedImageSource.png"); 
        //            List<TemplateMatch> regionMatches = PerformTemplateMatchingOnRegion(EnsureGrayscale(clonedImage), EnsureGrayscale(clonedTemplate));

        //            DrawMatchesOnImage(clonedImage, regionMatches);
        //            clonedImage.Save("ResizedImageWithMatches.png");

        //            // Adjust match location to map back to large image coordinates
        //            List<TemplateMatch> adjustedMatches = new List<TemplateMatch>();
        //            foreach (var match in regionMatches)
        //            {
        //                var adjustedRectangle = new Rectangle(
        //                    (int)(match.Rectangle.X / scaleFactor),
        //                    (int)(match.Rectangle.Y / scaleFactor),
        //                    (int)(match.Rectangle.Width / scaleFactor),
        //                    (int)(match.Rectangle.Height / scaleFactor)
        //                );

        //                adjustedMatches.Add(new TemplateMatch(adjustedRectangle, match.Similarity));
        //                Console.WriteLine($"Original Match: {match.Rectangle}");
        //                Console.WriteLine($"Adjusted Match: {adjustedRectangle}");
        //            }

        //            // Add matches to the global list
        //            matches.AddRange(adjustedMatches);
        //        }
        //        clonedTemplate.Dispose();

        //        // Output the results
        //        Console.WriteLine("Found matches:");
        //        foreach (var match in matches)
        //        {
        //            Console.WriteLine($"X: {match.Rectangle.X}, Y: {match.Rectangle.Y}, Similarity: {match.Similarity}");
        //        }

        //        SaveMatchedTemplates(sampleImage, matches);
        //        Console.WriteLine("This is the end");
        //    }


        //    static Bitmap ResizeImage(Bitmap image, int width, int height)
        //{
        //    Bitmap resizedImage = new Bitmap(width, height);
        //    using (Graphics graphics = Graphics.FromImage(resizedImage))
        //    {
        //        graphics.DrawImage(image, 0, 0, width, height);
        //    }
        //    return resizedImage;
        //}



        //static List<Bitmap> SplitImage(Bitmap image, List<Rectangle> regions)
        //{
        //    List<Bitmap> regionImages = new List<Bitmap>();

        //    foreach (var region in regions)
        //    {
        //        Bitmap regionImage = new Bitmap(region.Width, region.Height, PixelFormat.Format24bppRgb);

        //        using (Graphics g = Graphics.FromImage(regionImage))
        //        {
        //            g.DrawImage(image, new Rectangle(0, 0, region.Width, region.Height), region, GraphicsUnit.Pixel);
        //        }

        //        regionImages.Add(regionImage);
        //    }

        //    return regionImages;
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


        //static List<TemplateMatch> PerformTemplateMatchingOnRegion(Bitmap regionImage, Bitmap templateImage)
        //{
        //    ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0.91f); // Adjust the similarity threshold as needed
        //    TemplateMatch[] regionMatches = tm.ProcessImage(regionImage, templateImage);

        //    return new List<TemplateMatch>(regionMatches);
        //}

        //static List<Rectangle> DivideIntoOverlappingRegions(Bitmap image, Bitmap templateImage, int paddingPercentage)
        //{
        //    List<Rectangle> regions = new List<Rectangle>();

        //    int templateWidth = templateImage.Width;
        //    int templateHeight = templateImage.Height;

        //    // Calculate padding based on the template size and the padding percentage
        //    int paddingWidth = (int)(templateWidth * (paddingPercentage / 100.0));
        //    int paddingHeight = (int)(templateHeight * (paddingPercentage / 100.0));

        //    // Ensure the region size is at least as big as the template size plus padding
        //    int regionWidth = Math.Max(image.Width, templateWidth + paddingWidth);
        //    int regionHeight = Math.Max(templateHeight + paddingHeight, templateHeight);

        //    // Calculate overlap
        //    int overlap = regionHeight / 6;

        //    for (int y = 0; y <= image.Height - regionHeight; y += regionHeight - overlap)
        //    {
        //        regions.Add(new Rectangle(0, y, regionWidth, regionHeight));
        //    }
        //    return regions;
        //}





        //static void SaveMatchedTemplates(Bitmap originalImage, List<TemplateMatch> matches)
        //{
        //    for (int i = 0; i < matches.Count; i++)
        //    {
        //        var match = matches[i];
        //        var cropArea = match.Rectangle;

        //        Console.WriteLine($"Saving match {i + 1}: X: {match.Rectangle.X}, Y: {match.Rectangle.Y}, Similarity: {match.Similarity}");

        //        // Crop the matched template from the original image
        //        using (Bitmap matchedTemplate = originalImage.Clone(cropArea, originalImage.PixelFormat))
        //        {
        //            // Save the matched template with a unique filename
        //            string filename = $"MatchedTemplate_{match.Rectangle.X}_{match.Rectangle.Y}_{i + 1}.png";
        //            matchedTemplate.Save(filename);
        //            Console.WriteLine($"Saved {filename}");
        //        }
        //    }
        //}

        //static void DrawMatchesOnImage(Bitmap image, List<TemplateMatch> matches)
        //{
        //    using (Graphics g = Graphics.FromImage(image))
        //    {
        //        foreach (var match in matches)
        //        {
        //            g.DrawRectangle(new Pen(Color.Red, 2), match.Rectangle);
        //        }
        //    }
        //}




    }
