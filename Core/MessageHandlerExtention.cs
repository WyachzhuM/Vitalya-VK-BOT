using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using vkbot_vitalya.Config;
using vkbot_vitalya.Services;
using VkNet;
using VkNet.Model;
using vkbot_vitalya.Services.Generators.TextGeneration;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using vkbot_vitalya.Core;
using vkbot_vitalya.Services.Generators;
using Image = SixLabors.ImageSharp.Image;

namespace vkbot_vitalya;

/// <summary>
/// Extention to handlers
/// </summary>
public partial class MessageHandler {
    private Dictionary<long, int> chaosScores = new Dictionary<long, int>(); // Счёт хаоса

    private async Task HandlePhotoCommand(Message message, string command) {
        var image = await FindImageInMessage(message);
        if (image == null) {
            L.W("Tried to handle photo command, but photo not found.");
            return;
        }

        switch (command) {
            case "break":
                L.I("Command 'Break' recognized.");
                HandleImageCommand(message, image, Processor.BreakImage);
                break;
            case "liquidate":
                L.I("Command 'Liquidate' recognized.");
                HandleImageCommand(message, image, Processor.LiquidateImage);
                break;
            case "compress":
                L.I("Command 'Compress' recognized.");
                HandleImageCommand(message, image, Processor.CompressImage);
                break;
            case "add_text":
                L.I("Command 'AddText' recognized.");
                HandleImageCommand(message, image, Processor.AddTextImageCommand);
                break;
            case "funeral":
                L.I("Command 'Funeral' recognized.");
                await HandleFuneralCommand(message);
                return;
            default:
                L.W("Tried to handle photo command, but command doesn't need a photo. Generating random message.");
                var responseMessage = await MessageProcessor.KeepUpConversation();
                Answer(message.PeerId!.Value, responseMessage);
                break;
        }
    }

    private async void HandleImageCommand(Message message, Image<Rgba32> originalImage,
        Func<Image<Rgba32>, Image<Rgba32>> imageProcessor) {
        L.I("Handling image command...");

        try {
            Image<Rgba32> processedImage;
            var sw = new Stopwatch();
            try {
                sw.Start();
                processedImage = imageProcessor(originalImage);
                sw.Stop();
                L.I($"Image processing took {sw.ElapsedMilliseconds} ms");
            } catch (Exception e) {
                L.E($"Error processing image: {e.Message}");
                return;
            }

            var photo = await _vk.UploadImage(processedImage);
            if (photo == null) {
                return;
            }

            // Send the saved photo in a message
            _vk.Api.Messages.Send(new MessagesSendParams {
                RandomId = Rand.Next(),
                PeerId = message.PeerId!.Value,
                ReplyTo = message.Id,
                Attachments = photo
            });

            L.I("Processed photo sent to user.");
        } catch (Exception e) {
            L.E("Failed to handle command", e);
        }
    }

    private async void HandleMemeCommand(Message message, string keywords) {
        Meme? meme;
        if (string.IsNullOrEmpty(keywords)) {
            // Генерируем случайный мем, если ключевые слова отсутствуют
            meme = await ServiceEndpoint.MemeGen.RandomMeme(string.Empty, MemeType.Image);
        } else {
            // Генерируем мем по введенным ключевым словам
            meme = await ServiceEndpoint.MemeGen.RandomMeme(keywords, MemeType.Image);
        }

        if (meme != null) {
            var memeUrl = meme.Url;
            L.I($"Found meme URL: {memeUrl}");

            try {
                var photo = await _vk.UploadImageFrom(memeUrl, new HttpClient());
                if (photo == null) {
                    return;
                }

                var text = await MessageProcessor.KeepUpConversation();

                // Send the saved meme image in a message
                _vk.Api.Messages.Send(new MessagesSendParams {
                    RandomId = Rand.Next(),
                    PeerId = message.PeerId!.Value,
                    ReplyTo = message.Id,
                    Attachments = photo,
                    Message = text
                });

                L.I("Meme sent to user.");
            } catch (Exception ex) {
                L.E($"Exception in HandleMemeCommand: {ex.Message}");
                L.E($"Stack Trace: {ex.StackTrace}");
            }
        } else {
            L.I("No meme found.");
            Answer(message.PeerId!.Value, "Извините, не удалось найти мемы по заданным ключевым словам.");
        }
    }

