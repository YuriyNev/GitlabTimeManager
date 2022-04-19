using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using TelegramSender;
using Catel.IoC;
using GitLabTimeManager.Services;
using GitLabTimeManagerCore.Services;

public static class Program
{
    private static TelegramBotClient? Bot;

    public static async Task Main()
    {
        BuildContainer();

        Bot = new TelegramBotClient(Configuration.BotToken);

        User me = await Bot.GetMeAsync();
        Console.Title = me.Username ?? "My awesome Bot";

        using var cts = new CancellationTokenSource();

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        ReceiverOptions receiverOptions = new() { AllowedUpdates = { } };
        Bot.StartReceiving(Handlers.HandleUpdateAsync,
            Handlers.HandleErrorAsync,
            receiverOptions,
            cts.Token);

        var reportService = ServiceLocator.Default.ResolveType<IReportSenderService>();
        await reportService.RunAsync(Bot, cts.Token);

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        // Send cancellation request to stop bot
        cts.Cancel();
    }

    private static void BuildContainer()
    {
        var serviceLocator = IoCConfiguration.DefaultServiceLocator;
        serviceLocator.MissingType += ServiceLocator_MissingType;
#if !DEBUG
            //serviceLocator.RegisterTypeAndInstantiate<ExceptionWatcher>();
#endif
        var calendarService = new WorkingCalendar();
        var _ = calendarService.InitializeAsync();

        serviceLocator.RegisterType<IReportSenderService, ReportSenderService>();
        serviceLocator.RegisterType<ISourceControl, SourceControl>();
        serviceLocator.RegisterType<ILabelService, LabelProcessor>();
        serviceLocator.RegisterInstance<ICalendar>(calendarService);
        serviceLocator.RegisterType<IDataRequestService, DataRequestService>();
        serviceLocator.RegisterType<IProfileService, ProfileService>();
        serviceLocator.RegisterType<IUserProfile, UserProfile>();
        serviceLocator.RegisterType<IHttpService, HttpService>();
        serviceLocator.RegisterType<IReportProvider, ReportProvider>();
        serviceLocator.RegisterType<IUserService, UserService>();
        serviceLocator.RegisterType<IStorageService, StorageService>();
    }

    private static void ServiceLocator_MissingType(object sender, MissingTypeEventArgs e)
    {
        Debug.WriteLine(e.Tag);
    }
}