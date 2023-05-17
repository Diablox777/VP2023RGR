using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using LogicSimulator.Models;
using LogicSimulator.Views;
using LogicSimulator.Views.Shapes;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;

namespace LogicSimulator.ViewModels {
    public class Log
    { // Класс для записи логов и вывода их в окно Logg в
        static readonly List<string> logs = new(); // Статический список строк, содержащий логи
        static readonly string path = "../../../Log.txt"; // Путь к файлу для записи логов
        static bool first = true; // Флаг, указывающий на то, что производится первая запись в файл

        static readonly bool use_file = false; // Флаг, указывающий на то, что нужна запись в файл

        public static MainWindowViewModel? Mwvm { private get; set; } //логирование и отображение сообщений польз.интерфейсу, а также запись сообщений в файл.
        public static void Write(string message, bool without_update = false) { //пишем в файл и при необходимости обновляем польз.интерфейс
            if (!without_update)
            { //если равен false (по умолчанию), сообщение разбивается на строки и каждая строка добавляется в список logs.
                foreach (var mess in message.Split('\n')) logs.Add(mess);
                while (logs.Count > 45) logs.RemoveAt(0); //Если количество записей в списке больше 45, удаляются самые старые записи.

                if (Mwvm != null) Mwvm.Logg = string.Join('\n', logs); //Если не равно null, присваивает свойству Logg значение логов, разделенных символом новой строки.
            }

            if (use_file)
            { // Если use_file равно true, сообщение также записывается в файл.
                if (first) File.WriteAllText(path, message + "\n"); //// Если это первое сообщение, создается новый файл и записывается первое сообщение.
                else File.AppendAllText(path, message + "\n"); //// В противном случае, сообщение добавляется к существующим сообщениям в файле.
                first = false;
            }
        }
    }

    public class MainWindowViewModel: ViewModelBase {
        private string log = ""; // Свойство Logg используется для обновления логов в пользовательском интерфейсе
        public string Logg { get => log; set => this.RaiseAndSetIfChanged(ref log, value); }

        public MainWindowViewModel()
        {  // Конструктор, инициализирующий Log.Mwvm, Comm и NewItem
            Log.Mwvm = this;
            Comm = ReactiveCommand.Create<string, Unit>(n => { FuncComm(n); return new Unit(); });
            NewItem = ReactiveCommand.Create<Unit, Unit>(_ => { FuncNewItem(); return new Unit(); });
        }

