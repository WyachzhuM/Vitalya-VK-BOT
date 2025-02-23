using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;
using vkbot_vitalya.Services.Generators.TextGeneration;
using Font = SixLabors.Fonts.Font;

namespace vkbot_vitalya.Services.Generators;

public class ImageProcessor
{
    private static Random random = new Random();

    public Image<Rgba32> BreakImage(Image<Rgba32> image)
    {
        int width = image.Width;
        int height = image.Height;
        var brokenImage = new Image<Rgba32>(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offsetX = random.Next(-10, 10);    // Slight horizontal offset for glitch effect
                int offsetY = random.Next(-10, 10);    // Slight vertical offset for glitch effect
                int newX = Math.Clamp(x + offsetX, 0, width - 1);
                int newY = Math.Clamp(y + offsetY, 0, height - 1);
                brokenImage[x, y] = image[newX, newY];
            }
        }

        string randomText = MessageProcessor.KeepUpConversation().Result;
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
        int newWidth = image.Width / 10;
        int newHeight = image.Height / 10;

        var compressedImage = image.Clone(ctx => ctx.Resize(newWidth, newHeight));
        var finalImage = compressedImage.Clone(ctx => ctx.Resize(image.Width, image.Height));

        string randomText = MessageProcessor.KeepUpConversation().Result;
        var finalImageWithBorder = AddTextToImageWithBorder(finalImage, randomText);

        return finalImageWithBorder;
    }

    public Image<Rgba32> AddTextImageCommand(Image<Rgba32> image)
    {
        string randomText = MessageProcessor.KeepUpConversation().Result;

        AddTopTextToImage(image, randomText);

        return image;
    }

    private void AddTopTextToImage(Image<Rgba32> image, string text)
    {
        Font font = SystemFonts.CreateFont("Arial", 36, FontStyle.Bold);

        var options = new TextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center

        };

        var textBounds = TextMeasurer.MeasureBounds(text, options);
        PointF textPosition = new PointF(image.Width / 2 - textBounds.Width / 2, image.Height / 2 - textBounds.Height / 2 + 100);

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
        Font font = SystemFonts.CreateFont("Arial", 16, FontStyle.Bold);
        PointF point = new PointF(10, 10);

        image.Mutate(ctx => ctx.DrawText(text, font, Color.White, point));
    }

    private void AddWatermarkText(Image<Rgba32> image, string text)
    {
        Font font = SystemFonts.CreateFont("Arial", 40, FontStyle.Bold);

        var options = new TextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var textBounds = TextMeasurer.MeasureBounds(text, options);
        float scale = Math.Min(image.Width / textBounds.Width, image.Height / textBounds.Height);

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
        int borderThickness = 20;
        Font font = SystemFonts.CreateFont("Arial", 36, FontStyle.Bold);

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
        PointF textPosition = new PointF(newImage.Width / 2 - textBounds.Width / 2, newImage.Height - borderThickness / 2 - textBounds.Height);

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
    public static Image<Rgba32> Funeral(Image<Rgba32> image) {
        const int maxWidth = 256;
        const int maxHeight = 256;

        var grave = Image.Load<Rgba32>("./grave.png");

        var aspectRatio = (float)image.Width / image.Height;
        if (aspectRatio > 2) {
            image.Mutate(i => i.Resize(image.Width, image.Width / 2));
        } else if (aspectRatio < 0.5) {
            image.Mutate(i => i.Resize(image.Height / 2, image.Height));
        }

        if (image.Width > maxWidth) {
            image.Mutate(i => i.Resize(maxWidth, image.Height / (image.Width / maxWidth)));
        }

        if (image.Height > maxHeight) {
            image.Mutate(i => i.Resize(maxHeight, image.Width / (image.Height / maxHeight)));
        }

        image.Mutate(i => i.Grayscale());

        var font = SystemFonts.CreateFont("Georgia", 30, FontStyle.Italic);
        grave.Mutate(i =>
            i.DrawImage(image, new Point(500 + (maxWidth - image.Width) / 2, 220 + (maxHeight - image.Height) / 2), 1));
        grave.Mutate(i =>
            i.DrawText(DateTime.Parse("17.03.1984").ToString("d") + " - " + DateTime.Today.ToString("d"), font,
                Color.Black, new PointF(465, 500)));
        grave.Mutate(i =>
            i.DrawText("REST IN PENIS", SystemFonts.CreateFont("Georgia", 40, FontStyle.Italic), Color.Black,
                new PointF(480, 560)));

        return grave;
    }
}