    private async void HandleWeatherCommand(Message message, string cityName) {
        var weatherResponse = await ServiceEndpoint.WeatherService.GetWeatherAsync(cityName);
        if (weatherResponse != null) {
            var weatherMessage =
                $"Погода в {weatherResponse.Name}:\n" +
                $"Температура: {weatherResponse.Main?.Temp ?? 0}°C\n" +
                $"Ощущается как: {weatherResponse.Main?.FeelsLike ?? 0}°C\n" +
                $"Описание: {weatherResponse.Weather?.FirstOrDefault()?.Description}\n" +
                $"Ветер: {weatherResponse.Wind?.Speed ?? 0} м/с\n" +
                $"Влажность: {weatherResponse.Main?.Humidity ?? 0}%";

            Answer(message.PeerId!.Value, weatherMessage, message.Id);
        } else {
            Answer(message.PeerId!.Value, "Извините, не удалось получить информацию о погоде.", message.Id);
        }
    }

    private async void HandleAnimeCommand(Message message, string _tags = "") {
        var commandText = message.Text.ToLower().Trim();
        string[] commandParts = commandText.Split(new[] { ' ' }, 3);

        var tags = "";

        if (_tags == "") {
            if (commandParts.Length >= 3) tags = commandParts[2].Trim();
        } else {
            tags = _tags;
        }

        // Если теги не указаны, будут использоваться отрицательные теги по умолчанию
        L.I($"Requesting Danbooru with tags: {tags}");

        var randomPost = await ServiceEndpoint.SafebooruApi.GetRandomPostAsync(tags);

        if (randomPost != null) {
            var imageUrl = randomPost.FileUrl;
            L.I($"Found image URL: {imageUrl}");

            try {
                var photo = await _vk.UploadImageFrom(imageUrl, ServiceEndpoint.DanbooruApi.Client);
                if (photo == null) return;

                List<string> variableLabel = new List<string>() {
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

                var random = new Random();

                var b = new MessageKeyboardButton {
                    Action = new MessageKeyboardButtonAction {
                        Type = VkNet.Enums.StringEnums.KeyboardButtonActionType.Text,
                        Label = variableLabel[random.Next(variableLabel.Count)],
                        Payload = JsonConvert.SerializeObject(new { command = "anim", _tags = tags })
                    }
                };

                List<MessageKeyboardButton> buttonsRow1 = [b];

                var values = new List<List<MessageKeyboardButton>> { buttonsRow1 };

                var keyboard = new MessageKeyboard {
                    Buttons = values,
                    Inline = true
                };

                _vk.Api.Messages.Send(new MessagesSendParams {
                    RandomId = Rand.Next(),
                    PeerId = message.PeerId!.Value,
                    Attachments = photo,
                    ReplyTo = message.Id,
                    Keyboard = keyboard
                });

                L.I("Anime image sent to user.");
            } catch (Exception ex) {
                L.I($"Exception in HandleAnimeCommand: {ex.Message}");
                L.I($"Stack Trace: {ex.StackTrace}");
            }
        } else {
            L.I("No anime image found.");
            Answer(message.PeerId!.Value, "Извините, не удалось найти изображение аниме.");
        }
    }

    private async void HandleHCommand(Message message, string _tags = "") {
        var isForb = false;

        var onForbriddenTag = () => { isForb = true; };

        var commandText = message.Text.ToLower().Trim();
        string[] commandParts = commandText.Split(new[] { ' ' }, 3);

        var tags = "";

        if (_tags == "") {
            if (commandParts.Length >= 3) tags = commandParts[2].Trim();
        } else {
            tags = _tags;
        }

        // Если теги не указаны, будут использоваться отрицательные теги по умолчанию
        L.I($"Requesting Danbooru with tags: {tags}");

        var imageUrl = await ServiceEndpoint.DanbooruApi.RandomImageAsync(onForbriddenTag, tags);

        if (isForb && !Program.IgnoreTagsBlacklist) {
            L.I("Found forbidden tags, aborting");
            return;
        }

        if (imageUrl != null) {
            L.I($"Found image URL: {imageUrl}");

            try {
                var photo = await _vk.UploadImageFrom(imageUrl, ServiceEndpoint.DanbooruApi.Client);
                if (photo == null)
                    return;

                var b = new MessageKeyboardButton {
                    Action = new MessageKeyboardButtonAction {
                        Type = VkNet.Enums.StringEnums.KeyboardButtonActionType.Text,
                        Label = "Еще!",
                        Payload = JsonConvert.SerializeObject(new { command = "hen", _tags = tags })
                    }
                };

                List<MessageKeyboardButton> buttonsRow1 = new List<MessageKeyboardButton> { b };

                var values = new List<List<MessageKeyboardButton>> { buttonsRow1 };

                var keyboard = new MessageKeyboard {
                    Buttons = values,
                    Inline = true
                };

                _vk.Api.Messages.Send(new MessagesSendParams {
                    RandomId = Rand.Next(),
                    PeerId = message.PeerId!.Value,
                    Attachments = photo,
                    ReplyTo = message.Id,
                    Keyboard = keyboard
                });

                L.I("Anime image sent to user.");
            } catch (Exception ex) {
                L.I($"Exception in HandleAnimeCommand: {ex.Message}");
                L.I($"Stack Trace: {ex.StackTrace}");
            }
        } else {
            L.I("No anime image found.");
            Answer(message.PeerId!.Value, "Извините, не удалось найти изображение аниме.");
        }
    }

    private async void HandleHelpCommand(Message message) {
        var help = File.ReadAllText("./config.json");

        await _vk.Api.Messages.SendAsync(new MessagesSendParams {
            RandomId = Rand.Next(),
            PeerId = message.PeerId!.Value,
            ReplyTo = message.Id,
            Message = help
        });
    }

    private async void HandleSearchCommand(Message message, string location) {
        var (image, foundLocation) = await ServiceEndpoint.Map.Search(location);

        if (image == null) {
            _vk.Api.Messages.Send(new MessagesSendParams {
                RandomId = Rand.Next(),
                PeerId = message.PeerId!.Value,
                ReplyTo = message.Id,
                Message = $"Не удалось узнать где {location}!"
            });
            return;
        }

        var photos = await _vk.UploadImage(image);
        if (photos == null) return;

        var text = await MessageProcessor.KeepUpConversation();

        _vk.Api.Messages.Send(new MessagesSendParams {
            RandomId = Rand.Next(),
            PeerId = message.PeerId!.Value,
            ReplyTo = message.Id,
            Attachments = photos,
            Message = $"{location} {text} \n{foundLocation.Item1}\n{foundLocation.Item2}",
            //Lat = long.Parse(output.Item2.lat),
            //Longitude = long.Parse(output.Item2.lon)
        });
    }

    private async void HandlePythonCommand(Message message) {
        var commandText = message.Text.Trim();
        string[] commandParts = commandText.Split(new[] { ' ' }, 3);

        if (commandParts.Length < 3 || !commandParts[1].Equals("py", StringComparison.OrdinalIgnoreCase)) {
            Answer(message.PeerId!.Value,
                "Пожалуйста, укажи Python-код после команды, например: `v py print('Hello')`");
            return;
        }

        var pythonCode = commandParts[2].Trim();
        L.I($"Received Python code: {pythonCode}");

        var pythonCodeLower = pythonCode.ToLower();
        if (Regex.IsMatch(pythonCodeLower, @"(os|sys|subprocess|import|exec|eval|\bimp\b|\bort\b)")) {
            Answer(message.PeerId!.Value,
                "Использование системных модулей, импорта или опасных функций запрещено.");
            return;
        }

        if (pythonCode.Length > 1000) {
            Answer(message.PeerId!.Value, "Слишком длинный код (максимум 1000 символов).");
            return;
        }

        try {
            var output = await ExecutePythonCode(pythonCode);
            Answer(message.PeerId!.Value, output.Length > 0 ? output : "Код выполнен, но вывода нет.");
            L.I("Python code executed successfully.");
        } catch (Exception ex) {
            Answer(message.PeerId!.Value, "Ошибка при выполнении кода: " + ex.Message);
            L.I($"Error executing Python code: {ex.Message}");
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
                L.I($"Error loading image: {e.Message}");
                return null;
            }

            return originalImage;
        }

        return message.ReplyMessage != null ? await FindImageInMessage(message.ReplyMessage) : null;
    }

