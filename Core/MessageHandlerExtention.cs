using System.Collections.ObjectModel;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;
using System.Text;
using vkbot_vitalya.Config;
using vkbot_vitalya.Services;
using VkNet;
using VkNet.Model;
using vkbot_vitalya.Services.Generators.TextGeneration;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using vkbot_vitalya.Core;
using vkbot_vitalya.Services.Generators;
using Image = SixLabors.ImageSharp.Image;

namespace vkbot_vitalya;

/// <summary>
/// Extention to handlers
/// </summary>
public partial class MessageHandler
{
    private Dictionary<long, int> chaosScores = new Dictionary<long, int>(); // Счёт хаоса

    private async Task HandlePhotoCommand(VkApi api, Message message, ulong groupId, string command, Conf config) {
        var image = await FindImageInMessage(message);
        if (image == null) {
            L.W("Tried to handle photo command, but photo not found.");
            return;
        }

        switch (command) {
            case "break":
                L.M("Command 'Break' recognized.");
                HandleImageCommand(api, message, image, Processor.BreakImage, groupId);
                break;
            case "liquidate":
                L.M("Command 'Liquidate' recognized.");
                HandleImageCommand(api, message, image, Processor.LiquidateImage, groupId);
                break;
            case "compress":
                L.M("Command 'Compress' recognized.");
                HandleImageCommand(api, message, image, Processor.CompressImage, groupId);
                break;
            case "add_text":
                L.M("Command 'AddText' recognized.");
                HandleImageCommand(api, message, image, Processor.AddTextImageCommand, groupId);
                break;
            case "funeral":
                L.M("Command 'Funeral' recognized.");
                HandleFuneralCommand(api, message, groupId);
                return;
            default:
                L.W("Tried to handle photo command, but command doesn't need a photo. Generating random message.");
                var responseMessage = await MessageProcessor.KeepUpConversation();
                SendResponse(api, message.PeerId.Value, responseMessage);
                break;
        }
    }

    private async void HandleImageCommand(VkApi api, Message message, Image<Rgba32> originalImage,
        Func<Image<Rgba32>, Image<Rgba32>> imageProcessor, ulong groupId) {
        L.M("Handling image command...");

        try {
            Image<Rgba32> processedImage;
            var sw = new Stopwatch();
            try {
                sw.Start();
                processedImage = imageProcessor(originalImage);
                sw.Stop();
                L.M($"Image processing took {sw.ElapsedMilliseconds} ms");
            } catch (Exception e) {
                L.E($"Error processing image: {e.Message}");
                return;
            }

            var photos = await UploadImageToVk(api, processedImage, groupId);
            if (photos == null) {
                return;
            }

            // Send the saved photo in a message
            api.Messages.Send(new MessagesSendParams {
                RandomId = _random.Next(),
                PeerId = message.PeerId.Value,
                ReplyTo = message.Id,
                Attachments = photos
            });

            L.M("Processed photo sent to user.");
        } catch (Exception ex) {
            L.E($"Exception in HandleImageCommand: {ex.Message}");
            L.E($"Stack Trace: {ex.StackTrace}");
        }
    }

