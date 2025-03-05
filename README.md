# vkbot_vitalya

охх даааа... чистый сишарп, в питон больше ни ногой

## Описание

**vkbot_vitalya** - это бот, который поддерживает команды для генерации текста и обработки фотографий, используя
библиотеку ImageSharp и VkNet для работы с VK API.

## Возможности

### Системные команды

- **Виталя /generate_sentences** - генерирует текст.
- **Виталя /echo** - проверка бота на работоспособность.

### Команды для обработки фото

- **Виталя сломай** - каша из пикселей.
- **Виталя ликвидируй** - обесцвечивает и добавляет надпись "ЛИКВИДИРОВАН".
- **Виталя сожми** - делает из фото пиксели, добавляет рамку, добавляет свой текст.
- **Виталя е##ни** - добавляет свой текст.
- **Виталя мем [текст пользовтаеля]** - ищет мем по тексту (англ. текст обязателен).
- **Виталя погода [название города]** - получение погоды на данный момент времени указаном городе.
- **Виталя аниме [теги (необязательно)]** - ищет аниме картинки.

## Требования

- **Ubuntu** (или другой Linux дистрибутив).
- **.NET SDK**.
- **libgdiplus** для работы с System.Drawing на Linux.
- **Установленный шрифт Arial** (или другой, если вы поменяете его в коде).

## Установка и Запуск на Ubuntu сервере

### step 1: Обновление системы

```sh
sudo apt update
sudo apt upgrade -y
```

## step 2: Установка .NET SDK

```sh
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-6.0
```

## step 3: Установка libgdiplus

```sh
sudo apt install libgdiplus
sudo ln -s libgdiplus.so /usr/lib64/libgdiplus.so
```

## step 4: Установка шрифтов

```sh
sudo apt install ttf-mscorefonts-installer
```

## step 5: Установка зависимостей (если нет)

```sh
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Drawing
dotnet add package SixLabors.Fonts

dotnet add package VkNet
```

## step 6: Сборка и запуск проекта

```sh
dotnet build
dotnet run
```

...по идее должно работать

- ах, да, файл ```config.json``` не забудь поставить, он должен выглядеть вот так:

```json
{
  "bot_name": "виталя",
  "response_probability": 0.2,
  "commands": {
    "generate_sentences": "/generate_sentences",
    "echo": "/echo",
    "break": "сломай",
    "liquidate": "ликвидируй",
    "compress": "сожми",
    "add_text": "ебани",
    "meme": "мем"
  }
}
```

## Помните:

- **в тебе мем**
- **Виталя валера**
- **жмых виталя, виталя без тебя**
