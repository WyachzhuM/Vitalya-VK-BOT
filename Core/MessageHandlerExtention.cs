using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Net;
using System.Text;
using vkbot_vitalya.Config;
using vkbot_vitalya.Services;
using VkNet;
using VkNet.Model;
using VkNet.Enums.Filters;
using System;
using System.Reflection.Emit;
using vkbot_vitalya.Services.Generators.TextGeneration;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace vkbot_vitalya;

/// <summary>
/// Extention to handlers
/// </summary>
public partial class MessageHandler
{
    private Dictionary<long, int> chaosScores = new Dictionary<long, int>(); // Счёт хаоса

    private async Task HandlePhotoCommand(VkApi api, Message message, ulong groupId, string command, string actualCommand, Conf config)
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
                            var responseMessage = await MessageProcessor.KeepUpConversation();
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
                var responseMessage = await MessageProcessor.KeepUpConversation();
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
                        RandomId = _random.Next(),
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
        Meme meme;
        if (string.IsNullOrEmpty(keywords))
        {
            // Генерируем случайный мем, если ключевые слова отсутствуют
            meme = await ServiceEndpoint.MemeGen.RandomMeme(string.Empty, MemeType.Image);
        }
        else
        {
            // Генерируем мем по введенным ключевым словам
            meme = await ServiceEndpoint.MemeGen.RandomMeme(keywords, MemeType.Image);
        }

