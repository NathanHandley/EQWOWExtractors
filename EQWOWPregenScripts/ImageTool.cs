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
                {
                    Console.WriteLine("Incorrect dimensions for image " + minimap.FullFilePath)
                }

                // Calculate destination position (normalize by subtracting minXTile/minYTile)
                int destX = (minimap.XTile - minXTile) * tileWidth;
                int destY = (minimap.YTile - minYTile) * tileHeight;

                // Copy tile to output image
                outputImage.Mutate(ctx => ctx.DrawImage(tileImage, new Point(destX, destY), 1f));
            }

            // Save output image
            outputImage.SaveAsPng(outputFilePath);
        }
    }
}
