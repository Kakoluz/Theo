using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Theo
{
    partial class Program
    {
        static string[]? Lines;
        static int Insults;
        static readonly CancellationTokenSource TokenSource = new();
        static readonly SemaphoreSlim Semaphore = new(0, 1);

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: Theo <token file> <insults file>");
                return;
            }

            var tokenPath = Path.GetFullPath(args[0]);
            if (!System.IO.File.Exists(tokenPath))
            {
                Console.Error.WriteLine($"{tokenPath} file not found");
                return;
            }
            var insultsPath = Path.GetFullPath(args[1]);
            if (!System.IO.File.Exists(insultsPath))
            {
                Console.Error.WriteLine($"{insultsPath} file not found");
                return;
            }

            string token = System.IO.File.ReadAllText(tokenPath).Trim();
            Lines = System.IO.File.ReadAllLines(insultsPath);
            Insults = Lines.Length;
            Console.CancelKeyPress += CancelationRequest;
            try
            {
                _ = RunBotAsync(token);
                Semaphore.Wait(TokenSource.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine("Catched exception: " + e.Message + "\n\nRebooting Bot");
            }
        }

        private static void CancelationRequest(object? sender, ConsoleCancelEventArgs? eventArgs)
        {
            TokenSource.Cancel();
        }

        static async Task RunBotAsync(string token)
        {
            Random ran = new();
            var botClient = new TelegramBotClient(token);
            var me = await botClient.GetMe();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message] // receive message update types
            };
            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: TokenSource.Token);

            Console.WriteLine($"@{me.Username} is fully started");

            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                // Only process Message updates: https://core.telegram.org/bots/api#message
                if (update.Type != UpdateType.Message)
                    return;

                // Only process text messages
                if (update.Message!.Type != MessageType.Text)
                    return;

                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text ?? "";
                var messageLower = messageText.ToLower();
                var ReplyParams = new ReplyParameters()
                {
                    ChatId = chatId,
                    MessageId = update.Message.MessageId,
                };

                if (SinnohRegex().IsMatch(messageLower))
                {
                    Message sentMessage = await botClient.SendMessage(
                        chatId: chatId,
                        text: "Sinnohfag\n\nhttps://www.youtube.com/watch?v=5Vf1G3vUJ5g",
                        disableNotification: true,
                        replyParameters: ReplyParams,
                        cancellationToken: cancellationToken);
                    return;
                }

                else if (messageText.Contains($"@{me.Username}") && (update.Message.Chat.Type == ChatType.Group || update.Message.Chat.Type == ChatType.Supergroup))
                {
                    if (update.Message.ReplyToMessage != null)
                    {
                        ReplyParams.MessageId = update.Message.ReplyToMessage.MessageId;
                        Message sentMessage = await botClient.SendMessage(
                            chatId: chatId,
                            text: Lines![ran.Next(Insults)],
                            disableNotification: true,
                            replyParameters: ReplyParams,
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Message sentMessage = await botClient.SendMessage(
                                chatId: chatId,
                                text: Lines![ran.Next(Insults)],
                                disableNotification: true,
                                cancellationToken: cancellationToken);
                    }
                }
                else if (update.Message.Chat.Type == ChatType.Private)
                {
                    Message sentMessage = await botClient.SendMessage(
                            chatId: chatId,
                            text: Lines![ran.Next(Insults)],
                            disableNotification: true,
                            cancellationToken: cancellationToken);
                }

            }

            Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                var ErrorMessage = exception switch
                {
                    ApiRequestException apiRequestException
                        => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };

                Console.WriteLine(ErrorMessage);
                return Task.CompletedTask;
            }
        }

        [GeneratedRegex(@"\b[sS]h?in{1,2}oh\b")]
        private static partial Regex SinnohRegex();
    }
}