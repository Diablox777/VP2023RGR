using Avalonia;
using Avalonia.ReactiveUI;
using System; 

namespace LogicSimulator
{
    public class Program
    {
        [STAThread] //однопоточный режим приложения
        public static void Main(string[] args) => BuildAvaloniaApp() //главная функция приложения.
            .StartWithClassicDesktopLifetime(args); //запуск приложения с использованием метода StartWithClassicDesktopLifetime,
                                                    //который создает и запускает новый экземпляр приложения с использованием классической схемы жизненного цикла рабочего стола.

        public static AppBuilder BuildAvaloniaApp() //метод для конфигурации Avalonia-приложения.
            => AppBuilder.Configure<App>() //указание, что конфигурация настраивает класс App.
                .UsePlatformDetect() //автоматическое определение платформы.
                .LogToTrace() //логи пишутся в трассировочную консоль
                .UseReactiveUI(); //подключение ReactiveUI для обеспечения реакции на изменения данных и интерактивности.
    }
}