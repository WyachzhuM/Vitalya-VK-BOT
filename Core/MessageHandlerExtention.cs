using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;
using System.Text;
using vkbot_vitalya.Config;
using vkbot_vitalya.Services;
using vkbot_vitalya.Services.Generators;
using VkNet;
using VkNet.Model;

namespace vkbot_vitalya;

/// <summary>
/// Extention to handlers
/// </summary>
public partial class MessageHandler
{
    private void HandlePhotoCommand(VkApi api, Message message, ulong groupId, string command, string actualCommand, Conf config)
    {
        // Get attachments from message
        var attachments = message.Attachments;

        if (attachments != null && attachments.Count > 0 && attachments[0].Instance is Photo photo)
        {
            // Get the URL of the largest photo
            var largestPhoto = photo.Sizes?.OrderByDescending(s => s.Width * s.Height).FirstOrDefault();
            if (largestPhoto != null)
            {
                var photoUrl = largestPhoto.Url?.AbsoluteUri;
                if (photoUrl != null)
                {
                    Console.WriteLine($"Photo URL: {photoUrl}");
                    File.AppendAllText("./log.txt", $"Photo URL: {photoUrl}\n");

                    // Determine the command and call the appropriate image processing function
                    switch (actualCommand)
                    {
                        case "break":
                            Console.WriteLine("Command 'Break' recognized.");
                            File.AppendAllText("./log.txt", "Command 'Break' recognized.\n");
                            HandleImageCommand(api, message, photoUrl, Processor.BreakImage, groupId);
                            break;
                        case "liquidate":
                            Console.WriteLine("Command 'Liquidate' recognized.");
                            File.AppendAllText("./log.txt", "Command 'Liquidate' recognized.\n");
                            HandleImageCommand(api, message, photoUrl, Processor.LiquidateImage, groupId);
                            break;
                        case "compress":
                            Console.WriteLine("Command 'Compress' recognized.");
                            File.AppendAllText("./log.txt", "Command 'Compress' recognized.\n");
                            HandleImageCommand(api, message, photoUrl, Processor.CompressImage, groupId);
                            break;
                        case "add_text":
                            Console.WriteLine("Command 'AddText' recognized.");
                            File.AppendAllText("./log.txt", "Command 'AddText' recognized.\n");
                            HandleImageCommand(api, message, photoUrl, Processor.AddTextImageCommand, groupId);
                            break;
                        default:
                            Console.WriteLine("No matching command found. Generating random message.");
                            File.AppendAllText("./log.txt", "No matching command found. Generating random message.\n");
                            var responseMessage = MessageProcessor.GenerateRandomMessage();
                            SendResponse(api, message.PeerId.Value, responseMessage);
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Photo URL is null.");
                    File.AppendAllText("./log.txt", "Photo URL is null.\n");
                }
            }
            else
            {
                Console.WriteLine("No largest photo found.");
                File.AppendAllText("./log.txt", "No largest photo found.\n");
            }
        }
        else
        {
            Console.WriteLine("No photo attachments found or attachments are not photos.");
            File.AppendAllText("./log.txt", "No photo attachments found or attachments are not photos.\n");

            // Handle cases where message doesn't contain a photo but may still need to respond
            if (command.Contains(config.Commands["generate_sentences"].First(), StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Command 'Generate Sentences' recognized.");
                File.AppendAllText("./log.txt", "Command 'Generate Sentences' recognized.\n");
                var responseMessage = MessageProcessor.GenerateMultipleSentences();
                SendResponse(api, message.PeerId.Value, responseMessage);
            }
            else if (command.Contains(config.Commands["echo"].First(), StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Command 'Echo' recognized.");
                File.AppendAllText("./log.txt", "Command 'Echo' recognized.\n");
                var echoText = message.Text.Substring(config.Commands["echo"].First().Length).Trim();
                SendResponse(api, message.PeerId.Value, echoText);
            }
        }
    }

    private void HandleImageCommand(VkApi api, Message message, string imageUrl, Func<Image<Rgba32>, Image<Rgba32>> imageProcessor, ulong groupId)
    {
        Console.WriteLine("Handling image command...");
        File.AppendAllText("./log.txt", "Handling image command...\n");

        try
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] imageBytes = webClient.DownloadData(imageUrl);
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    Image<Rgba32> originalImage;

                    try
                    {
                        originalImage = SixLabors.ImageSharp.Image.Load<Rgba32>(ms);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error loading image: {e.Message}");
                        File.AppendAllText("./log.txt", $"Error loading image: {e.Message}\n");
                        return;
                    }

                    Image<Rgba32> processedImage;

                    try
                    {
                        processedImage = imageProcessor(originalImage);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error processing image: {e.Message}");
                        File.AppendAllText("./log.txt", $"Error processing image: {e.Message}\n");
                        return;
                    }

                    string outputPath = "./output.jpg";

                    try
                    {
                        processedImage.Save(outputPath, new JpegEncoder());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error saving image: {e.Message}");
                        File.AppendAllText("./log.txt", $"Error saving image: {e.Message}\n");
                        return;
                    }

                    Console.WriteLine("Image processed and saved to disk.");
                    File.AppendAllText("./log.txt", "Image processed and saved to disk.\n");

                    // Get the server for uploading photos
                    var uploadServer = api.Photo.GetMessagesUploadServer((long)groupId).UploadUrl;
                    Console.WriteLine($"Upload URL: {uploadServer}");
                    File.AppendAllText("./log.txt", $"Upload URL: {uploadServer}\n");

                    // Upload the processed photo to the server
                    var responseBytes = webClient.UploadFile(uploadServer, outputPath);
                    var responseString = Encoding.ASCII.GetString(responseBytes);

                    // Save the processed photo
                    var savedPhotos = api.Photo.SaveMessagesPhoto(responseString);
                    Console.WriteLine("Photo uploaded to VK.");
                    File.AppendAllText("./log.txt", "Photo uploaded to VK.\n");

                    // Send the saved photo in a message
                    api.Messages.Send(new MessagesSendParams
                    {
                        RandomId = random.Next(),
                        PeerId = message.PeerId.Value,
                        ReplyTo = message.Id,
                        Attachments = savedPhotos
                    });

                    Console.WriteLine("Processed photo sent to user.");
                    File.AppendAllText("./log.txt", "Processed photo sent to user.\n");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in HandleImageCommand: {ex.Message}");
            File.AppendAllText("./log.txt", $"Exception in HandleImageCommand: {ex.Message}\n");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            File.AppendAllText("./log.txt", $"Stack Trace: {ex.StackTrace}\n");
        }
    }

    private async void HandleMemeCommand(VkApi api, Message message, ulong groupId, string keywords)
    {
        MemeGenResponse? response = await ServiceEndpoint.MemeGen.SearchMemes(keywords, 1, MemeType.Image);
        if (response != null && response.Memes.Count > 0)
        {
            string memeUrl = response.Memes[0].Url;
            Console.WriteLine($"Found meme URL: {memeUrl}");
            File.AppendAllText("./log.txt", $"Found meme URL: {memeUrl}\n");

            // Get the server for uploading photos
            var uploadServer = api.Photo.GetMessagesUploadServer((long)groupId).UploadUrl;
            Console.WriteLine($"Upload URL: {uploadServer}");
            File.AppendAllText("./log.txt", $"Upload URL: {uploadServer}\n");

            try
            {
                // Download meme image
                using WebClient webClient = new WebClient();
                byte[] imageBytes = webClient.DownloadData(memeUrl);
                string outputPath = "./meme.jpg";
                File.WriteAllBytes(outputPath, imageBytes);

                // Upload the meme image to the server
                var responseBytes = webClient.UploadFile(uploadServer, outputPath);
                var responseString = Encoding.ASCII.GetString(responseBytes);

                // Save the uploaded meme image
                var savedPhotos = api.Photo.SaveMessagesPhoto(responseString);
                Console.WriteLine("Meme uploaded to VK.");
                File.AppendAllText("./log.txt", "Meme uploaded to VK.\n");

                // Send the saved meme image in a message
                api.Messages.Send(new MessagesSendParams
                {
                    RandomId = random.Next(),
                    PeerId = message.PeerId.Value,
                    ReplyTo = message.Id,
                    Attachments = savedPhotos,
                    Message = MessageProcessor.GenerateRandomMessage()
                });

                Console.WriteLine("Meme sent to user.");
                File.AppendAllText("./log.txt", "Meme sent to user.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in HandleMemeCommand: {ex.Message}");
                File.AppendAllText("./log.txt", $"Exception in HandleMemeCommand: {ex.Message}\n");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                File.AppendAllText("./log.txt", $"Stack Trace: {ex.StackTrace}\n");
            }
        }
        else
        {
            Console.WriteLine("No meme found.");
            File.AppendAllText("./log.txt", "No meme found.\n");
            SendResponse(api, message.PeerId.Value, "Извините, не удалось найти мемы по заданным ключевым словам.");
        }
    }

    private async void HandleWeatherCommand(VkApi api, Message message, string cityName)
    {
        WeatherResponse? weatherResponse = await ServiceEndpoint.WeatherService.GetWeatherAsync(cityName);
        if (weatherResponse != null)
        {
            string weatherMessage =
                $"Погода в {weatherResponse.Name}:\n" +
                $"Температура: {weatherResponse.Main?.Temp ?? 0}°C\n" +
                $"Ощущается как: {weatherResponse.Main?.FeelsLike ?? 0}°C\n" +
                $"Описание: {weatherResponse.Weather?.FirstOrDefault()?.Description}\n" +
                $"Ветер: {weatherResponse.Wind?.Speed ?? 0} м/с\n" +
                $"Влажность: {weatherResponse.Main?.Humidity ?? 0}%";

            SendResponse(api, message.PeerId.Value, weatherMessage, message.Id);
        }
        else
        {
            SendResponse(api, message.PeerId.Value, "Извините, не удалось получить информацию о погоде.", message.Id);
        }
    }

    private async void HandleAnimeCommand(VkApi api, Message message, ulong groupId)
    {
        bool isForb = false;

        Action onForbriddenTag = () =>
        {
            api.Messages.Send(new MessagesSendParams
            {
                RandomId = random.Next(),
                ReplyTo = message.Id,
                PeerId = message.PeerId.Value,
                Message = "Я не буду эту хуйню постить"
            });
            isForb = true;
        };

        string commandText = message.Text.ToLower().Trim();
        string[] commandParts = commandText.Split(new[] { ' ' }, 3);

        string tags = "";

        if (commandParts.Length >= 3) tags = commandParts[2].Trim();

        // Если теги не указаны, будут использоваться отрицательные теги по умолчанию
        Console.WriteLine($"Requesting Danbooru with tags: {tags}");
        File.AppendAllText("./log.txt", $"Requesting Danbooru with tags: {tags}\n");

        vkbot_vitalya.Services.Post? randomPost = await ServiceEndpoint.DanbooruApi.RandomImageAsync(onForbriddenTag, tags);

        if (isForb)
            return;

        if (randomPost != null)
        {
            string imageUrl = randomPost.FileUrl;
            Console.WriteLine($"Found image URL: {imageUrl}");
            File.AppendAllText("./log.txt", $"Found image URL: {imageUrl}\n");

            if (!imageUrl.EndsWith(".jpg") && !imageUrl.EndsWith(".jpeg") && !imageUrl.EndsWith(".png") && !imageUrl.EndsWith(".gif"))
            {
                Console.WriteLine("Unsupported file type.");
                File.AppendAllText("./log.txt", "Unsupported file type.\n");
                SendResponse(api, message.PeerId.Value, "Извините, не удалось найти изображение аниме, так как файл оказался неподдерживаемым типом.");
                return;
            }

            var uploadServer = api.Photo.GetMessagesUploadServer((long)groupId).UploadUrl;
            Console.WriteLine($"Upload URL: {uploadServer}");
            File.AppendAllText("./log.txt", $"Upload URL: {uploadServer}\n");

            try
            {
                using HttpResponseMessage response = await ServiceEndpoint.DanbooruApi.Client.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();
                using Stream inputStream = await response.Content.ReadAsStreamAsync();
                using var memoryStream = new MemoryStream();
                await inputStream.CopyToAsync(memoryStream);
                byte[] imageBytes = memoryStream.ToArray();
                string boundary = "----WebKitFormBoundary7MA4YWxkTrZu0gW";

                using var formDataContent = new MultipartFormDataContent(boundary);
                formDataContent.Headers.Remove("Content-Type");
                formDataContent.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
                var byteArrayContent = new ByteArrayContent(imageBytes);
                byteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                formDataContent.Add(byteArrayContent, "file1", "anime.jpg");

                using HttpResponseMessage uploadResponse = await ServiceEndpoint.DanbooruApi.Client.PostAsync(uploadServer, formDataContent);
                uploadResponse.EnsureSuccessStatusCode();
                string responseString = await uploadResponse.Content.ReadAsStringAsync();
                var savedPhotos = api.Photo.SaveMessagesPhoto(responseString);
                Console.WriteLine("Anime image uploaded to VK.");
                File.AppendAllText("./log.txt", "Anime image uploaded to VK.\n");

                api.Messages.Send(new MessagesSendParams
                {
                    RandomId = random.Next(),
                    PeerId = message.PeerId.Value,
                    Attachments = savedPhotos
                });

                Console.WriteLine("Anime image sent to user.");
                File.AppendAllText("./log.txt", "Anime image sent to user.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in HandleAnimeCommand: {ex.Message}");
                File.AppendAllText("./log.txt", $"Exception in HandleAnimeCommand: {ex.Message}\n");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                File.AppendAllText("./log.txt", $"Stack Trace: {ex.StackTrace}\n");
            }
        }
        else
        {
            Console.WriteLine("No anime image found.");
            File.AppendAllText("./log.txt", "No anime image found.\n");
            SendResponse(api, message.PeerId.Value, "Извините, не удалось найти изображение аниме.");
        }
    }

    private async void HandleHelpCommand(VkApi api, Message message, ulong groupId)
    {
        string help = File.ReadAllText("./config.json");

        await api.Messages.SendAsync(new MessagesSendParams
        {
            RandomId = random.Next(),
            PeerId = message.PeerId.Value,
            ReplyTo = message.Id,
            Message = help
        });
    }

    private async void HandleSearchCommand(VkApi api, Message message, ulong groupId, string location)
    {
        var output = await ServiceEndpoint.Map.Search(location);

        var outputPath = output.Item1;

        try
        {
            var uploadServer = api.Photo.GetMessagesUploadServer((long)groupId).UploadUrl;
            Console.WriteLine($"Upload URL: {uploadServer}");
            File.AppendAllText("./log.txt", $"Upload URL: {uploadServer}\n");

            using WebClient webClient = new WebClient();
            var responseBytes = webClient.UploadFile(uploadServer, outputPath);
            var responseString = Encoding.ASCII.GetString(responseBytes);

            var savedPhotos = api.Photo.SaveMessagesPhoto(responseString);
            Console.WriteLine("location uploaded to VK.");
            File.AppendAllText("./log.txt", "Location photo uploaded to VK.\n");

            // Send the saved meme image in a message
            api.Messages.Send(new MessagesSendParams
            {
                RandomId = random.Next(),
                PeerId = message.PeerId.Value,
                ReplyTo = message.Id,
                Attachments = savedPhotos,
                Message = $"{location} {MessageProcessor.GenerateRandomMessage()} \n{output.Item2.Item1}\n{output.Item2.Item2}",
                //Lat = long.Parse(output.Item2.lat),
                //Longitude = long.Parse(output.Item2.lon)
            });
        }
        catch
        {
            api.Messages.Send(new MessagesSendParams
            {
                RandomId = random.Next(),
                PeerId = message.PeerId.Value,
                ReplyTo = message.Id,
                Message = $"{location} - Не удалось найти это место!"
            });
        }
    }
}

public static class MessagesExtentions
{
    public static void Out(this Message message)
    {
        string isReply = message.ReplyMessage != null ? $"Reply from userID: {message.ReplyMessage.FromId}" : "Is not reply";

        string formatted = "\n==============================\n" +
    $"Message_{message.Id}\n" +
    $"From userID: {message.FromId}\n" +
    $"In chat: {message.PeerId}\n" +
    $"Created At: {message.Date}\n" +
    $"{isReply}\n" +
    "==============================\n";

        //200000000 для бесед
        if (message.PeerId.ToString().StartsWith("200000000"))
            Console.ForegroundColor = ConsoleColor.Red;
        else
            Console.ForegroundColor = ConsoleColor.Green;

        Console.WriteLine(formatted);
        Console.ForegroundColor = ConsoleColor.White;
    }
}