    /// Jpeg only
    public static async Task<ReadOnlyCollection<Photo>?> UploadImageToVk(VkApi api, Image image, ulong groupId) {
        var sw = new Stopwatch();
        sw.Start();
        // Get the server for uploading photos
        var uploadUrl = api.Photo.GetMessagesUploadServer((long)groupId).UploadUrl;
        L.M($"Upload URL: {uploadUrl}");

        // Upload the processed photo to VK
        using var memoryStream = new MemoryStream();
        await image.SaveAsync(memoryStream, new JpegEncoder());
        memoryStream.Position = 0; // нахуя это
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(memoryStream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "photo", "image.jpeg");
        using var httpClient = new HttpClient();
            
        var response = await httpClient.PostAsync(uploadUrl, content);
        if (!response.IsSuccessStatusCode) {
            L.E($"Error uploading image: {response.StatusCode} - {response.ReasonPhrase}");
            var errorContent = await response.Content.ReadAsStringAsync();
            L.E($"Response: {errorContent}");
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync();
        var photos = api.Photo.SaveMessagesPhoto(responseString);
        sw.Stop();
        L.M($"Photo uploaded to VK. It took {sw.ElapsedMilliseconds} ms.");
        return photos;
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
            L.M($"Found meme URL: {memeUrl}");

            // Get the server for uploading photos
            var uploadServer = api.Photo.GetMessagesUploadServer((long)groupId).UploadUrl;
            L.M($"Upload URL: {uploadServer}");

            try
            {
                // Download meme image
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(memeUrl);
                response.EnsureSuccessStatusCode();
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                using var memoryStream = new MemoryStream(imageBytes);
                using var image = Image.Load<Rgba32>(memoryStream);

                var photos = await UploadImageToVk(api, image, groupId);
                if (photos == null) return;

                string text = await MessageProcessor.KeepUpConversation();

                // Send the saved meme image in a message
                api.Messages.Send(new MessagesSendParams
                {
                    RandomId = _random.Next(),
                    PeerId = message.PeerId.Value,
                    ReplyTo = message.Id,
                    Attachments = photos,
                    Message = text
                });

                L.M("Meme sent to user.");
            }
            catch (Exception ex)
            {
                L.E($"Exception in HandleMemeCommand: {ex.Message}");
                L.E($"Stack Trace: {ex.StackTrace}");
            }
        }
        else
        {
            L.M("No meme found.");
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
        L.M($"Requesting Danbooru with tags: {tags}");

        Console.WriteLine(tags);

        var randomPost = await ServiceEndpoint.SafebooruApi.GetRandomPostAsync(tags);

        if (randomPost != null)
        {
            string imageUrl = randomPost.FileUrl;
            L.M($"Found image URL: {imageUrl}");

            var uploadServer = api.Photo.GetMessagesUploadServer((long)groupId).UploadUrl;
            L.M($"Upload URL: {uploadServer}");

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
                L.M("Anime image uploaded to VK.");

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

                List<MessageKeyboardButton> buttonsRow1 = [b];

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

                L.M("Anime image sent to user.");
            }
            catch (Exception ex)
            {
                L.M($"Exception in HandleAnimeCommand: {ex.Message}");
                L.M($"Stack Trace: {ex.StackTrace}");
            }
        }
        else
        {
            L.M("No anime image found.");
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
        L.M($"Requesting Danbooru with tags: {tags}");

        Services.Post? randomPost = await ServiceEndpoint.DanbooruApi.RandomImageAsync(onForbriddenTag, tags);

        if (isForb)
            return;

        if (randomPost != null)
        {
            string imageUrl = randomPost.FileUrl;
            L.M($"Found image URL: {imageUrl}");

            var uploadServer = api.Photo.GetMessagesUploadServer((long)groupId).UploadUrl;
            L.M($"Upload URL: {uploadServer}");

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
                L.M("Anime image uploaded to VK.");

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

                L.M("Anime image sent to user.");
            }
            catch (Exception ex)
            {
                L.M($"Exception in HandleAnimeCommand: {ex.Message}");
                L.M($"Stack Trace: {ex.StackTrace}");
            }
        }
        else
        {
            L.M("No anime image found.");
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

    private async void HandleSearchCommand(VkApi api, Message message, ulong groupId, string location) {
        var (image, foundLocation) = await ServiceEndpoint.Map.Search(location);

        if (image == null) {
            api.Messages.Send(new MessagesSendParams {
                RandomId = _random.Next(),
                PeerId = message.PeerId.Value,
                ReplyTo = message.Id,
                Message = $"Не удалось узнать где {location}!"
            });
            return;
        }

        var photos = await UploadImageToVk(api, image, groupId);
        if (photos == null) return;

        var text = await MessageProcessor.KeepUpConversation();

        api.Messages.Send(new MessagesSendParams {
            RandomId = _random.Next(),
            PeerId = message.PeerId.Value,
            ReplyTo = message.Id,
            Attachments = photos,
            Message = $"{location} {text} \n{foundLocation.Item1}\n{foundLocation.Item2}",
            //Lat = long.Parse(output.Item2.lat),
            //Longitude = long.Parse(output.Item2.lon)
        });
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
        L.M($"Received Python code: {pythonCode}");

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
            L.M("Python code executed successfully.");
        }
        catch (Exception ex)
        {
            SendResponse(api, message.PeerId.Value, "Ошибка при выполнении кода: " + ex.Message);
            L.M($"Error executing Python code: {ex.Message}");
        }
    }
    
    private static async Task<Image<Rgba32>?> FindImageInMessage(Message message) {

        var attachments = message.Attachments;
        if (attachments is { Count: > 0 } && attachments[0].Instance is Photo photo) {
            var largestPhoto = photo.Sizes?.OrderByDescending(s => s.Width * s.Height).FirstOrDefault();
            var photoUrl = largestPhoto?.Url?.AbsoluteUri;
            if (photoUrl == null) return null;
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(photoUrl);
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            using var ms = new MemoryStream(imageBytes);
            Image<Rgba32> originalImage;

            try {
                originalImage = Image.Load<Rgba32>(ms);
            } catch (Exception e) {
                L.M($"Error loading image: {e.Message}");
                return null;
            }

            return originalImage;
        }

        return message.ReplyMessage != null ? await FindImageInMessage(message.ReplyMessage) : null;
    }

    public async Task HandleFuneralCommand(VkApi api, Message message, ulong groupId) {
        var sourceImage = await FindImageInMessage(message);
        if (sourceImage == null) {
            api.Messages.Send(new MessagesSendParams {
                Message = "Некого хоронить!",
                RandomId = _random.Next(),
                PeerId = message.PeerId.Value
            });
            return;
        }

        var processedImage = await ImageProcessor.Funeral(sourceImage);

        var photos = await UploadImageToVk(api, processedImage, groupId);
        if (photos == null) return;

        api.Messages.Send(new MessagesSendParams {
            Message = "RIP🥀",
            RandomId = _random.Next(),
            PeerId = message.PeerId.Value,
            // ReplyTo = message.Id,
            Attachments = photos
        });

        L.M("Processed photo sent to user.");
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

        L.M("Starting chaos...");

        try
        {
            var members = await api.Messages.GetConversationMembersAsync(message.PeerId.Value);
            var randomMember = members.Profiles.OrderBy(x => Guid.NewGuid()).First();
            long victimId = randomMember.Id;

            var randomMember2 = members.Profiles.OrderBy(x => Guid.NewGuid()).First();

            string task = GenerateChaosTask($"[id{randomMember2.Id}|{randomMember2.FirstName} {randomMember2.LastName}]");

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

            L.M($"Chaos task assigned to {randomMember.FirstName}: {task}");
        }
        catch (Exception ex)
        {
            SendResponse(api, message.PeerId.Value, "Ошибка хаоса: " + ex.Message);
            L.M($"Error in chaos: {ex.Message}");
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
        $"спонсировать побег в лес",
        $"выебать {name} в жопу с разбега",
        $"трахнуть {name} до потери пульса",
        $"засунуть {name} голову в унитаз и смыть",
        $"сломать {name} нос об стену",
        $"выбить {name} зубы кувалдой",
        $"раздавить {name} яйца прессом",
        $"заставить {name} спорить с зеркалом до слез",
        $"сказать {name}, что голоса в голове хотят пиццу",
        $"заставить {name} танцевать с воображаемой бабкой",
        $"написать на лбу {name} \"шиза внутри\" и отправить в магазин",
        $"убедить {name}, что его кот — агент ФСБ",
        $"заставить {name} шептать \"я нормальный\" в подушку всю ночь",
        $"подарить {name} пустую коробку как \"лекарство от голосов\"",
        $"сказать {name}, что его тень хочет его задушить",
        $"заставить {name} искать Wi-Fi в лесу от деревьев",
        $"убедить {name}, что он застрял в симуляции без выхода",
        $"заставить {name} петь колыбельную своему отражению",
        $"сказать {name}, что его мозг сбежал через уши",
        $"заставить {name} обнимать мусорку и называть ее мамой",
        $"убедить {name}, что дождь — это слезы его второго я",
        $"заставить {name} искать свою душу в унитазе",
        $"сказать {name}, что он умер, но не заметил",
        $"заставить {name} писать письма своему выдуманному другу в стену",
        $"убедить {name}, что он — картошка в прошлой жизни",
        $"заставить {name} кричать \"где мой разум\" в пустую комнату",
        $"сказать {name}, что его ноги — шпионы и следят за ним",
        $"заставить {name} рисовать круги и шептать \"это мой дом\"",
        $"убедить {name}, что лампа в комнате — его босс",
        $"заставить {name} носить носок на руке как вторую личность",
        $"сказать {name}, что его голоса в голове устраивают забастовку",
        $"заставить {name} искать таблетки в миске с макаронами",
        $"убедить {name}, что он видит мир в инверсии",
        $"заставить {name} гладить воздух и называть его псом"

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

public static class MessagesExtensions
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