        if (meme != null)
        {
            string memeUrl = meme.Url;
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

                string text = await MessageProcessor.KeepUpConversation();

                // Send the saved meme image in a message
                api.Messages.Send(new MessagesSendParams
                {
                    RandomId = _random.Next(),
                    PeerId = message.PeerId.Value,
                    ReplyTo = message.Id,
                    Attachments = savedPhotos,
                    Message = text
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

    private async void HandleAnimeCommand(VkApi api, Message message, ulong groupId, string _tags = "")
    {
        string commandText = message.Text.ToLower().Trim();
        string[] commandParts = commandText.Split(new[] { ' ' }, 3);

        string tags = "";

        if (_tags == "")
        {
            if (commandParts.Length >= 3) tags = commandParts[2].Trim();
        }
        else
        {
            tags = _tags;
        }

        // Если теги не указаны, будут использоваться отрицательные теги по умолчанию
        Console.WriteLine($"Requesting Danbooru with tags: {tags}");
        File.AppendAllText("./log.txt", $"Requesting Danbooru with tags: {tags}\n");

        Console.WriteLine(tags);

        var randomPost = await ServiceEndpoint.SafebooruApi.GetRandomPostAsync(tags);

        if (randomPost != null)
        {
            string imageUrl = randomPost.FileUrl;
            Console.WriteLine($"Found image URL: {imageUrl}");
            File.AppendAllText("./log.txt", $"Found image URL: {imageUrl}\n");

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

                List<string> variableLabel = new List<string>()
                {
                    "Еще!",
                    "Ещ...е.. а.",
                    "Ах!! !!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "Еще!",
                    "ещо"
                };

                Random random = new Random();

                var b = new MessageKeyboardButton
                {
                    Action = new MessageKeyboardButtonAction
                    {
                        Type = VkNet.Enums.StringEnums.KeyboardButtonActionType.Text,
                        Label = variableLabel[random.Next(variableLabel.Count)],
                        Payload = JsonConvert.SerializeObject(new { command = "anim", _tags = tags })
                    }
                };

                List<MessageKeyboardButton> buttonsRow1 = new List<MessageKeyboardButton> { b };

                var values = new List<List<MessageKeyboardButton>> { buttonsRow1 };

                var keyboard = new MessageKeyboard
                {
                    Buttons = values,
                    Inline = true
                };

                api.Messages.Send(new MessagesSendParams
                {
                    RandomId = _random.Next(),
                    PeerId = message.PeerId.Value,
                    Attachments = savedPhotos,
                    ReplyTo = message.Id,
                    Keyboard = keyboard
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

    private async void HandleHCommand(VkApi api, Message message, ulong groupId, string _tags = "")
    {
        bool isForb = false;

        Action onForbriddenTag = () =>
        {
            isForb = true;
        };

        string commandText = message.Text.ToLower().Trim();
        string[] commandParts = commandText.Split(new[] { ' ' }, 3);

        string tags = "";

        if(_tags == "")
        {
            if (commandParts.Length >= 3) tags = commandParts[2].Trim();
        }
        else
        {
            tags = _tags;
        }

        // Если теги не указаны, будут использоваться отрицательные теги по умолчанию
        Console.WriteLine($"Requesting Danbooru with tags: {tags}");
        File.AppendAllText("./log.txt", $"Requesting Danbooru with tags: {tags}\n");

        Services.Post? randomPost = await ServiceEndpoint.DanbooruApi.RandomImageAsync(onForbriddenTag, tags);

        if (isForb)
            return;

        if (randomPost != null)
        {
            string imageUrl = randomPost.FileUrl;
            Console.WriteLine($"Found image URL: {imageUrl}");
            File.AppendAllText("./log.txt", $"Found image URL: {imageUrl}\n");

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
                formDataContent.Add(byteArrayContent, "file1", "h.jpg");

                using HttpResponseMessage uploadResponse = await ServiceEndpoint.DanbooruApi.Client.PostAsync(uploadServer, formDataContent);
                uploadResponse.EnsureSuccessStatusCode();
                string responseString = await uploadResponse.Content.ReadAsStringAsync();
                var savedPhotos = api.Photo.SaveMessagesPhoto(responseString);
                Console.WriteLine("Anime image uploaded to VK.");
                File.AppendAllText("./log.txt", "Anime image uploaded to VK.\n");

                var b = new MessageKeyboardButton
                {
                    Action = new MessageKeyboardButtonAction
                    {
                        Type = VkNet.Enums.StringEnums.KeyboardButtonActionType.Text,
                        Label = "Еще!",
                        Payload = JsonConvert.SerializeObject(new { command = "hen", _tags = tags })
                    }
                };

                List<MessageKeyboardButton> buttonsRow1 = new List<MessageKeyboardButton> { b };

                var values = new List<List<MessageKeyboardButton>> { buttonsRow1 };

                var keyboard = new MessageKeyboard
                {
                    Buttons = values,
                    Inline = true
                };

                api.Messages.Send(new MessagesSendParams
                {
                    RandomId = _random.Next(),
                    PeerId = message.PeerId.Value,
                    Attachments = savedPhotos,
                    ReplyTo = message.Id,
                    Keyboard = keyboard
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
            RandomId = _random.Next(),
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

            string text = await MessageProcessor.KeepUpConversation();

            // Send the saved meme image in a message
            api.Messages.Send(new MessagesSendParams
            {
                RandomId = _random.Next(),
                PeerId = message.PeerId.Value,
                ReplyTo = message.Id,
                Attachments = savedPhotos,
                Message = $"{location} {text} \n{output.Item2.Item1}\n{output.Item2.Item2}",
                //Lat = long.Parse(output.Item2.lat),
                //Longitude = long.Parse(output.Item2.lon)
            });
        }
        catch
        {
            api.Messages.Send(new MessagesSendParams
            {
                RandomId = _random.Next(),
                PeerId = message.PeerId.Value,
                ReplyTo = message.Id,
                Message = $"{location} - Не удалось найти это место!"
            });
        }
    }

    private async void HandlePythonCommand(VkApi api, Message message, ulong groupId)
    {
        string commandText = message.Text.Trim();
        string[] commandParts = commandText.Split(new[] { ' ' }, 3);

        if (commandParts.Length < 3 || !commandParts[1].Equals("py", StringComparison.OrdinalIgnoreCase))
        {
            SendResponse(api, message.PeerId.Value, "Пожалуйста, укажи Python-код после команды, например: `v py print('Hello')`");
            return;
        }

        string pythonCode = commandParts[2].Trim();
        Console.WriteLine($"Received Python code: {pythonCode}");
        File.AppendAllText("./log.txt", $"Received Python code: {pythonCode}\n");

        string pythonCodeLower = pythonCode.ToLower();
        if (Regex.IsMatch(pythonCodeLower, @"(os|sys|subprocess|import|exec|eval|\bimp\b|\bort\b)"))
        {
            SendResponse(api, message.PeerId.Value, "Использование системных модулей, импорта или опасных функций запрещено.");
            return;
        }

        if (pythonCode.Length > 1000)
        {
            SendResponse(api, message.PeerId.Value, "Слишком длинный код (максимум 1000 символов).");
            return;
        }

        try
        {
            string output = await ExecutePythonCode(pythonCode);
            SendResponse(api, message.PeerId.Value, output.Length > 0 ? output : "Код выполнен, но вывода нет.");
            Console.WriteLine("Python code executed successfully.");
            File.AppendAllText("./log.txt", "Python code executed successfully.\n");
        }
        catch (Exception ex)
        {
            SendResponse(api, message.PeerId.Value, "Ошибка при выполнении кода: " + ex.Message);
            Console.WriteLine($"Error executing Python code: {ex.Message}");
            File.AppendAllText("./log.txt", $"Error executing Python code: {ex.Message}\n");
        }
    }

    private async Task<string> ExecutePythonCode(string code)
    {
        string pythonPath = "python";

        string arguments = $"-c \"{code.Replace("\"", "\\\"")}\"";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = arguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (Process process = new Process { StartInfo = startInfo })
        {
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            process.OutputDataReceived += (sender, args) => { if (args.Data != null) output.AppendLine(args.Data); };
            process.ErrorDataReceived += (sender, args) => { if (args.Data != null) error.AppendLine(args.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            bool completed = await Task.Run(() => process.WaitForExit(5000));

            if (!completed)
            {
                process.Kill();
                throw new Exception("Код выполнялся слишком долго (более 5 секунд).");
            }

            process.WaitForExit();

            if (error.Length > 0)
            {
                return $"Ошибка: {error.ToString().Trim()}";
            }

            return output.ToString().Trim();
        }
    }

    private async void HandleChaosCommand(VkApi api, Message message, ulong groupId)
    {
        string commandText = message.Text.Trim();
        string[] commandParts = commandText.Split(new[] { ' ' }, 2); // "v chaos"

        if (commandParts.Length < 2 || !commandParts[1].Equals("chaos", StringComparison.OrdinalIgnoreCase))
        {
            SendResponse(api, message.PeerId.Value, "Просто напиши: `v chaos`");
            return;
        }

        Console.WriteLine("Starting chaos...");
        File.AppendAllText("./log.txt", "Starting chaos...\n");

        try
        {
            var members = await api.Messages.GetConversationMembersAsync(message.PeerId.Value);
            var randomMember = members.Profiles.OrderBy(x => Guid.NewGuid()).First();
            long victimId = randomMember.Id;

            string task = GenerateChaosTask(randomMember.FirstName);

            var buttons = new List<MessageKeyboardButton>
        {
            new MessageKeyboardButton
            {
                Action = new MessageKeyboardButtonAction
                {
                    Type = VkNet.Enums.StringEnums.KeyboardButtonActionType.Text,
                    Label = "Выполнено",
                    Payload = JsonConvert.SerializeObject(new { command = "chaos_done", victim = victimId })
                }
            },
            new MessageKeyboardButton
            {
                Action = new MessageKeyboardButtonAction
                {
                    Type = VkNet.Enums.StringEnums.KeyboardButtonActionType.Text,
                    Label = "Провал",
                    Payload = JsonConvert.SerializeObject(new { command = "chaos_fail", victim = victimId })
                }
            }
        };

            var keyboard = new MessageKeyboard
            {
                Buttons = new List<List<MessageKeyboardButton>> { buttons },
                Inline = true
            };

            await api.Messages.SendAsync(new MessagesSendParams
            {
                RandomId = new Random().Next(),
                PeerId = message.PeerId.Value,
                Message = $"🔥 Хаос начинается! Жертва: [id{victimId}|{randomMember.FirstName} {randomMember.LastName}]\nЗадание: {task}\nГолосуйте!",
                Keyboard = keyboard
            });

            Console.WriteLine($"Chaos task assigned to {randomMember.FirstName}: {task}");
            File.AppendAllText("./log.txt", $"Chaos task assigned to {randomMember.FirstName}: {task}\n");
        }
        catch (Exception ex)
        {
            SendResponse(api, message.PeerId.Value, "Ошибка хаоса: " + ex.Message);
            Console.WriteLine($"Error in chaos: {ex.Message}");
            File.AppendAllText("./log.txt", $"Error in chaos: {ex.Message}\n");
        }
    }

    private string GenerateChaosTask(string name)
    {
        Random random = new Random();
        string[] actions = {
        $"выебать {name}",
        $"трахнуть {name}",
        $"написать выебан на жопе {name}",
        $"пойти нахуй",
        $"сделать KYS",
    };
        return actions[random.Next(actions.Length)];
    }

    #region Settings
    private async void HandleSettingsCommand(VkApi api, Message message, ulong groupId)
    {
        var chat = _saves.Chats.FirstOrDefault(c => c.PeerID == message.PeerId.Value);

        if (chat == null)
        {
            SendResponse(api, message.PeerId.Value, "Чат не найден.");
            return;
        }

        if (chat.Propertyes == null)
        {
            chat.Propertyes = new ChatPropertyes();
            _saves.Save(SavesFilePath);
        }

        var b1 = CreateToggleButton(chat.Propertyes.IsAnime, "anime", "Аниме");
        var b2 = CreateToggleButton(chat.Propertyes.IsHentai, "hentai", "Хентай");
        var b3 = CreateToggleButton(chat.Propertyes.IsImageProccestion, "image_processing", "Обработка изображений");
        var b4 = CreateToggleButton(chat.Propertyes.IsMeme, "meme", "Мемы");
        var b5 = CreateToggleButton(chat.Propertyes.IsWeather, "weather", "Погода");
        var b6 = CreateToggleButton(chat.Propertyes.IsLocation, "location", "Местоположение");

        List<MessageKeyboardButton> buttonsRow1 = new List<MessageKeyboardButton> { b1, b2, b3 };
        List<MessageKeyboardButton> buttonsRow2 = new List<MessageKeyboardButton> { b4, b5, b6 };

        var values = new List<List<MessageKeyboardButton>> { buttonsRow1, buttonsRow2 };

        var keyboard = new MessageKeyboard
        {
            Buttons = values,
            Inline = true
        };

        await api.Messages.SendAsync(new MessagesSendParams
        {
            RandomId = _random.Next(),
            PeerId = message.PeerId.Value,
            Message = "Настройки чата",
            Keyboard = keyboard
        });
    }

    // Метод для создания переключающих кнопок
    private MessageKeyboardButton CreateToggleButton(bool isEnabled, string command, string label)
    {
        return new MessageKeyboardButton
        {
            Action = new MessageKeyboardButtonAction
            {
                Type = VkNet.Enums.StringEnums.KeyboardButtonActionType.Text,
                Label = isEnabled ? $"✅ {label}" : $"🚫 {label}",
                Payload = JsonConvert.SerializeObject(new { command = $"toggle_{command}" })
            }
        };
    }

    private async Task<bool> IsUserAdmin(VkApi api, long chatId, long userId)
    {
        try
        {
            var members = await api.Messages.GetConversationMembersAsync(chatId, null, (ulong?)_groupId, default);

            var admins = members.Items.Where(x => x.IsAdmin).Select(x => x.MemberId);
            return admins.Contains(userId);
        }
        catch (VkNet.Exception.ConversationAccessDeniedException ex)
        {
            Console.WriteLine($"Access denied to chat {chatId}: {ex.Message}");
            return false;
        }
    }

    // Метод для обработки Payload (данные, переданные кнопкой)
    public async void HandlePayload(VkApi api, Message message, ulong groupId)
    {
        if (message.Payload != null)
        {
            dynamic payload = JsonConvert.DeserializeObject(message.Payload);
            string command = payload.command;
            long chatId = message.PeerId.Value;

            switch (command)
            {
                case "anim":
                    string tags = payload._tags;
                    HandleAnimeCommand(api, message, groupId, tags);
                    return;
                case "hen":
                    string tagshen = payload._tags;
                    HandleHCommand(api, message, groupId, tagshen);
                    return;

                case "chaos_done":
                    long doneVictim = payload.victim;
                    chaosScores[doneVictim] = chaosScores.GetValueOrDefault(doneVictim) + 1;
                    SendResponse(api, message.PeerId.Value, $"Задание выполнено! [id{doneVictim}|Жертва] получает +1 хаос-очко. Текущий счёт: {chaosScores[doneVictim]}");
                    return;

                case "chaos_fail":
                    long failVictim = payload.victim;
                    chaosScores[failVictim] = chaosScores.GetValueOrDefault(failVictim) - 1;
                    SendResponse(api, message.PeerId.Value, $"Задание провалено! [id{failVictim}|Жертва] теряет 1 хаос-очко. Текущий счёт: {chaosScores[failVictim]}");
                    return;
            }

            long userId = message.FromId.Value;

            if (!await IsUserAdmin(api, message.PeerId.Value, userId))
            {
                SendResponse(api, message.PeerId.Value, "Только админы могут изменять настройки.");
                return;
            }

            var chat = _saves.Chats.FirstOrDefault(c => c.PeerID == chatId);
            if (chat == null)
            {
                SendResponse(api, message.PeerId.Value, "Чат не найден.");
                return;
            }

            if (chat.Propertyes == null)
            {
                chat.Propertyes = new ChatPropertyes();
                _saves.Save(SavesFilePath);
            }

            switch (command)
            {
                case "toggle_anime":
                    chat.Propertyes.IsAnime = !chat.Propertyes.IsAnime;
                    break;
                case "toggle_hentai":
                    chat.Propertyes.IsHentai = !chat.Propertyes.IsHentai;
                    break;
                case "toggle_image_processing":
                    chat.Propertyes.IsImageProccestion = !chat.Propertyes.IsImageProccestion;
                    break;
                case "toggle_meme":
                    chat.Propertyes.IsMeme = !chat.Propertyes.IsMeme;
                    break;
                case "toggle_weather":
                    chat.Propertyes.IsWeather = !chat.Propertyes.IsWeather;
                    break;
                case "toggle_location":
                    chat.Propertyes.IsLocation = !chat.Propertyes.IsLocation;
                    break;

                default:
                    SendResponse(api, message.PeerId.Value, "Неизвестная команда.");
                    return;
            }

            _saves.Save(SavesFilePath);

            SendResponse(api, message.PeerId.Value, "Настройки обновлены.");

            // Отправляем обновленную клавиатуру
            HandleSettingsCommand(api, message, (ulong)chatId);
        }
    }
    #endregion
}

public static class MessagesExtentions
{
    public static void Out(this Message message)
    {
        string isReply = message.ReplyMessage != null ? $"Reply from userID: {message.ReplyMessage.FromId}" : "Is not reply";

        string formatted = $"from: {message.FromId}, mId:{message.Id} : {message.Date}";

        //200000000 для бесед
        if (message.PeerId.ToString().StartsWith("200000000"))
            Console.ForegroundColor = ConsoleColor.Red;
        else
            Console.ForegroundColor = ConsoleColor.Green;

        Console.WriteLine(formatted);
        Console.ForegroundColor = ConsoleColor.White;
    }
}
