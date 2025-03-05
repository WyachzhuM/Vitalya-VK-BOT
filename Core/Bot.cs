﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using SixLabors.ImageSharp.Formats.Jpeg;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using vkbot_vitalya.Mappers;
using vkbot_vitalya.Services.Generators;
using VkNet;
using VkNet.Model;
using Image = SixLabors.ImageSharp.Image;
using User = vkbot_vitalya.Config.User;

public class Bot {
    public readonly VkApi Api = new VkApi();
    public readonly Saves Saves = Saves.Load();

    /// Падежи
    /// Nom - Именительный
    /// Gen - Родительный
    /// Dat - Дательный
    /// Acc - Винительный
    /// Abl - Творительный
    /// Ins - Предложный
    public enum Declension {
        Nom,
        Gen,
        Dat,
        Acc,
        Abl,
        Ins
    }

    public static string GetName(User user, Declension? declension = Declension.Nom) {
        var name = declension switch {
            Declension.Gen => user.FirstNameGen + " " + user.LastNameGen,
            Declension.Dat => user.FirstNameDat + " " + user.LastNameDat,
            Declension.Acc => user.FirstNameAcc + " " + user.LastNameAcc,
            Declension.Abl => user.FirstNameAbl + " " + user.LastNameAbl,
            Declension.Ins => user.FirstNameIns + " " + user.LastNameIns,
            _ => user.FirstName + " " + user.LastName
        };
        if (string.IsNullOrEmpty(name.Trim())) {
            return user.FirstName + " " + user.LastName;
        }

        return name;
    }

    public static string PingUser(User user, string? name = null, Declension? decl = null) {
        return $"[id{user.Id}|{name ?? GetName(user, decl)}]";
    }

    /// Upload new image to VK
    public async Task<ReadOnlyCollection<Photo>?> UploadImage(Image image) {
        var sw = new Stopwatch();
        sw.Start();
        var uploadUrl = Api.Photo.GetMessagesUploadServer((long)Auth.Instance.GroupId).UploadUrl;
        using var memoryStream = new MemoryStream();
        await image.SaveAsync(memoryStream, new JpegEncoder());
        memoryStream.Position = 0;
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
        var photos = Api.Photo.SaveMessagesPhoto(responseString);
        sw.Stop();
        L.I($"Photo uploaded to VK in {sw.ElapsedMilliseconds} ms");
        return photos;
    }

    /// Asynchronously upload image from URL
    public async Task<ReadOnlyCollection<Photo>?> UploadImageFrom(string imageUrl, HttpClient client) {
        var sw = new Stopwatch();
        sw.Start();
        var uploadUrl = Api.Photo.GetMessagesUploadServer((long)Auth.Instance.GroupId).UploadUrl;

        try {
            using var response = await client.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) {
                L.W($"Failed to download image from {imageUrl} (Status code: {response.StatusCode})");
                return null;
            }

            await using var originalImageStream = await response.Content.ReadAsStreamAsync();
            using var imageStream = new MemoryStream();
            await ImageProcessor.ResizeAndCompressImage(originalImageStream, imageStream);
            imageStream.Position = 0;
            using var content = new MultipartFormDataContent();

            var fileContent = new StreamContent(imageStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            content.Add(fileContent, "photo", "image.jpeg");

            using var httpClient = new HttpClient();
            var vkResponse = await httpClient.PostAsync(uploadUrl, content);
            if (!vkResponse.IsSuccessStatusCode) {
                L.W($"Failed to upload image to {uploadUrl} (Status code: {response.StatusCode})");
                return null;
            }

            var responseString = await vkResponse.Content.ReadAsStringAsync();
            var photo = Api.Photo.SaveMessagesPhoto(responseString);
            sw.Stop();
            L.I($"Photo copied to VK in {sw.ElapsedMilliseconds} ms");
            return photo;
        } catch (Exception e) {
            L.E($"Failed to download image from {imageUrl}", e);
            return null;
        }
    }

    public async Task<List<User>> GetChatMembers(long peerId) {
        if (Conf.Instance.AutoUpdateChats) {
            await UpdateChat(peerId);
        }

        var chat = Saves.Chats.FirstOrDefault(c => c.PeerId == peerId);
        if (chat != null)
            return chat.Users;
        await UpdateChat(peerId);
        return Saves.Chats.FirstOrDefault(c => c.PeerId == peerId)!.Users;
    }

    #region 💩

    private static readonly string[] NameFields = new string[12];

    static Bot() {
        var i = 0;
        foreach (var name in Enum.GetNames(typeof(Declension))) {
            NameFields[i] = "first_name_" + name.ToLower();
            NameFields[i + 1] = "last_name_" + name.ToLower();
            i += 2;
        }
    }

    #endregion 💩

    public async Task UpdateChat(long peerId) {
        var result = await Api.Messages.GetConversationMembersAsync(peerId, fields: NameFields);
        var vkUsers = result.Profiles;
        Saves.Chats.FirstOrDefault(c => c.PeerId == peerId)!.Users.Clear();
        foreach (var vkUser in vkUsers) {
            var user = UserMapper.Map<User>(vkUser);
            Saves.AddUserToChat(peerId, user);
        }

        Saves.Save();
    }
}