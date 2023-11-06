using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml.Linq;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Math.Optimization.Losses;


namespace DACS.CBEDS.ENERGY
{
    public interface IFormExtractor
    {
        List<string> ExtractForms(string imagePath, String FailedImagePath);
    }

    public class DocumentTypeMetadata
    {
        public string Name { get; set; }
        public int ItemsPerPage { get; set; }
        public Dictionary<string, object> Configurations { get; set; }
        public string MinHeight { get; set; } = "10%"; // Default to 10% of the image height
        public string MinWidth { get; set; } = "10%"; // Default to 10% of the image width
        public string MaxHeight { get; set; } = "50%"; // Default to 50% of the image height
        /// <summary>
        /// When using seperators, This tells us how many pages can be behind this doctype
        /// </summary>
        public bool SeperatorSplitPerPage { get; set; } = true;
        public bool SeperatorAllowAsSubpage { get; set; } = false;

        public DocumentTypeMetadata(string name, int itemsPerPage = 1, Dictionary<string, object> configurations = null, string maxHeight = "50%", string minHeight = "10%", string minWidth = "10%", bool seperatorsplitperpage = true, bool seperatorallowassubpage = false)
        {
            Name = name;
            ItemsPerPage = itemsPerPage;
            Configurations = configurations ?? new Dictionary<string, object>();
            MaxHeight = maxHeight;
            MinHeight = minHeight;
            MinWidth = minWidth;
            SeperatorSplitPerPage = seperatorsplitperpage;
            SeperatorAllowAsSubpage = seperatorallowassubpage;
        }

        public int GetMaxHeight(int imageHeight) => CalculateDimension(MaxHeight, imageHeight);
        public int GetMinHeight(int imageHeight) => CalculateDimension(MinHeight, imageHeight);
        public int GetMinWidth(int imageWidth) => CalculateDimension(MinWidth, imageWidth);

        private int CalculateDimension(string dimensionValue, int imageSize)
        {
            if (dimensionValue.EndsWith("%"))
            {
                if (int.TryParse(dimensionValue.TrimEnd('%'), out int percentage))
                {
                    return imageSize * percentage / 100;
                }
                throw new InvalidOperationException($"Invalid percentage value for dimension: {dimensionValue}.");
            }
            else if (int.TryParse(dimensionValue, out int pixelSize))
            {
                return pixelSize;
            }
            throw new InvalidOperationException($"Invalid value for dimension: {dimensionValue}.");
        }
    }


    public class DocumentTypeConfiguration
    {
        private readonly Dictionary<string, DocumentTypeMetadata> _documentTypes;

        public DocumentTypeConfiguration()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            _documentTypes = new Dictionary<string, DocumentTypeMetadata>(comparer);

            AddDocumentType(new DocumentTypeMetadata(name: "Energy DD1898 Triple", itemsPerPage: 3, maxHeight: "50%", minHeight: "500", minWidth: "2050", seperatorsplitperpage: true, seperatorallowassubpage: false));
            AddDocumentType(new DocumentTypeMetadata(name: "Energy DD1898 Double", itemsPerPage: 2, maxHeight: "50%", minHeight: "500", minWidth: "2050", seperatorsplitperpage: true, seperatorallowassubpage: false));
            AddDocumentType(new DocumentTypeMetadata(name: "Energy DD1898 Single", itemsPerPage: 1, maxHeight: "60%", minHeight: "500", minWidth: "2050", seperatorsplitperpage: true, seperatorallowassubpage: false));
            AddDocumentType(new DocumentTypeMetadata(name: "Energy DD1898", seperatorsplitperpage: true, seperatorallowassubpage: false));
            AddDocumentType(new DocumentTypeMetadata(name: "Energy DD250", itemsPerPage: 1, maxHeight: "100%", minHeight: "90%", minWidth: "90%"));
            // Add more document types as needed...
        }

        private void AddDocumentType(DocumentTypeMetadata documentType)
        {
            _documentTypes[documentType.Name] = documentType;
        }

