using Avalonia;
using Avalonia.ReactiveUI;
using System; 

namespace LogicSimulator
{
    public class Program
    {
        [STAThread] 
        public static void Main(string[] args) => BuildAvaloniaApp() 
            .StartWithClassicDesktopLifetime(args); //создает и запускает новый экземпляр приложения с использованием классической схемы жизненного цикла рабочего стола.

        public static AppBuilder BuildAvaloniaApp() 
            => AppBuilder.Configure<App>() 
                .UsePlatformDetect() 
                .LogToTrace() 
                .UseReactiveUI(); 
    }
}