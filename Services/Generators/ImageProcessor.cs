using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;
using SixLabors.ImageSharp.Formats.Jpeg;
using vkbot_vitalya.Services.Generators.TextGeneration;

namespace vkbot_vitalya.Services.Generators;

public static class ImageProcessor {
    private static readonly Random Rand = new Random();

    public static Image<Rgba32> BreakImage(Image<Rgba32> image) {
        var width = image.Width;
        var height = image.Height;
        var brokenImage = new Image<Rgba32>(width, height);

        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var offsetX = Rand.Next(-10, 10); // Slight horizontal offset for glitch effect
                var offsetY = Rand.Next(-10, 10); // Slight vertical offset for glitch effect
                var newX = Math.Clamp(x + offsetX, 0, width - 1);
                var newY = Math.Clamp(y + offsetY, 0, height - 1);
                brokenImage[x, y] = image[newX, newY];
            }
        }

        var randomText = MessageProcessor.KeepUpConversation().Result;
        brokenImage.AddText(randomText);

        return brokenImage;
    }

    public static Image<Rgba32> LiquidateImage(Image<Rgba32> image) {
        var grayImage = image.Clone(ctx => ctx.Grayscale());

        AddWatermarkText(grayImage, "ЛИКВИДИРОВАН");

        return grayImage;
    }

    public static Image<Rgba32> CompressImage(Image<Rgba32> image) {
        var newWidth = image.Width / 10;
        var newHeight = image.Height / 10;

        var compressedImage = image.Clone(ctx => ctx.Resize(newWidth, newHeight));
        var finalImage = compressedImage.Clone(ctx => ctx.Resize(image.Width, image.Height));

        var randomText = MessageProcessor.KeepUpConversation().Result;
        var finalImageWithBorder = finalImage.AddTextWithBorder(randomText);

        return finalImageWithBorder;
    }

    public static Image<Rgba32> AddTextImageCommand(Image<Rgba32> image) {
        var randomText = MessageProcessor.KeepUpConversation().Result;
        image.AddTopText(randomText);

        return image;
    }

    private static void AddTopText(this Image<Rgba32> image, string text) {
        var font = SystemFonts.CreateFont("Arial", 36, FontStyle.Bold);

        var options = new TextOptions(font) {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var textBounds = TextMeasurer.MeasureBounds(text, options);
        var textPosition = new PointF(image.Width / 2 - textBounds.Width / 2,
            image.Height / 2 - textBounds.Height / 2 + 100);

        image.Mutate(ctx => {
            ctx.DrawText(new DrawingOptions { GraphicsOptions = new GraphicsOptions { Antialias = true } },
                text, font, Pens.Solid(Color.White, 6),
                textPosition);

            ctx.DrawText(new DrawingOptions { GraphicsOptions = new GraphicsOptions { Antialias = true } },
                text, font, Brushes.Solid(Color.Black),
                textPosition);
        });
    }

    private static void AddText(this Image<Rgba32> image, string text) {
        var font = SystemFonts.CreateFont("Arial", 16, FontStyle.Bold);
        var point = new PointF(10, 10);

        image.Mutate(ctx => ctx.DrawText(text, font, Color.White, point));
    }

    private static void AddWatermarkText(this Image<Rgba32> image, string text) {
        var font = SystemFonts.CreateFont("Arial", 40, FontStyle.Bold);

        var options = new TextOptions(font) {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var textBounds = TextMeasurer.MeasureBounds(text, options);
        var scale = Math.Min(image.Width / textBounds.Width, image.Height / textBounds.Height);

        var transform = Matrix3x2.CreateTranslation(-textBounds.Width / 2, -textBounds.Height / 2) *
                        Matrix3x2.CreateScale(scale) *
                        Matrix3x2.CreateRotation((float)Math.PI / 4) *
                        Matrix3x2.CreateTranslation(image.Width / 2, image.Height / 2);

        var drawingOptions = new DrawingOptions {
            GraphicsOptions = new GraphicsOptions { Antialias = true },
            Transform = transform
        };

        image.Mutate(context => { context.DrawText(drawingOptions, text, font, Color.Red, PointF.Empty); });
    }

    private static Image<Rgba32> AddTextWithBorder(this Image<Rgba32> image, string text) {
        const int borderThickness = 20;
        var font = SystemFonts.CreateFont("Arial", 36, FontStyle.Bold);

        // Create a new image with black border
        var newImage = new Image<Rgba32>(image.Width + borderThickness * 2, image.Height + borderThickness * 2);
        newImage.Mutate(ctx => {
            ctx.Fill(Color.Black);

            // Draw white inner border
            ctx.DrawLine(Color.White, borderThickness / 2, new PointF[] {
                new(borderThickness / 2, borderThickness / 2),
                new(newImage.Width - borderThickness / 2, borderThickness / 2),
                new(newImage.Width - borderThickness / 2, newImage.Height - borderThickness / 2),
                new(borderThickness / 2, newImage.Height - borderThickness / 2),
                new(borderThickness / 2, borderThickness / 2)
            });

            // Draw original image in the center of the new image
            ctx.DrawImage(image, new Point(borderThickness, borderThickness), 1);
        });

        // Draw black text with white outline
        var options = new TextOptions(font) {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var textBounds = TextMeasurer.MeasureBounds(text, options);
        var textPosition = new PointF(newImage.Width / 2 - textBounds.Width / 2,
            newImage.Height - borderThickness / 2 - textBounds.Height);

        newImage.Mutate(ctx => {
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

    public static async Task<Image<Rgba32>> Funeral(this Image<Rgba32> image) {
        const int maxWidth = 306;
        const int maxHeight = 306;

        var minDate = DateTime.Parse("01.01.1930");
        var maxDate = DateTime.Parse("1.1.2006");


        var grave = Image.Load<Rgba32>("./grave.png");

        // todo определить нужные размеры и растянуть за один раз лучшим методом

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
        grave.Mutate(i =>
            i.DrawImage(image, new Point(475 + (maxWidth - image.Width) / 2, 200 + (maxHeight - image.Height) / 2), 1));
        grave.Mutate(i =>
            i.DrawText(
                $"{minDate.AddTicks((long)(Rand.NextDouble() * (maxDate - minDate).Ticks)):d} - {DateTime.Today:d}",
                font, Color.Black, new(465, 500)));

        var text = await MessageProcessor.KeepUpConversation();
        grave.Mutate(i =>
            i.DrawText(text, SystemFonts.CreateFont("Georgia", 40, FontStyle.Italic), Color.Black, new(480, 560)));

        return grave;
    }
}