using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Net;
using System.Text;
using VkNet;
using VkNet.Model;

namespace vkbot_vitalya;

public static class MessageHandler
{
    private static Random random = new Random();
    private static MemeGen memeGen;
    private static WeatherService weatherService;
    private static DanbooruApi danbooruApi;

    public static void Initialize(MemeGen memeGenInstance, WeatherService weatherServiceInstance, DanbooruApi danbooruApiInstance)
    {
        memeGen = memeGenInstance;
        weatherService = weatherServiceInstance;
        danbooruApi = danbooruApiInstance;
    }

    public static void HandleMessage(VkApi api, Message message, Config config, ulong groupId)
    {
        if (api == null || message == null || config == null)
        {
            Console.WriteLine("API, message, or config is null");
            File.AppendAllText("./log.txt", "API, message, or config is null\n");
            return;
        }

        Console.WriteLine("Handling message...");
        File.AppendAllText("./log.txt", "Handling message...\n");

        // Extract command from message
        var command = message.Text?.ToLower().Trim();
        if (command == null)
        {
            Console.WriteLine("Command is null");
            File.AppendAllText("./log.txt", "Command is null\n");
            return;
        }

        Console.WriteLine($"Command received: {command}");
        File.AppendAllText("./log.txt", $"Command received: {command}\n");

        // Check if the command matches the meme generation pattern
        string prefixMeme = $"{config.BotName.ToLower()} {config.Commands.Meme.ToLower()} ";
        string prefixWeather = $"{config.BotName.ToLower()} {config.Commands.Weather.ToLower()} ";
        string prefixAnime = $"{config.BotName.ToLower()} {config.Commands.Anime.ToLower()}";

        if (command.StartsWith(prefixMeme))
        {
            string keywords = command.Substring(prefixMeme.Length).Trim();
            HandleMemeCommand(api, message, groupId, keywords);
        }
        if (command.StartsWith(prefixWeather))
        {
            string cityName = command.Substring(prefixWeather.Length).Trim();
            HandleWeatherCommand(api, message, cityName);
        }
        if (command.StartsWith(prefixAnime))
        {
            HandleAnimeCommand(api, message, groupId);
        }

        // Log the defined commands for comparison
        Console.WriteLine($"Defined 'Break' command: {config.Commands.Break}");
        Console.WriteLine($"Defined 'Liquidate' command: {config.Commands.Liquidate}");
        Console.WriteLine($"Defined 'Compress' command: {config.Commands.Compress}");
        File.AppendAllText("./log.txt", $"Defined 'Break' command: {config.Commands.Break}\n");
        File.AppendAllText("./log.txt", $"Defined 'Liquidate' command: {config.Commands.Liquidate}\n");
        File.AppendAllText("./log.txt", $"Defined 'Compress' command: {config.Commands.Compress}\n");

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
                    if (command.Contains(config.Commands.Break, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Command 'Break' recognized.");
                        File.AppendAllText("./log.txt", "Command 'Break' recognized.\n");
                        HandleImageCommand(api, message, photoUrl, ImageProcessor.BreakImage, groupId);
                    }
                    else if (command.Contains(config.Commands.Liquidate, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Command 'Liquidate' recognized.");
                        File.AppendAllText("./log.txt", "Command 'Liquidate' recognized.\n");
                        HandleImageCommand(api, message, photoUrl, ImageProcessor.LiquidateImage, groupId);
                    }
                    else if (command.Contains(config.Commands.Compress, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Command 'Compress' recognized.");
                        File.AppendAllText("./log.txt", "Command 'Compress' recognized.\n");
                        HandleImageCommand(api, message, photoUrl, ImageProcessor.CompressImage, groupId);
                    }
                    else if (command.Contains(config.Commands.AddText, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Command 'AddText' recognized.");
                        File.AppendAllText("./log.txt", "Command 'AddText' recognized.\n");
                        HandleImageCommand(api, message, photoUrl, ImageProcessor.AddTextImageCommand, groupId);
                    }
                    else
                    {
                        Console.WriteLine("No matching command found. Generating random message.");
                        File.AppendAllText("./log.txt", "No matching command found. Generating random message.\n");
                        var responseMessage = GenerateRandomMessage();
                        SendResponse(api, message.PeerId.Value, responseMessage);
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

            // Process text commands
            if (random.NextDouble() < config.ResponseProbability || command.Contains(config.Commands.GenerateSentences) || command.Contains(config.Commands.Echo))
            {
                if (command.Contains(config.Commands.GenerateSentences, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Command 'Generate Sentences' recognized.");
                    File.AppendAllText("./log.txt", "Command 'Generate Sentences' recognized.\n");
                    var responseMessage = GenerateMultipleSentences();
                    SendResponse(api, message.PeerId.Value, responseMessage);
                }
                else if (command.Contains(config.Commands.Echo, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Command 'Echo' recognized.");
                    File.AppendAllText("./log.txt", "Command 'Echo' recognized.\n");
                    var echoText = message.Text.Substring(config.Commands.Echo.Length).Trim();
                    SendResponse(api, message.PeerId.Value, echoText);
                }
                else
                {
                    Console.WriteLine("Generating random message.");
                    File.AppendAllText("./log.txt", "Generating random message.\n");
                    var responseMessage = GenerateRandomMessage();
                    SendResponse(api, message.PeerId.Value, responseMessage);
                }
            }
            else
            {
                Console.WriteLine("Message did not match any command and did not trigger response probability.");
                File.AppendAllText("./log.txt", "Message did not match any command and did not trigger response probability.\n");
            }
        }
    }

    private static void HandleImageCommand(VkApi api, Message message, string imageUrl, Func<Image<Rgba32>, Image<Rgba32>> imageProcessor, ulong groupId)
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

    private static string GenerateMultipleSentences()
    {
        var lines = File.ReadAllLines(Program.MessagesFilePath);
        if (lines.Length == 0) return "I have nothing to say.";

        var words = lines.SelectMany(line => line.Split(' ')).ToList();
        var sentences = new List<string>();

        for (var i = 0; i < 5; i++)
        {
            var randomWords = words.OrderBy(x => random.Next()).Take(5).ToArray();
            sentences.Add(string.Join(" ", randomWords));
        }

        return string.Join(". ", sentences) + ".";
    }

    public static string GenerateRandomMessage()
    {
        var lines = File.ReadAllLines(Program.MessagesFilePath);
        if (lines.Length == 0) return "I have nothing to say.";

        var method = random.Next(2);

        if (method == 0)
        {
            var randomMessages = lines.OrderBy(x => random.Next()).Take(2).ToArray();
            return string.Join(" ", randomMessages);
        }
        else
        {
            var words = lines.SelectMany(line => line.Split(' ')).ToList();
            var randomWords = words.OrderBy(x => random.Next()).Take(5).ToArray();
            return string.Join(" ", randomWords);
        }
    }

    private static async void HandleMemeCommand(VkApi api, Message message, ulong groupId, string keywords)
    {
        MemeGenResponse? response = await memeGen.SearchMemes(keywords, 1, MemeType.Image);
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
                using (WebClient webClient = new WebClient())
                {
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
                        Attachments = savedPhotos,
                        Message = GenerateRandomMessage()
                    });

                    Console.WriteLine("Meme sent to user.");
                    File.AppendAllText("./log.txt", "Meme sent to user.\n");
                }
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

    private static async void HandleWeatherCommand(VkApi api, Message message, string cityName)
    {
        WeatherResponse? weatherResponse = await weatherService.GetWeatherAsync(cityName);
        if (weatherResponse != null)
        {
            string weatherMessage =
                $"Погода в {weatherResponse.Name}:\n" +
                $"Температура: {weatherResponse.Main?.Temp ?? 0}°C\n" +
                $"Ощущается как: {weatherResponse.Main?.FeelsLike ?? 0}°C\n" +
                $"Описание: {weatherResponse.Weather?.FirstOrDefault()?.Description}\n" +
                $"Ветер: {weatherResponse.Wind?.Speed ?? 0} м/с\n" +
                $"Влажность: {weatherResponse.Main?.Humidity ?? 0}%";

            SendResponse(api, message.PeerId.Value, weatherMessage);
        }
        else
        {
            SendResponse(api, message.PeerId.Value, "Извините, не удалось получить информацию о погоде.");
        }
    }

    private static async void HandleAnimeCommand(VkApi api, Message message, ulong groupId)
    {
        string commandText = message.Text.ToLower().Trim();
        string[] commandParts = commandText.Split(new[] { ' ' }, 3);

        string tags = "";

        if (commandParts.Length >= 3)
        {
            tags = commandParts[2].Trim();
        }

        // Если теги не указаны, будут использоваться отрицательные теги по умолчанию
        Console.WriteLine($"Requesting Danbooru with tags: {tags}");
        File.AppendAllText("./log.txt", $"Requesting Danbooru with tags: {tags}\n");

        Post? randomPost = await danbooruApi.RandomImageAsync(tags);
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
                using (HttpResponseMessage response = await danbooruApi.Client.GetAsync(imageUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using (Stream inputStream = await response.Content.ReadAsStreamAsync())
                    using (var memoryStream = new MemoryStream())
                    {
                        await inputStream.CopyToAsync(memoryStream);
                        byte[] imageBytes = memoryStream.ToArray();
                        string boundary = "----WebKitFormBoundary7MA4YWxkTrZu0gW";

                        using (var formDataContent = new MultipartFormDataContent(boundary))
                        {
                            formDataContent.Headers.Remove("Content-Type");
                            formDataContent.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
                            var byteArrayContent = new ByteArrayContent(imageBytes);
                            byteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                            formDataContent.Add(byteArrayContent, "file1", "anime.jpg");

                            using (HttpResponseMessage uploadResponse = await danbooruApi.Client.PostAsync(uploadServer, formDataContent))
                            {
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
                        }
                    }
                }
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

    private static void SendResponse(VkApi api, long peerId, string message)
    {
        api.Messages.Send(new MessagesSendParams
        {
            RandomId = random.Next(),
            PeerId = peerId,
            Message = message
        });
        Console.WriteLine($"Sent response: {message}");
        File.AppendAllText("./log.txt", $"Sent response: {message}\n");
    }
}