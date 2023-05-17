using Avalonia.Controls.Presenters;
using Avalonia.Controls;
using ReactiveUI;
using System.Reactive;
using LogicSimulator.Views;
using LogicSimulator.Models;

namespace LogicSimulator.ViewModels {
    public class LauncherWindowViewModel: ViewModelBase
    { // Контекст окна
        Window? me;
        private static readonly MainWindow mw = new(); // Основное окно-редактор

        public LauncherWindowViewModel()
        { // Конструктор, Создаем команды пользовательского ввода
            Create = ReactiveCommand.Create<Unit, Unit>(_ => { FuncCreate(); return new Unit(); });
            Open = ReactiveCommand.Create<Unit, Unit>(_ => { FuncOpen(); return new Unit(); });
            Exit = ReactiveCommand.Create<Unit, Unit>(_ => { FuncExit(); return new Unit(); });
        }
        public void AddWindow(Window lw) => me = lw; // Метод для установки контекста окна

        void FuncCreate() { //функция создания нового проекта
            var newy = map.filer.CreateProject(); 
            CurrentProj = newy; // Устанавливаем текущий проект
            mw.Show(); // Показываем основное окно
            mw.Update(); //обновляем окно
            me?.Close(); //закрываем окно
        }
        void FuncOpen()
        { 
            if (me == null) return; // Если контекст окна не определен, завершаем обработчик

            var selected = map.filer.SelectProjectFile(me); // Выбираем проект для открытия
            if (selected == null) return; // Если проект не выбран, завершаем обработчик

            CurrentProj = selected; // Устанавливаем текущий проект
            mw.Show();
            mw.Update();
            me?.Close(); //закрываем LauncherWindow
        }
        void FuncExit() {
            me?.Close(); //закрываем LauncherWindow
            mw.Close(); //закрываем MainWindow
        }
        // Команды пользовательского ввода
        public ReactiveCommand<Unit, Unit> Create { get; }
        public ReactiveCommand<Unit, Unit> Open { get; }
        public ReactiveCommand<Unit, Unit> Exit { get; }


        public static Project[] ProjectList { get => map.filer.GetSortedProjects(); } // Коллекция всех проектов

        public void DTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e) { // Обработчик двойного щелчка по элементу списка проектов
            var src = (Control?) e.Source;  // Получаем источник события

            if (src is ContentPresenter cp && cp.Child is Border bord) src = bord; // Если источник ContentPresenter, выбираем его Child элемент - Border
            if (src is Border bord2 && bord2.Child is TextBlock tb2) src = tb2; // Если источник является Border, выбираем его Child элемент - TextBlock

            if (src is not TextBlock tb || tb.Tag is not Project proj) return; // Если источник не является TextBlock или его Tag не является проектом, завершаем обработчик

            CurrentProj = proj; // Устанавливаем текущий проект

            mw.Show();
            mw.Update();
            me?.Close();
        }

        /*
         * Для тестирования
         */

        public static MainWindow GetMW => mw;  // Метод для получения объекта MainWindow
    }
}