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
        public static void CombineMinimapImages(List<MinimapMetadata> minimaps, string outputFilePath)
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
            int outputWidth = columns * tileWidth;
            int outputHeight = rows * tileHeight;

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
    }
}
