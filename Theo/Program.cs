using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Theo
{
    class Program
    {
        static string[] lines;
        static int insults = 0;
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                string token = System.IO.File.ReadAllText(args[0]);
                lines = System.IO.File.ReadAllLines(args[1]);
                foreach (string s in lines)
                {
                    ++insults;
                }
                while (true)
                {
                    try
                    {
                        runBotAsync(token).Wait();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Catched exception: " + e.Message + "\n\nRebooting Bot");
                    }
                }
            }
            Console.WriteLine("Invalid arguments");
            Console.ReadLine();
        }

        static async System.Threading.Tasks.Task runBotAsync(string token)
        {
            Random ran = new Random();
            var botClient = new TelegramBotClient(token);
            var me = await botClient.GetMe();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            };
            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token);

            Console.WriteLine($"@{me.Username} is fully started");
            Console.ReadLine();

            // Send cancellation request to stop bot
            cts.Cancel();

            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                // Only process Message updates: https://core.telegram.org/bots/api#message
                if (update.Type != UpdateType.Message)
                    return;

                // Only process text messages
                if (update.Message!.Type != MessageType.Text)
                    return;

                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text;
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
                        text: "Shinnofag\n\nhttps://www.youtube.com/watch?v=5Vf1G3vUJ5g&ab_channel=Jespoke",
                        disableNotification: true,
                        replyParameters: ReplyParams,
                        cancellationToken: cancellationToken);
                }
                else if (messageText.Contains($"@{me.Username}") && (update.Message.Chat.Type == ChatType.Group || update.Message.Chat.Type == ChatType.Supergroup))
                {
                    if (update.Message.ReplyToMessage != null)
                    {
                        ReplyParams.MessageId = update.Message.ReplyToMessage.MessageId;
                        Message sentMessage = await botClient.SendMessage(
                            chatId: chatId,
                            text: lines[ran.Next(insults)],
                            disableNotification: true,
                            replyParameters: ReplyParams,
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Message sentMessage = await botClient.SendMessage(
                                chatId: chatId,
                                text: lines[ran.Next(insults)],
                                disableNotification: true,
                                cancellationToken: cancellationToken);
                    }
                }
                else if (update.Message.Chat.Type == ChatType.Private)
                {
                    Message sentMessage = await botClient.SendMessage(
                            chatId: chatId,
                            text: lines[ran.Next(insults)],
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