//  Author: Nathan Handley(nathanhandley @protonmail.com)
//  Copyright (c) 2025 Nathan Handley
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace EQWOWPregenScripts
{
    internal class ImageTool
    {
        public static void CombineMinimapImages(List<MinimapMetadata> minimaps, string outputFilePath, out int outputWidth, out int outputHeight,
            out int startPixelX, out int startPixelY, out int endPixelX, out int endPixelY)
        {
            // Find minimum and maximum tile indices
            int minXTile = minimaps.Min(m => m.XTile);
            int maxXTile = minimaps.Max(m => m.XTile);
            int minYTile = minimaps.Min(m => m.YTile);
            int maxYTile = minimaps.Max(m => m.YTile);

            // Calculate grid dimensions (normalized to start at 0,0)
            int columns = maxXTile - minXTile + 1;
            int rows = maxYTile - minYTile + 1;

            // Load first image to get dimensions (assuming all images are same size)
            using var firstImage = Image.Load<Rgba32>(minimaps[0].FullFilePath);
            int tileWidth = firstImage.Width;
            int tileHeight = firstImage.Height;

            // Create output image
            outputWidth = columns * tileWidth;
            outputHeight = rows * tileHeight;

            using var outputImage = new Image<Rgba32>(outputWidth, outputHeight);

            // Process each minimap
            foreach (var minimap in minimaps)
            {
                using var tileImage = Image.Load<Rgba32>(minimap.FullFilePath);

                // Verify tile image dimensions match
                if (tileImage.Width != tileWidth || tileImage.Height != tileHeight)
                    Console.WriteLine("Incorrect dimensions for image " + minimap.FullFilePath);

                // Calculate destination position (normalize by subtracting minXTile/minYTile)
                int destX = (minimap.XTile - minXTile) * tileWidth;
                int destY = (minimap.YTile - minYTile) * tileHeight;

                // Copy tile to output image
                outputImage.Mutate(ctx => ctx.DrawImage(tileImage, new Point(destX, destY), 1f));
            }

            // Save output image
            outputImage.SaveAsPng(outputFilePath);

            // Calculate bounding rectangle of non-pure-black pixels
            startPixelX = outputImage.Width;
            endPixelX = -1;
            startPixelY = outputImage.Height;
            endPixelY = -1;

            for (int y = 0; y < outputImage.Height; y++)
            {
                for (int x = 0; x < outputImage.Width; x++)
                {
                    Rgba32 pixel = outputImage[x, y];
                    if (pixel.R != 0 || pixel.G != 0 || pixel.B != 0)
                    {
                        if (x < startPixelX) startPixelX = x;
                        if (x > endPixelX) endPixelX = x;
                        if (y < startPixelY) startPixelY = y;
                        if (y > endPixelY) endPixelY = y;
                    }
                }
            }
        }

        public static void AdjustPixelBrightness(string sourceImagePath, string targetImagePath, float scaleAmount, int maxBrightness)
        {
            // Load source image
            using var sourceImage = Image.Load<Rgba32>(sourceImagePath);

            // Create target image with same dimensions
            using var targetImage = new Image<Rgba32>(sourceImage.Width, sourceImage.Height);

            // Process each pixel
            sourceImage.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        ref Rgba32 pixel = ref pixelRow[x];
                        float r = pixel.R;
                        float g = pixel.G;
                        float b = pixel.B;

                        // Skip black
                        if (r == 0 && g == 0 && b == 0)
                        {
                            targetImage[x, y] = pixel;
                            continue;
                        }

                        // Find the minimum component and maximum component for scaling
                        float minColor = r;
                        if (minColor == 0 || g < minColor)
                            minColor = g;
                        if (minColor == 0 || b < minColor)
                            minColor = b;
                        float maxColor = Math.Max(Math.Max(r, g), b);

                        // Only process if within bounds
                        if (maxColor < maxBrightness)
                        {
                            // Calculate scaling factor to bring minComponent to minBrightness
                            float scale = scaleAmount;

                            // Calculate some possible max values
                            float possibleR = r * scale;
                            float possibleG = g * scale;
                            float possibleB = b * scale;

                            // Reduce scale back if any went above the max
                            float maxScaledBrightness = Math.Max(Math.Max(possibleR, possibleG), possibleB);
                            if (maxScaledBrightness > maxBrightness)
                                scale *= maxBrightness / maxScaledBrightness;

                            // Apply scaling
                            r *= scale;
                            g *= scale;
                            b *= scale;
                        }

                        // Assign new pixel values to target image
                        targetImage[x, y] = new Rgba32((byte)Math.Round(r), (byte)Math.Round(g), (byte)Math.Round(b), pixel.A);
                    }
                }
            });

            // Save target image
            targetImage.SaveAsPng(targetImagePath);
        }

        public static void GenerateFullMap(string inputImageFilePath, string outputImageFilePath, int minPixelX, int minPixelY, int maxPixelX,
            int maxPixelY, int topBorderPixelSize, int bottomBorderPixelSize, int leftBorderPixelSize, int rightBorderPixelSize, int fullOutputPixelWidth, int fullOutputPixelHeight,
            Color backgroundColor, Color borderColor, int transparentRightWidth, int transparentBottomHeight,
            out float contentWidthScaledProportion, out float contentHeightScaledProportion)
        {
            // Load the input image
            using (Image<Rgba32> inputImage = Image.Load<Rgba32>(inputImageFilePath))
            {
                // Create a new in-memory image with 2 pixels larger dimensions
                int paddedWidth = inputImage.Width + 2;
                int paddedHeight = inputImage.Height + 2;
                using (Image<Rgba32> paddedImage = new Image<Rgba32>(paddedWidth, paddedHeight))
                {
                    // Set the background color
                    paddedImage.Mutate(ctx => ctx.BackgroundColor(backgroundColor));

                    // Copy the inputImage into the center of the paddedImage
                    paddedImage.Mutate(ctx => ctx.DrawImage(inputImage, new Point(1, 1), 1f));

                    // Calculate crop rectangle
                    int cropWidth = maxPixelX - minPixelX;
                    int cropHeight = maxPixelY - minPixelY;

                    // Crop the image
                    using (Image<Rgba32> croppedImage = paddedImage.Clone(ctx => ctx.Crop(new Rectangle(minPixelX, minPixelY, cropWidth, cropHeight))))
                    {
                        // Find the bounding box of non-transparent pixels
                        int minX = cropWidth, minY = cropHeight, maxX = -1, maxY = -1;
                        croppedImage.ProcessPixelRows(accessor =>
                        {
                            for (int y = 0; y < croppedImage.Height; y++)
                            {
                                Span<Rgba32> row = accessor.GetRowSpan(y);
                                for (int x = 0; x < croppedImage.Width; x++)
                                {
                                    if (row[x].A > 0)
                                    {
                                        if (x < minX) minX = x;
                                        if (x > maxX) maxX = x;
                                        if (y < minY) minY = y;
                                        if (y > maxY) maxY = y;
                                    }
                                }
                            }
                        });

                        // Create a new image with space for a 1-pixel black border
                        int borderedWidth = cropWidth + 2;
                        int borderedHeight = cropHeight + 2;
                        using (Image<Rgba32> borderedImage = new Image<Rgba32>(borderedWidth, borderedHeight))
                        {
                            // Set transparent background
                            borderedImage.Mutate(ctx => ctx.BackgroundColor(Color.Transparent));

                            // This processes all rows but only sets pixels in the border regions for safety and simplicity
                            borderedImage.ProcessPixelRows(accessor =>
                            {
                                int borderMinX = minX + 1;
                                int borderMinY = minY + 1;
                                int borderMaxX = maxX + 1;
                                int borderMaxY = maxY + 1;

                                for (int y = 0; y < borderedImage.Height; y++)
                                {
                                    Span<Rgba32> row = accessor.GetRowSpan(y);

                                    // Top border: 1 pixel above borderMinY, spanning x-range
                                    if (y == borderMinY - 1)
                                    {
                                        for (int x = borderMinX - 1; x <= borderMaxX + 1; x++)
                                        {
                                            row[x] = Color.Black;
                                        }
                                    }

                                    // Bottom border: 1 pixel below borderMaxY, spanning x-range
                                    if (y == borderMaxY + 1)
                                    {
                                        for (int x = borderMinX - 1; x <= borderMaxX + 1; x++)
                                        {
                                            row[x] = Color.Black;
                                        }
                                    }

                                    // Left border: 1 pixel left of borderMinX, for this y if in range
                                    if (y >= borderMinY - 1 && y <= borderMaxY + 1 && borderMinX - 1 >= 0 && borderMinX - 1 < borderedWidth)
                                    {
                                        row[borderMinX - 1] = Color.Black;
                                    }

                                    // Right border: 1 pixel right of borderMaxX, for this y if in range
                                    if (y >= borderMinY - 1 && y <= borderMaxY + 1 && borderMaxX + 1 >= 0 && borderMaxX + 1 < borderedWidth)
                                    {
                                        row[borderMaxX + 1] = Color.Black;
                                    }
                                }
                            });

                            // Copy the cropped image into the bordered image
                            borderedImage.Mutate(ctx => ctx.DrawImage(croppedImage, new Point(1, 1), 1f));

                            // Replace pure black pixels with transparency if an adjacent pixel is not black
                            borderedImage.ProcessPixelRows(accessor =>
                            {
                                // Create a copy of the pixel data to avoid modifying while reading
                                Rgba32[,] pixelCopy = new Rgba32[borderedImage.Height, borderedImage.Width];
                                for (int y = 0; y < borderedImage.Height; y++)
                                {
                                    Span<Rgba32> row = accessor.GetRowSpan(y);
                                    for (int x = 0; x < borderedImage.Width; x++)
                                    {
                                        pixelCopy[y, x] = row[x];
                                    }
                                }

                                // Process each pixel
                                for (int y = 0; y < borderedImage.Height; y++)
                                {
                                    Span<Rgba32> row = accessor.GetRowSpan(y);
                                    for (int x = 0; x < borderedImage.Width; x++)
                                    {
                                        // Check if the pixel is pure black
                                        if (pixelCopy[y, x].R == 0 && pixelCopy[y, x].G == 0 && pixelCopy[y, x].B == 0)
                                        {
                                            bool hasNonBlackNeighbor = false;

                                            // Check top neighbor
                                            if (y > 0)
                                            {
                                                Rgba32 top = pixelCopy[y - 1, x];
                                                if (top.R > 0 || top.G > 0 || top.B > 0)
                                                {
                                                    hasNonBlackNeighbor = true;
                                                }
                                            }

                                            // Check bottom neighbor
                                            if (y < borderedImage.Height - 1)
                                            {
                                                Rgba32 bottom = pixelCopy[y + 1, x];
                                                if (bottom.R > 0 || bottom.G > 0 || bottom.B > 0)
                                                {
                                                    hasNonBlackNeighbor = true;
                                                }
                                            }

                                            // Check left neighbor
                                            if (x > 0)
                                            {
                                                Rgba32 left = pixelCopy[y, x - 1];
                                                if (left.R > 0 || left.G > 0 || left.B > 0)
                                                {
                                                    hasNonBlackNeighbor = true;
                                                }
                                            }

                                            // Check right neighbor
                                            if (x < borderedImage.Width - 1)
                                            {
                                                Rgba32 right = pixelCopy[y, x + 1];
                                                if (right.R > 0 || right.G > 0 || right.B > 0)
                                                {
                                                    hasNonBlackNeighbor = true;
                                                }
                                            }

                                            // Either set it as background or border color
                                            if (hasNonBlackNeighbor == false)
                                                row[x] = backgroundColor;
                                            else
                                                row[x] = borderColor;

                                        }
                                    }
                                }
                            });

                            // Calculate scaling to fit within output dimensions, accounting for borders
                            int availableWidth = fullOutputPixelWidth - (leftBorderPixelSize + rightBorderPixelSize + transparentRightWidth);
                            int availableHeight = fullOutputPixelHeight - (topBorderPixelSize + bottomBorderPixelSize + transparentBottomHeight); 

                            // Maintain aspect ratio
                            float aspectRatio = (float)borderedWidth / borderedHeight;
                            int scaledWidth;
                            int scaledHeight;

                            if (aspectRatio > (float)availableWidth / availableHeight)
                            {
                                // Image is wider relative to available space
                                scaledWidth = availableWidth;
                                scaledHeight = (int)(availableWidth / aspectRatio);
                            }
                            else
                            {
                                // Image is taller relative to available space
                                scaledHeight = availableHeight;
                                scaledWidth = (int)(availableHeight * aspectRatio);
                            }

                            contentWidthScaledProportion = (float)scaledWidth / (float)availableWidth;
                            contentHeightScaledProportion = (float)scaledHeight / (float)availableHeight;

                            // Create final output image with transparent background
                            using (Image<Rgba32> outputImage = new Image<Rgba32>(fullOutputPixelWidth, fullOutputPixelHeight))
                            {
                                // Set transparent background
                                outputImage.Mutate(ctx => ctx.BackgroundColor(backgroundColor));

                                // Calculate position to center the scaled image within the borders
                                int xOffset = leftBorderPixelSize + (availableWidth - scaledWidth) / 2;
                                int yOffset = topBorderPixelSize + (availableHeight - scaledHeight) / 2;

                                // Resize the bordered image
                                using (Image<Rgba32> resizedImage = borderedImage.Clone(ctx => ctx.Resize(new ResizeOptions
                                {
                                    Size = new Size(scaledWidth, scaledHeight),
                                    Mode = ResizeMode.Stretch,
                                    Sampler = KnownResamplers.Lanczos3
                                })))
                                {
                                    // Draw the resized image onto the output
                                    outputImage.Mutate(ctx => ctx.DrawImage(resizedImage, new Point(xOffset, yOffset), 1f));
                                }

                                // Replace specified pixels on right and bottom edges with transparent
                                outputImage.ProcessPixelRows(accessor =>
                                {
                                    // Replace right edge pixels
                                    for (int y = 0; y < outputImage.Height; y++)
                                    {
                                        Span<Rgba32> row = accessor.GetRowSpan(y);
                                        for (int x = outputImage.Width - transparentRightWidth; x < outputImage.Width; x++)
                                        {
                                            row[x] = Color.Transparent;
                                        }
                                    }

                                    // Replace bottom edge pixels (re-process rows to avoid overlap issues)
                                    for (int y = outputImage.Height - transparentBottomHeight; y < outputImage.Height; y++)
                                    {
                                        Span<Rgba32> row = accessor.GetRowSpan(y);
                                        for (int x = 0; x < outputImage.Width - transparentRightWidth; x++)
                                        {
                                            row[x] = Color.Transparent;
                                        }
                                    }
                                });

                                // Save output image as PNG
                                outputImage.SaveAsPng(outputImageFilePath);
                            }
                        }
                    }
                }
            }
        }
    }
}
