using System.Drawing.Imaging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using vkbot_vitalya.Core;
using vkbot_vitalya.Services.Generators.TextGeneration;
using Font = SixLabors.Fonts.Font;

namespace vkbot_vitalya.Services.Generators;

public class ImageProcessor
{
    private static Random random = new Random();

    public Image<Rgba32> BreakImage(Image<Rgba32> image)
    {
        var width = image.Width;
        var height = image.Height;
        var brokenImage = new Image<Rgba32>(width, height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offsetX = random.Next(-10, 10);    // Slight horizontal offset for glitch effect
                var offsetY = random.Next(-10, 10);    // Slight vertical offset for glitch effect
                var newX = Math.Clamp(x + offsetX, 0, width - 1);
                var newY = Math.Clamp(y + offsetY, 0, height - 1);
                brokenImage[x, y] = image[newX, newY];
            }
        }

        var randomText = MessageProcessor.KeepUpConversation().Result;
        AddTextToImage(brokenImage, randomText);

        return brokenImage;
    }

    public Image<Rgba32> LiquidateImage(Image<Rgba32> image)
    {
        var grayImage = image.Clone(ctx => ctx.Grayscale());

        AddWatermarkText(grayImage, "ЛИКВИДИРОВАН");

        return grayImage;
    }

    public Image<Rgba32> CompressImage(Image<Rgba32> image)
    {
        var newWidth = image.Width / 10;
        var newHeight = image.Height / 10;

        var compressedImage = image.Clone(ctx => ctx.Resize(newWidth, newHeight));
        var finalImage = compressedImage.Clone(ctx => ctx.Resize(image.Width, image.Height));

        var randomText = MessageProcessor.KeepUpConversation().Result;
        var finalImageWithBorder = AddTextToImageWithBorder(finalImage, randomText);

        return finalImageWithBorder;
    }

    public Image<Rgba32> AddTextImageCommand(Image<Rgba32> image)
    {
        var randomText = MessageProcessor.KeepUpConversation().Result;

        AddTopTextToImage(image, randomText);

        return image;
    }

    private void AddTopTextToImage(Image<Rgba32> image, string text)
    {
        var font = SystemFonts.CreateFont("Arial", 36, FontStyle.Bold);

        var options = new TextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center

        };

        var textBounds = TextMeasurer.MeasureBounds(text, options);
        var textPosition = new PointF(image.Width / 2 - textBounds.Width / 2, image.Height / 2 - textBounds.Height / 2 + 100);

        image.Mutate(ctx =>
        {
            ctx.DrawText(new DrawingOptions { GraphicsOptions = new GraphicsOptions { Antialias = true } },
                         text, font, Pens.Solid(Color.White, 6),
                         textPosition);

            ctx.DrawText(new DrawingOptions { GraphicsOptions = new GraphicsOptions { Antialias = true } },
                         text, font, Brushes.Solid(Color.Black),
                         textPosition);
        });
    }

    private void AddTextToImage(Image<Rgba32> image, string text)
    {
        var font = SystemFonts.CreateFont("Arial", 16, FontStyle.Bold);
        var point = new PointF(10, 10);

        image.Mutate(ctx => ctx.DrawText(text, font, Color.White, point));
    }

    private void AddWatermarkText(Image<Rgba32> image, string text)
    {
        var font = SystemFonts.CreateFont("Arial", 40, FontStyle.Bold);

        var options = new TextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var textBounds = TextMeasurer.MeasureBounds(text, options);
        var scale = Math.Min(image.Width / textBounds.Width, image.Height / textBounds.Height);

        var transform = Matrix3x2.CreateTranslation(-textBounds.Width / 2, -textBounds.Height / 2) *
                        Matrix3x2.CreateScale(scale) *
                        Matrix3x2.CreateRotation((float)Math.PI / 4) *
                        Matrix3x2.CreateTranslation(image.Width / 2, image.Height / 2);

        var drawingOptions = new DrawingOptions
        {
            GraphicsOptions = new GraphicsOptions { Antialias = true },
            Transform = transform
        };

        image.Mutate(context =>
        {
            context.DrawText(drawingOptions, text, font, Color.Red, PointF.Empty);
        });
    }

    private Image<Rgba32> AddTextToImageWithBorder(Image<Rgba32> image, string text)
    {
        var borderThickness = 20;
        var font = SystemFonts.CreateFont("Arial", 36, FontStyle.Bold);

        // Create a new image with black border
        var newImage = new Image<Rgba32>(image.Width + borderThickness * 2, image.Height + borderThickness * 2);
        newImage.Mutate(ctx =>
        {
            ctx.Fill(Color.Black);

            // Draw white inner border
            ctx.DrawLine(Color.White, borderThickness / 2, new PointF[] {
                    new PointF(borderThickness / 2, borderThickness / 2),
                    new PointF(newImage.Width - borderThickness / 2, borderThickness / 2),
                    new PointF(newImage.Width - borderThickness / 2, newImage.Height - borderThickness / 2),
                    new PointF(borderThickness / 2, newImage.Height - borderThickness / 2),
                    new PointF(borderThickness / 2, borderThickness / 2)
                });

            // Draw original image in the center of the new image
            ctx.DrawImage(image, new Point(borderThickness, borderThickness), 1);
        });

        // Draw black text with white outline
        var options = new TextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center

        };

        var textBounds = TextMeasurer.MeasureBounds(text, options);
        var textPosition = new PointF(newImage.Width / 2 - textBounds.Width / 2, newImage.Height - borderThickness / 2 - textBounds.Height);

        newImage.Mutate(ctx =>
        {
            // Draw white outline
            ctx.DrawText(new DrawingOptions { GraphicsOptions = new GraphicsOptions { Antialias = true } },
                         text, font, Pens.Solid(Color.White, 6),
                         textPosition);

            // Draw black text
            ctx.DrawText(new DrawingOptions { GraphicsOptions = new GraphicsOptions { Antialias = true } },
                         text, font, Brushes.Solid(Color.Black),
                         textPosition);
        });

        return newImage;
    }

    public static async Task ResizeAndCompressImage(Stream inputStream, Stream outputStream) {
        using var image = await Image.LoadAsync(inputStream);
        var originalWidth = image.Width;
        var originalHeight = image.Height;
        
        var newWidth = originalWidth;
        var newHeight = originalHeight;
        
        if (originalWidth > 2560 || originalHeight > 2048) {
            var scaleWidth = 2560.0 / originalWidth;
            var scaleHeight = 2048.0 / originalHeight;
            var scale = Math.Min(scaleWidth, scaleHeight);
        
            newWidth = (int)(originalWidth * scale);
            newHeight = (int)(originalHeight * scale);
        }
        
        image.Mutate(ctx => ctx.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
        var encoder = new JpegEncoder();

        await image.SaveAsync(outputStream, encoder);
    }

    public static async Task<Image<Rgba32>> Funeral(Image<Rgba32> image) {
        const int maxWidth = 306;
        const int maxHeight = 306;

        var grave = Image.Load<Rgba32>("./grave.png");

        var aspectRatio = (float)image.Width / image.Height;
        if (aspectRatio > 2) {
            image.Mutate(i => i.Resize(image.Width, image.Width / 2));
        } else if (aspectRatio < 0.5) {
            image.Mutate(i => i.Resize(image.Height / 2, image.Height));
        }

        // Если фото слишком маленькое, растягиваем по ширине игнорируя высоту, она проверится дальше
        if (image.Height < maxHeight && image.Width < maxWidth) {
            image.Mutate(i => i.Resize(maxWidth, (int)(image.Height / ((float)image.Width / maxWidth))));
        }

        if (image.Width > maxWidth) {
            image.Mutate(i => i.Resize(maxWidth, (int)(image.Height / ((float)image.Width / maxWidth))));
        }

        if (image.Height > maxHeight) {
            image.Mutate(i => i.Resize((int)(image.Width / ((float)image.Height / maxHeight)), maxHeight));
        }

        image.Mutate(i => i.Saturate(0.25f));

        var font = SystemFonts.CreateFont("Georgia", 30, FontStyle.Italic);
        /* I FUCKING LOVE FUNCTIONAL STYLE */
        grave.Mutate(i =>
            i.DrawImage(image, new Point(475 + (maxWidth - image.Width) / 2, 195 + (maxHeight - image.Height) / 2), 1));
        grave.Mutate(i => i.DrawText(DateTime.Parse("01.01.1930")
            .AddTicks((long)(random.NextDouble()
                             * (DateTime.Parse("01.01.2006") - DateTime.Parse("01.01.1930")).Ticks))
            .ToString("d") + " - " + DateTime.Today.ToString("d"), font, Color.Black, new PointF(465, 500)));

        var message = await MessageProcessor.KeepUpConversation();


        grave.Mutate(i =>
            i.DrawText(message, SystemFonts.CreateFont("Georgia", 40, FontStyle.Italic), Color.Black,
                new PointF(480, 560)));

        return grave;
    }
}