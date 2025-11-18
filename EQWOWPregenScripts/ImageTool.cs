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
using SixLabors.ImageSharp.Drawing.Processing;

namespace EQWOWPregenScripts
{
    internal class ImageTool
    {
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

        public static void CombineMinimapImagesWithBorderAndCrop(List<MinimapMetadata> minimaps, string outputFilePath, Rgba32 borderColorValue, 
            out int startPixelX, out int startPixelY, out int endPixelX, out int endPixelY, out int sizeOfTileInPixelsAcross)
        {
            Color borderColor = new Color(borderColorValue);

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
            sizeOfTileInPixelsAcross = tileWidth; // Tiles are all square, so doesn't matter which to use

            // Create output image, including a pixel border
            int combinedWidthPreCrop = (columns * tileWidth) + 2;
            int combinedHeightPreCrop = (rows * tileHeight) + 2;

            using var combinedImage = new Image<Rgba32>(combinedWidthPreCrop, combinedHeightPreCrop);

            // Process each minimap by copying it into the larger image
            foreach (var minimap in minimaps)
            {
                using var tileImage = Image.Load<Rgba32>(minimap.FullFilePath);

                // Verify tile image dimensions match
                if (tileImage.Width != tileWidth || tileImage.Height != tileHeight)
                    Console.WriteLine("Incorrect dimensions for image " + minimap.FullFilePath);

                // Calculate destination position (normalize by subtracting minXTile/minYTile)
                int destX = ((minimap.XTile - minXTile) * tileWidth) + 1;
                int destY = ((minimap.YTile - minYTile) * tileHeight) + 1;

                // Copy tile to output image
                combinedImage.Mutate(ctx => ctx.DrawImage(tileImage, new Point(destX, destY), 1f));
            }

            // Go around the image and add a border
            combinedImage.ProcessPixelRows(accessor =>
            {
                // Create a copy of the pixel data to avoid modifying while reading
                Rgba32[,] pixelCopy = new Rgba32[combinedImage.Height, combinedImage.Width];
                for (int y = 0; y < combinedImage.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < combinedImage.Width; x++)
                    {
                        pixelCopy[y, x] = row[x];
                    }
                }

                // Process each pixel
                for (int y = 0; y < combinedImage.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < combinedImage.Width; x++)
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
                            if (y < combinedImage.Height - 1)
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
                            if (x < combinedImage.Width - 1)
                            {
                                Rgba32 right = pixelCopy[y, x + 1];
                                if (right.R > 0 || right.G > 0 || right.B > 0)
                                {
                                    hasNonBlackNeighbor = true;
                                }
                            }

                            // Set the border color
                            if (hasNonBlackNeighbor == true)
                                row[x] = borderColor;

                        }
                    }
                }
            });

            // Calculate bounding rectangle of non-pure-black pixels
            startPixelX = combinedImage.Width;
            endPixelX = -1;
            startPixelY = combinedImage.Height;
            endPixelY = -1;

            for (int y = 0; y < combinedImage.Height; y++)
            {
                for (int x = 0; x < combinedImage.Width; x++)
                {
                    Rgba32 pixel = combinedImage[x, y];
                    if (pixel.R != 0 || pixel.G != 0 || pixel.B != 0)
                    {
                        if (x < startPixelX) startPixelX = x;
                        if (x > endPixelX) endPixelX = x;
                        if (y < startPixelY) startPixelY = y;
                        if (y > endPixelY) endPixelY = y;
                    }
                }
            }

            // Crop the image
            int croppedWidth = (endPixelX - startPixelX) + 1;
            int croppedHeight = (endPixelY - startPixelY) + 1;
            Rectangle croppedRectangle = new Rectangle(startPixelX, startPixelY, croppedWidth, croppedHeight);
            using var croppedImage = combinedImage.Clone(img => img.Crop(croppedRectangle));

            // Save output image
            croppedImage.SaveAsPng(outputFilePath);
        }

        public static void GenerateFullMap(string sourceImageFullPath, string targetImageFullPath, int scaledContentWidth, int scaledContentHeight,
            Rgba32 backgroundColor, int contentTargetWidth, int contentTargetHeight, float pixelScale, int offsetX, int offsetY)
        {
            // Load the cropped image
            using var croppedSourceImage = Image.Load<Rgba32>(sourceImageFullPath);

            // Replace all pure black with the background color
            croppedSourceImage.ProcessPixelRows(accessor =>
            {
                // Create a copy of the pixel data to avoid modifying while reading
                Rgba32[,] pixelCopy = new Rgba32[croppedSourceImage.Height, croppedSourceImage.Width];
                for (int y = 0; y < croppedSourceImage.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < croppedSourceImage.Width; x++)
                        pixelCopy[y, x] = row[x];
                }

                // Process each pixel
                for (int y = 0; y < croppedSourceImage.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < croppedSourceImage.Width; x++)
                    {
                        // Check if the pixel is pure black and set to background if so
                        if (pixelCopy[y, x].R == 0 && pixelCopy[y, x].G == 0 && pixelCopy[y, x].B == 0)
                            row[x] = backgroundColor;
                    }
                }
            });

            // Resize the image
            var resizeOptions = new ResizeOptions
            {
                Size = new Size(scaledContentWidth, scaledContentHeight),
                Mode = ResizeMode.Stretch,
                Sampler = KnownResamplers.Lanczos3
            };
            using var resized = croppedSourceImage.Clone(ctx => ctx.Resize(resizeOptions));

            // In creating the final image, add on the clear padding on the right and bottom
            int finalOutputWidth = 1024; // 22 transparent pixels pad the right
            int finalOutputHeight = 768; // 100 transparent pixels pad the bottom
            using var finalImage = new Image<Rgba32>(finalOutputWidth, finalOutputHeight, Color.Transparent);
            finalImage.Mutate(ctx => ctx.Fill(backgroundColor, new Rectangle(0, 0, 1002, 668)));
            finalImage.Mutate(ctx => ctx.DrawImage(resized, new Point(offsetX, offsetY), 1f));
            finalImage.SaveAsPng(targetImageFullPath);
        }
    }
}