    public async Task HandleFuneralCommand(Message message) {
        var sourceImage = await FindImageInMessage(message);
        if (sourceImage == null) {
            _vk.Api.Messages.Send(new MessagesSendParams {
                Message = "Некого хоронить!",
                RandomId = Rand.Next(),
                PeerId = message.PeerId!.Value
            });
            return;
        }

        var processedImage = await ImageProcessor.Funeral(sourceImage);

        var photos = await _vk.UploadImage(processedImage);
        if (photos == null) return;

        _vk.Api.Messages.Send(new MessagesSendParams {
            Message = "RIP🥀",
            RandomId = Rand.Next(),
            PeerId = message.PeerId!.Value,
            // ReplyTo = message.Id,
            Attachments = photos
        });

        L.I("Processed photo sent to user.");
    }

    private async Task<string> ExecutePythonCode(string code) {
        var pythonPath = "python";

        var arguments = $"-c \"{code.Replace("\"", "\\\"")}\"";

        var startInfo = new ProcessStartInfo {
            FileName = pythonPath,
            Arguments = arguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (var process = new Process { StartInfo = startInfo }) {
            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (sender, args) => {
                if (args.Data != null) output.AppendLine(args.Data);
            };
            process.ErrorDataReceived += (sender, args) => {
                if (args.Data != null) error.AppendLine(args.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var completed = await Task.Run(() => process.WaitForExit(5000));

            if (!completed) {
                process.Kill();
                throw new Exception("Код выполнялся слишком долго (более 5 секунд).");
            }

            process.WaitForExit();

            if (error.Length > 0) {
                return $"Ошибка: {error.ToString().Trim()}";
            }

            return output.ToString().Trim();
        }
    }

    private async void HandleChaosCommand(Message message) {
        var commandText = message.Text.Trim();
        string[] commandParts = commandText.Split(new[] { ' ' }, 2); // "v chaos"

        if (commandParts.Length < 2 || !commandParts[1].Equals("chaos", StringComparison.OrdinalIgnoreCase)) {
            Answer(message.PeerId!.Value, "Просто напиши: `v chaos`");
            return;
        }

        L.I("Starting chaos...");

        try {
            var members = await _vk.Api.Messages.GetConversationMembersAsync(message.PeerId!.Value);
            var victim = members.Profiles.OrderBy(x => Guid.NewGuid()).First();

            var randomMember2 = members.Profiles.OrderBy(x => Guid.NewGuid()).First();

            var task = GenerateChaosTask(Vk.PingUser(randomMember2));

            var buttons = new List<MessageKeyboardButton> {
                new MessageKeyboardButton {
                    Action = new MessageKeyboardButtonAction {
                        Type = VkNet.Enums.StringEnums.KeyboardButtonActionType.Text,
                        Label = "Выполнено",
                        Payload = JsonConvert.SerializeObject(new { command = "chaos_done", victim = victim.Id })
                    }
                },
                new MessageKeyboardButton {
                    Action = new MessageKeyboardButtonAction {
                        Type = VkNet.Enums.StringEnums.KeyboardButtonActionType.Text,
                        Label = "Провал",
                        Payload = JsonConvert.SerializeObject(new { command = "chaos_fail", victim = victim.Id })
                    }
                }
            };

            var keyboard = new MessageKeyboard {
                Buttons = new List<List<MessageKeyboardButton>> { buttons },
                Inline = true
            };

            await _vk.Api.Messages.SendAsync(new MessagesSendParams {
                RandomId = new Random().Next(),
                PeerId = message.PeerId!.Value,
                Message = $"🔥 Хаос начинается! Жертва: {Vk.PingUser(victim)}\nЗадание: {task}\nГолосуйте!",
                Keyboard = keyboard
            });

            L.I($"Chaos task assigned to {victim.FirstName}: {task}");
        } catch (Exception ex) {
            Answer(message.PeerId!.Value, "Ошибка хаоса: " + ex.Message);
            L.I($"Error in chaos: {ex.Message}");
        }
    }

    private static string GenerateChaosTask(string name) {
        var random = new Random();
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

    private async void HandleSettingsCommand(Message message) {
        var chat = _saves.Chats.FirstOrDefault(c => c.PeerId == message.PeerId);

        if (chat == null) {
            Answer(message.PeerId!.Value, "Чат не найден.");
            return;
        }

        if (chat.Properties == null) {
            chat.Properties = new ChatProperties();
            _saves.Save(SavesFilePath);
        }

        var b1 = CreateToggleButton(chat.Properties.IsAnime, "anime", "Аниме");
        var b2 = CreateToggleButton(chat.Properties.IsHentai, "hentai", "Хентай");
        var b3 = CreateToggleButton(chat.Properties.IsImageProccestion, "image_processing", "Обработка изображений");
        var b4 = CreateToggleButton(chat.Properties.IsMeme, "meme", "Мемы");
        var b5 = CreateToggleButton(chat.Properties.IsWeather, "weather", "Погода");
        var b6 = CreateToggleButton(chat.Properties.IsLocation, "location", "Местоположение");

        var buttonsRow1 = new List<MessageKeyboardButton> { b1, b2, b3 };
        List<MessageKeyboardButton> buttonsRow2 = new List<MessageKeyboardButton> { b4, b5, b6 };

        var values = new List<List<MessageKeyboardButton>> { buttonsRow1, buttonsRow2 };

        var keyboard = new MessageKeyboard {
            Buttons = values,
            Inline = true
        };

        await _vk.Api.Messages.SendAsync(new MessagesSendParams {
            RandomId = Rand.Next(),
            PeerId = message.PeerId!.Value,
            Message = "Настройки чата",
            Keyboard = keyboard
        });
    }

    // Метод для создания переключающих кнопок
    private MessageKeyboardButton CreateToggleButton(bool isEnabled, string command, string label) {
        return new MessageKeyboardButton {
            Action = new MessageKeyboardButtonAction {
                Type = VkNet.Enums.StringEnums.KeyboardButtonActionType.Text,
                Label = isEnabled ? $"✅ {label}" : $"🚫 {label}",
                Payload = JsonConvert.SerializeObject(new { command = $"toggle_{command}" })
            }
        };
    }

    private async Task<bool> IsUserAdmin(long chatId, long userId) {
        try {
            var members = await _vk.Api.Messages.GetConversationMembersAsync(chatId, null, Auth.Instance.GroupId);

            var admins = members.Items.Where(x => x.IsAdmin).Select(x => x.MemberId);
            return admins.Contains(userId);
        } catch (VkNet.Exception.ConversationAccessDeniedException ex) {
            L.I($"Access denied to chat {chatId}: {ex.Message}");
            return false;
        }
    }

    /// Payload - данные, переданные в сообщении при нажатии кнопки
    public async void HandlePayload(Message message) {
        dynamic payload = JsonConvert.DeserializeObject(message.Payload);
        string command = payload.command;
        var peerId = message.PeerId!.Value;

        switch (command) {
            case "anim":
                string tags = payload._tags;
                HandleAnimeCommand(message, tags);
                return;
            case "hen":
                string tagshen = payload._tags;
                HandleHCommand(message, tagshen);
                return;

            case "chaos_done":
                long doneVictim = payload.victim;
                chaosScores[doneVictim] = chaosScores.GetValueOrDefault(doneVictim) + 1;
                Answer(message.PeerId!.Value,
                    $"Задание выполнено! [id{doneVictim}|Жертва] получает +1 хаос-очко. Текущий счёт: {chaosScores[doneVictim]}");
                return;

            case "chaos_fail":
                long failVictim = payload.victim;
                chaosScores[failVictim] = chaosScores.GetValueOrDefault(failVictim) - 1;
                Answer(message.PeerId!.Value,
                    $"Задание провалено! [id{failVictim}|Жертва] теряет 1 хаос-очко. Текущий счёт: {chaosScores[failVictim]}");
                return;
        }

        var userId = message.FromId.Value;

        if (!await IsUserAdmin(message.PeerId!.Value, userId)) {
            Answer(message.PeerId!.Value, "Только админы могут изменять настройки.");
            return;
        }

        var chat = _saves.Chats.FirstOrDefault(c => c.PeerId == peerId);
        if (chat == null) {
            Answer(message.PeerId!.Value, "Чат не найден.");
            return;
        }

        if (chat.Properties == null) {
            chat.Properties = new ChatProperties();
            _saves.Save(SavesFilePath);
        }

        switch (command) {
            case "toggle_anime":
                chat.Properties.IsAnime = !chat.Properties.IsAnime;
                break;
            case "toggle_hentai":
                chat.Properties.IsHentai = !chat.Properties.IsHentai;
                break;
            case "toggle_image_processing":
                chat.Properties.IsImageProccestion = !chat.Properties.IsImageProccestion;
                break;
            case "toggle_meme":
                chat.Properties.IsMeme = !chat.Properties.IsMeme;
                break;
            case "toggle_weather":
                chat.Properties.IsWeather = !chat.Properties.IsWeather;
                break;
            case "toggle_location":
                chat.Properties.IsLocation = !chat.Properties.IsLocation;
                break;

            default:
                Answer(message.PeerId!.Value, "Неизвестная команда.");
                return;
        }

        _saves.Save(SavesFilePath);

        Answer(message.PeerId!.Value, "Настройки обновлены.");

        // Отправляем обновленную клавиатуру
        HandleSettingsCommand(message);
    }

    #endregion

    #region 💩

    private static string[] ЕБАНАЯХУЙНЯ = new string[12];

    static MessageHandler() {
        var i = 0;
        foreach (var name in Enum.GetNames(typeof(Vk.Declension))) {
            ЕБАНАЯХУЙНЯ[i] = "first_name_" + name.ToLower();
            ЕБАНАЯХУЙНЯ[i + 1] = "last_name_" + name.ToLower();
            i += 2;
        }
    }

    #endregion 💩

    private void HandleWhoCommand(UserRequest userRequest) {
        string[] prefixes = [
            "Уверен, что",
            "Определенно",
            "Твоя мама мне сказала, что",
            "В Библии написано, что",
            "Ящитаю, это",
            "На твоей жопе написано, что",
            "Аллах сказал, что",
            "По РЕН-ТВ рассказали, что",
            "В девятом классе изучали, что",
            "Всем известно, что",
            "Все и так знают:"
        ];
        if (Rand.NextSingle() < 0.1) {
            Answer(userRequest.Message, "А я откуда знаю?");
            return;
        }

        var text = userRequest.Keywords ?? string.Empty;
        // Я НЕНАВИЖУ NLP Я НЕНАВИЖУ NLP Я НЕНАВИЖУ NLP Я НЕНАВИЖУ NLP
        text = Regex.Replace(text, @"\bты\b", "я", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтебя\b", "меня", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bо тебе\b", "обо мне", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтебе\b", "мне", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтобой\b", "мной", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвой\b", "мой", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвоя\b", "моя", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвои\b", "мои", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвоим\b", "моим", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвоими\b", "моими", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвоих\b", "моих", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвоего\b", "моего", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвоей\b", "моей", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвоё\b", "моё", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвое\b", "мое", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвою\b", "мою", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвоем\b", "моем", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bтвоём\b", "моём", RegexOptions.IgnoreCase);
        text = text.Replace('?', '.');

        var users = _vk.Api.Messages.GetConversationMembers(userRequest.Message.PeerId!.Value, fields: ЕБАНАЯХУЙНЯ)
            .Profiles;
        var answerUser = users[Rand.Next(users.Count)];
        var prefix = prefixes[Rand.Next(prefixes.Length)];
        var decl = userRequest.Alias switch {
            "кого" => Vk.Declension.Gen,
            "кому" => Vk.Declension.Dat,
            "кем" => Vk.Declension.Abl,
            "о ком" => Vk.Declension.Ins,
            _ => Vk.Declension.Nom
        };
        Answer(userRequest.Message, $"{prefix} {Vk.PingUser(answerUser, decl: decl)} {text}");
    }

    private static async Task<(string url, string? text)> GetWikiPage(string title) {
        using var client = new HttpClient();
        var url = $"https://ru.wikipedia.org/w/api.php?action=query&format=json&prop=extracts" +
                  $"&exintro=true&explaintext=true&titles={Uri.EscapeDataString(title)}";


        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode) {
            return ($"ru.wikipedia.org/w/index.php?title={title}&action=edit", null);
        }

        var jsonString = await response.Content.ReadAsStringAsync();

        var data = JObject.Parse(jsonString);

        var pages = (JObject)data["query"]!["pages"]!;
        foreach (var page in pages.Properties()) {
            var text = page.Value["extract"];

            if (text != null) {
                return ($"\ud83d\udcc4 Источник: ru.wikipedia.org/wiki/{page.Value["title"]}", text.ToString());
            }
        }

        return ($"ru.wikipedia.org/w/index.php?title={title}&action=edit", null);
    }

    private async Task HandleWikiCommand(Message message, string title) {
        var (url, text) = await GetWikiPage(title);
        if (text == null) {
            Answer(message.PeerId!.Value, $"Нет такой статьи, напиши сам: {url}");
            return;
        }

        Answer(message.PeerId!.Value, $"{text.Trim()}\n\n{url}");
    }
}

public static class MessagesExtentions {
    public static void Out(this Message message) {
        L.I($"New message in chat {message.PeerId} from {message.FromId} (id: {message.Id})");
    }
}