        public DocumentTypeMetadata GetDocumentTypeMetadata(string documentType)
        {
            if (_documentTypes.TryGetValue(documentType, out DocumentTypeMetadata metadata))
            {
                return metadata;
            }

            throw new InvalidOperationException($"Unsupported document type: {documentType}");
        }
    }




    public abstract class BaseFormExtractor : IFormExtractor
    {
        protected DocumentTypeMetadata DocumentTypeMetadata { get; }

        protected BaseFormExtractor(DocumentTypeConfiguration documentTypeConfiguration, string documentType)
        {
            DocumentTypeMetadata = documentTypeConfiguration.GetDocumentTypeMetadata(documentType);
        }


        public virtual List<string> ExtractForms(string imagePath, string failedImagePath)
        {
            //Make sure the failed Image and directory paths are avliable.
            string directoryPathv = Path.GetDirectoryName(failedImagePath);
            if (!Directory.Exists(directoryPathv))
            {
                Directory.CreateDirectory(directoryPathv);
            }
            //List of all Temp Images so we can cleanup after ourselves if we dont need them
            List<string> tempImagePaths = new List<string>();

            List<string> extractedImagePaths = new List<string>();
            Bitmap maxBlobImage = null;
            Blob[] maxBlobs = null;
            int maxBlobsCount = 0;
            string dilationTypeUsed = "";

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

                    // Define a list of morphology filters to try
                    var dilationFilters = new List<(IInPlaceFilter filter, string name)>
            {
                (new Dilation(), "Dilation"),
                (new BinaryDilation3x3(), "BinaryDilation3x3"),
                (new Dilation3x3(), "Dilation3x3")
            };

                    // Create erosion and closing filters
                    Erosion erosionFilter = new Erosion();
                    Closing closingFilter = new Closing();
                    SobelEdgeDetector edgeDetector = new SobelEdgeDetector();

                    foreach (var (filter, name) in dilationFilters)
                    {
                        using (Bitmap cloneBinaryImage = (Bitmap)binaryImage.Clone())
                        {
                            closingFilter.ApplyInPlace(cloneBinaryImage);
                            filter.ApplyInPlace(cloneBinaryImage);
                            erosionFilter.ApplyInPlace(cloneBinaryImage);
                            using (Bitmap edgeImage = edgeDetector.Apply(cloneBinaryImage))
                            {

                                BlobCounter blobCounter = new BlobCounter
                                {
                                    FilterBlobs = true,
                                    MinHeight = DocumentTypeMetadata.GetMinHeight(cloneBinaryImage.Height),
                                    MinWidth = DocumentTypeMetadata.GetMinWidth(cloneBinaryImage.Width),
                                    MaxHeight = DocumentTypeMetadata.GetMaxHeight(cloneBinaryImage.Height)
                                };


                                blobCounter.ProcessImage(edgeImage);
                                Blob[] blobs = blobCounter.GetObjectsInformation();

                                // Save the binary image with the current dilation filter applied
                                string binaryImagePath = failedImagePath.Replace(".tif", $"_{name}.tif");
                                cloneBinaryImage.Save(binaryImagePath);
                                tempImagePaths.Add(binaryImagePath);

                                //Adding in for Edge Detection Images
                                string edgeImagePath = failedImagePath.Replace(".tif", $"_{name}_Edge.tif");
                                edgeImage.Save(edgeImagePath);
                                tempImagePaths.Add(edgeImagePath);

                                if (blobs.Length > maxBlobsCount)
                                {
                                    maxBlobsCount = blobs.Length;
                                    maxBlobs = blobs;
                                    dilationTypeUsed = name;
                                    maxBlobImage?.Dispose();
                                    maxBlobImage = (Bitmap)nonIndexedImage.Clone();
                                }

                                if (blobs.Length == DocumentTypeMetadata.ItemsPerPage)
                                {
                                    foreach (var blob in blobs)
                                    {
                                        int paddingLeft = 10;
                                        int paddingRight = 10;
                                        int paddingTop = 10;
                                        int paddingBottom = 30;

                                        Rectangle blobRect = blob.Rectangle;

                                        Rectangle paddedRect = new Rectangle(
        Math.Max(0, blobRect.Left - paddingLeft),
        Math.Max(0, blobRect.Top - paddingTop),
        Math.Min(nonIndexedImage.Width - blobRect.Left, blobRect.Width + paddingLeft + paddingRight),
        Math.Min(nonIndexedImage.Height - blobRect.Top, blobRect.Height + paddingTop + paddingBottom)
    );

                                        // Ensure the rectangle does not exceed image bounds
                                        if (paddedRect.Right > nonIndexedImage.Width)
                                        {
                                            paddedRect.Width = nonIndexedImage.Width - paddedRect.Left;
                                        }
                                        if (paddedRect.Bottom > nonIndexedImage.Height)
                                        {
                                            paddedRect.Height = nonIndexedImage.Height - paddedRect.Top;
                                        }

                                        Bitmap blobImage = nonIndexedImage.Clone(paddedRect, nonIndexedImage.PixelFormat);
                                        string tempFilePath = SaveImageToTempFile(blobImage);
                                        extractedImagePaths.Add(tempFilePath);
                                    }

                                    // If the correct number of blobs is found, delete all temporary images.
                                    foreach (string tempImagePath in tempImagePaths)
                                    {
                                        File.Delete(tempImagePath);
                                    }
                                    break; // Successfully found the correct number of blobs
                                }
                            }
                        }
                    }

                    if (extractedImagePaths.Count != DocumentTypeMetadata.ItemsPerPage)
                    {
                        if (maxBlobImage != null)
                        {
                            // Save the image with the maximum number of blobs found
                            string maxBlobsImagePath = failedImagePath.Replace(".tif", $"_{dilationTypeUsed}_MaxBlobs.tif");
                            SaveImageWithBlobs(maxBlobImage, maxBlobs, maxBlobsImagePath);
                            maxBlobImage.Dispose();
                        }
                        throw new InvalidOperationException($"Expected {DocumentTypeMetadata.ItemsPerPage} blobs on Doc Type {DocumentTypeMetadata.Name}, but found {maxBlobsCount} blobs. Image saved at: {failedImagePath}");
                    }
                }
            }

            return extractedImagePaths;
        }

        // Method to save the image with highlighted blobs
        private void SaveImageWithBlobs(Bitmap image, Blob[] blobs, string failedImagePath)
        {
            string directoryPath = Path.GetDirectoryName(failedImagePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Save the unmarked image first
            string unmarkedImagePath = failedImagePath.Replace(".tif", "_Unmarked.tif");
            image.Save(unmarkedImagePath);

            // Now draw the rectangles and save the marked image
            using (Graphics g = Graphics.FromImage(image))
            {
                Pen highlightPen = new Pen(Color.Purple, 10);
                foreach (var blob in blobs)
                {
                    g.DrawRectangle(highlightPen, blob.Rectangle);
                }
            }
            image.Save(failedImagePath);
        }



        /// <summary>
        /// This is used to save the Extracted Image out to a temp file and return that file path as a string.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public string SaveImageToTempFile(Bitmap image)
        {
            string tempFilePath = Path.GetTempFileName();
            tempFilePath = Path.ChangeExtension(tempFilePath, "tif");
            image.Save(tempFilePath, ImageFormat.Tiff);
            return tempFilePath;
        }


    }


    public class DD1898FormExtractor : BaseFormExtractor
    {
        public DD1898FormExtractor(DocumentTypeConfiguration documentTypeConfiguration, string DocType)
            : base(documentTypeConfiguration, DocType) { }

    }

    public class DD250FormExtractor : BaseFormExtractor
    {
        public DD250FormExtractor(DocumentTypeConfiguration documentTypeConfiguration, string DocType)
            : base(documentTypeConfiguration, DocType) { }


        ///THis is the sample should you need to override the Default Croping/Document Extraction Class.
        //public override List<string> ExtractForms(string imagePath)
        //{
        //    // Extraction logic for DD250
        //    // Use DocumentTypeMetadata.ItemsPerPage as needed
        //    // ...

        //    return new List<string>();
        //}
    }

    // ... other specific form extractors

    public class FormExtractorFactory
    {
        private readonly DocumentTypeConfiguration _documentTypeConfiguration;

        public FormExtractorFactory(DocumentTypeConfiguration documentTypeConfiguration)
        {
            _documentTypeConfiguration = documentTypeConfiguration;
        }

        public IFormExtractor GetFormExtractor(string documentType)
        {
            switch (documentType)
            {
                case "Energy DD1898 Triple": return new DD1898FormExtractor(_documentTypeConfiguration, documentType);
                case "Energy DD1898 Double": return new DD1898FormExtractor(_documentTypeConfiguration, documentType);
                case "Energy DD1898 Single": return new DD1898FormExtractor(_documentTypeConfiguration, documentType);
                case "Energy DD250": return new DD250FormExtractor(_documentTypeConfiguration, documentType);
                // case "DD1348": return new DD1348FormExtractor(_documentTypeConfiguration);
                default: throw new NotImplementedException($"Form extractor not implemented for document type: {documentType}");
            }
        }
    }

}