        private Window? mw;
        public void AddWindow(Window window)
        { // Метод AddWindow используется для добавления окна в окружение приложения
            var canv = window.Find<Canvas>("Canvas"); // Находим Canvas в окне Window и сохраняем его в переменной canv

            mw = window;  // Сохраняем экземпляр окна Window в поле mw и устанавливаем для карты свойство canv
            map.canv = canv;
            if (canv == null) return;  // Если на экране нет элемента Canvas, завершаем метод

            canv.Children.Add(map.Marker);  // Добавляем маркеры на Canvas
            canv.Children.Add(map.Marker2);

            var panel = (Panel?) canv.Parent; // Получаем Panel элемент, хранящий элемент Canvas
            if (panel == null) return; 

            panel.PointerPressed += (object? sender, PointerPressedEventArgs e) => {  // Добавляем обработчики событий нажатия, перемещения и отпускания кнопки мыши на Canvas
                if (e.Source != null && e.Source is Control @control) map.Press(@control, e.GetCurrentPoint(canv).Position);
                //Если источник события не равен null и является элементом Control, то вызывается метод Press
                //объекта map с параметрами @control и позицией, полученной из текущей точки события e на холсте.
            };
            panel.PointerMoved += (object? sender, PointerEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) map.Move(@control, e.GetCurrentPoint(canv).Position);
            };
            panel.PointerReleased += (object? sender, PointerReleasedEventArgs e) => { // Подписываемся на событие PointerReleased панели "panel"
                if (e.Source != null && e.Source is Control @control)
                { // Если источник события не равен null и является элементом Control
                    int mode = map.Release(@control, e.GetCurrentPoint(canv).Position); // Вызываем метод Release объекта map с параметрами @control и позицией, полученной из текущей точки события e на холсте
                    bool tap = map.tapped;  // Получаем значение переменной tapped из объекта map
                    if (tap && mode == 1) { //если холст нажат
                        var pos = map.tap_pos;
                        if (canv == null) return; // Если переменная canv равна null, то возвращаем управление

                        var newy = map.GenSelectedItem(); // Создаем новый элемент с помощью метода GenSelectedItem объекта map
                        newy.Move(pos);
                        map.AddItem(newy);
                    }
                }
            };
            panel.PointerWheelChanged += (object? sender, PointerWheelEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) map.WheelMove(@control, e.Delta.Y, e.GetCurrentPoint(canv).Position); // Если источник события не равен null и является элементом Control
            }; // Вызываем метод WheelMove объекта map с параметрами @control, изменением положения колеса мыши и позицией текущей точки события e на холсте
            mw.KeyDown += (object? sender, KeyEventArgs e) => { 
                if (e.Source != null && e.Source is Control @control) map.KeyPressed(@control, e.Key); // Вызываем метод KeyPressed объекта map с параметрами @control и нажатой клавишей
            };
        }

        public static IGate[] ItemTypes { get => map.item_types; } //возвращает массив элементов типа IGate из объекта map
        public static int SelectedItem { get => map.SelectedItem; set => map.SelectedItem = value; } //возвращает или изменяет выделенный элемент на холсте через объект map

        /*
         * Обработка той самой панели со схемами проекта
         */

        Grid? cur_grid; //Переменные для хранения текущего грида
        TextBlock? old_b_child; //предыдущего текстблока
        object? old_b_child_tag; //и его тэга
        string? prev_scheme_name; //а также предыдущего названия схемы

        public static string ProjName { get => CurrentProj == null ? "???" : CurrentProj.Name; } //// Определяем свойство ProjName, которое возвращает название текущего проекта, или "???" если проект не выбран

        public static ObservableCollection<Scheme> Schemes { get => CurrentProj == null ? new() : CurrentProj.schemes; } //возвращает коллекцию схем текущего проекта, или пустую коллекцию если проект не выбран



        public void DTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        { // Метод для обработки двойного клика мыши на элементе на холсте
            var src = (Control?) e.Source;

            if (src is ContentPresenter cp && cp.Child is Border bord) src = bord; // Из тэга TextBlock получаем TextBox и заменяем им TextBlock для возможности редактирования
            if (src is Border bord2 && bord2.Child is Grid g2) src = g2;
            if (src is Grid g3 && g3.Children[0] is TextBlock tb2) src = tb2;

            if (src is not TextBlock tb) return; // Если источник события не является TextBlock, то выходим из метода

            var p = tb.Parent;  // Находим элемент-родитель и если он не является Grid, то выходим из метода
            if (p == null) return;

            if (old_b_child != null)
                if (cur_grid != null) cur_grid.Children[0] = old_b_child;

            if (p is not Grid g) return;
            cur_grid = g;

            old_b_child = tb; // Запоминаем исходный объект TextBlock и его тэг
            old_b_child_tag = tb.Tag;
            prev_scheme_name = tb.Text;

            var newy = new TextBox { Text = tb.Text }; // Создаем новый объект TextBox и заменяем им TextBlock на холсте

            
            cur_grid.Children[0] = newy;
            

            newy.KeyUp += (object? sender, KeyEventArgs e) => { // Определяем обработчик события KeyUp для нового TextBox
                if (e.Key != Key.Return) return;  // Если нажата не клавиша Enter, то выходим из метода

                if (newy.Text != prev_scheme_name)
                { // Если текст в TextBox изменился, то изменяем имя текущего проекта или схемы

                    if ((string?) tb.Tag == "p_name") CurrentProj?.ChangeName(newy.Text);
                    else if (old_b_child_tag is Scheme scheme) scheme.ChangeName(newy.Text);
                }

                cur_grid.Children[0] = tb;  // Восстанавливаем вместо TextBox исходный объект TextBlock на холсте
                cur_grid = null; old_b_child = null;
            };
        }

        public void Update() { //  обновления интерфейса, вызова которого происходит после смены проекта
            Log.Write("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n    Текущий проект:\n" + CurrentProj); // При смене проекта выводим информацию о текущем проекте

            map.ImportScheme();  // Импортируем схему текущего проекта в объект map

            // Вызываем метод RaisePropertyChanged, который сообщает интерфейсу об обновлении свойств
            this.RaisePropertyChanged(new(nameof(ProjName))); // Обновляем название текущего проекта
            this.RaisePropertyChanged(new(nameof(Schemes))); // Обновляем список схем текущего проекта
            this.RaisePropertyChanged(new(nameof(CanSave))); //Обновляем возможность сохранения проекта
            if (mw != null) mw.Width++; // ГОРАААААААААААААЗДО больше толку, чем от всех этих НЕРАБОЧИХ через раз RaisePropertyChanged
        }

        public static bool CanSave { get => CurrentProj != null && CurrentProj.CanSave(); } // Определяем свойство CanSave, которое возвращает True если есть текущий проект и он может быть сохранен в файл

        /*
         * Кнопочки!
         */

        public void FuncComm(string Comm) { // Метод FuncComm используется для выполнения функций в зависимости от команды
            switch (Comm) {
            case "Create": // Создание нового проекта
                var newy = map.filer.CreateProject();
                CurrentProj = newy;
                Update();
                break;
            case "Open": // Открытие проекта
                if (mw == null) break;
                var selected = map.filer.SelectProjectFile(mw);
                if (selected != null) { // Если окно MainWindow не существует, то выходим из метода
                    CurrentProj = selected;
                    Update();
                }
                break;
            case "Save": //Сохранение проекта
                map.Export(); // Экспортируем текущую схему
                // Для создания тестовых штучек:
                File.WriteAllText("../../../for_test.json", Utils.Obj2json((map.current_scheme ?? throw new System.Exception("Что?!")).Export()));
                break;
            case "SaveAs": // Сохранение проекта с новым названием
                map.Export();
                if (mw != null) CurrentProj?.SaveAs(mw); // Сохраняем проект с новым названием
                this.RaisePropertyChanged(new(nameof(CanSave)));
                break;
            case "ExitToLauncher": // Выход из программы в Launcher
                new LauncherWindow().Show(); //// Открываем окно LauncherWindow
                mw?.Hide(); // Скрываем окно MainWindow
                break;
            case "Exit": //Выход из программы
                mw?.Close();
                break;
            }
        }

        public ReactiveCommand<string, Unit> Comm { get; } 

        private static void FuncNewItem() {
            CurrentProj?.AddScheme(null);
        }

        public ReactiveCommand<Unit, Unit> NewItem { get; }  // Команда для создания новой схемы

        public static bool LockSelfConnect { get => map.lock_self_connect; set => map.lock_self_connect = value; } // Свойство LockSelfConnect, которое позволяет блокировать соединение элементов самих с собой
    }
}