using Avalonia.Controls;
using Avalonia;
using LogicSimulator.ViewModels;
using LogicSimulator.Views.Shapes;
using System;
using System.Collections.Generic;
using DynamicData;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.LogicalTree;
using System.Linq;
using Button = LogicSimulator.Views.Shapes.Button;
using Avalonia.Input;

namespace LogicSimulator.Models {
    public class Mapper
    {   // Класс Mapper содержит разнообразные элементы, используемые для отрисовки визуальных элементов
        // в приложении, а также методы для их обновления.
        readonly Line marker = new() { Tag = "Marker", ZIndex = 2, IsVisible = false, Stroke = Brushes.YellowGreen, StrokeThickness = 3 };
        readonly Rectangle marker2 = new() { Tag = "Marker", Classes = new("anim"), ZIndex = 2, IsVisible = false, Stroke = Brushes.MediumAquamarine, StrokeThickness = 3 };
        
        public Line Marker { get => marker; } //используется для отрисовки маркера при выделении элемента (зеленая линия);
        public Rectangle Marker2 { get => marker2; } //используется для отрисовки маркера при выделении линии (ярко-зеленый прямоугольник);

        public readonly Simulator sim = new(); //используется для создания схемы из логических элементов;

        public Canvas canv = new(); //используется для отрисовки элементов в приложении;

        /*
         * Маркер
         */

        private IGate? marked_item; //содержит ссылку на выделенный элемент;
        private JoinedItems? marked_line; //содержит ссылку на сеть связанных линий, выделенную на схеме.

        private void UpdateMarker()   { //используется для обновления маркера при выделении элемента или линии
            marker2.IsVisible = marked_item != null || marked_line != null; //определяет, какой элемент был выделен (линия или логический элемент)

            if (marked_item != null) { //Если выделен логический элемент, то маркер отображается вокруг этого элемента в виде прямоугольника. 
                var bound = marked_item.GetBounds();
                marker2.Margin = new(bound.X, bound.Y);
                marker2.Width = bound.Width;
                marker2.Height = bound.Height;
                marked_line = null;
            }

            if (marked_line != null) { //Если же выделена линия, то маркер отображается вокруг концов линии в виде растянутого прямоугольника.
                var line = marked_line.line;
                var A = line.StartPoint;
                var B = line.EndPoint;
                marker2.Margin = new(Math.Min(A.X, B.X), Math.Min(A.Y, B.Y));
                marker2.Width = Math.Abs(A.X - B.X);
                marker2.Height = Math.Abs(A.Y - B.Y);
            }
        }

        /*
         * Выборка элементов
         */

        private int selected_item = 0; //отвечает за выбранный тип элемента.По умолчанию установлено значение "0";
        public int SelectedItem { get => selected_item; set => selected_item = value; } //указывает на выбранный тип элемента.

        private static IGate CreateItem(int n) { // нумерация логических элементов
            return n switch {
                0 => new AND_2(),
                1 => new OR_2(),
                2 => new NOT(),
                3 => new XOR_2(),
                4 => new DeMUX_3(),
                5 => new Switch(),
                6 => new Button(),
                7 => new LightBulb(),
                8 => new NAND_2(),
                9 => new FlipFlop(),
                10 => new OR_8(),
                11 => new AND_8(),
                _ => new AND_2(),
            };
        }

        public IGate[] item_types = Enumerable.Range(0, 12).Select(CreateItem).ToArray();

        public IGate GenSelectedItem() => CreateItem(selected_item); //возвращает выбранный элемент типа IGate.

        /*
         * Хранилище
         */

        readonly List<IGate> items = new();

        /* Private fields:
            - items: List элементов IGate - коллекция всех элементов на холсте;
            - sim: объект класса Simulator, представляющий симуляцию логической схемы;
            - marked_item: содержит ссылку на выделенный элемент;
            - marked_line: содержит ссылку на сеть связанных линий, выделенную на схеме.*/
        private void AddToMap(IControl item) { //используется для добавления элемента на холст в приложении;
            canv.Children.Add(item);
        }

        public void AddItem(IGate item) { //добавляет переданный им элемент на холст
            items.Add(item); //также обновляет список элементов items
            sim.AddItem(item); //добавляет переданный элемент в симуляцию
            AddToMap(item.GetSelf()); //добавляет элемент на холст
        }
        public void RemoveItem(IGate item) { //удаляющий элемент из коллекции элементов
            if (marked_item != null) { 
                marked_item = null;
                UpdateMarker();
            }
            if (marked_line != null && item.ContainsJoin(marked_line)) {
                marked_line = null;
                UpdateMarker();
            }

            items.Remove(item); 
            sim.RemoveItem(item);

            item.ClearJoins(); //удаляет сам элемент из холста
            ((Control) item).Remove();
        }
        public void RemoveAll() { //для удаления всех элементов с холста и очистки симуляции;
            foreach (var item in items.ToArray()) RemoveItem(item);
            sim.Clear();
        }

        private void SaveAllPoses() { //сохраняет позицию каждого элемента на холсте
            foreach (var item in items) item.SavePose();
        }

        /*
         * Определение режима перемещения
         */

        int mode = 0; //переменная, хранящая текущий режим приложения.
        /*
         *    Режимы:
         * 0 - ничего не делает
         * 1 - двигаем камеру
         * 2 - двигаем элемент
         * 3 - тянем элемент
         * 4 - вышвыриваем элемент
         * 5 - тянем линию от входа (In)
         * 6 - тянем линию от выхода (Out)
         * 7 - тянем линию от узла (IO)
         * 8 - тянем уже существующее соединение - переподключаем
        */

        private static int CalcMode(string? tag) {
            if (tag == null) return 0;
            return tag switch {
                "Scene" => 1,
                "Body" => 2,
                "Resizer" => 3,
                "Deleter" => 4,
                "In" => 5,
                "Out" => 6,
                "IO" => 7,
                "Join" => 8,
                "Pin" or _ => 0,
            };
        }

        //Этот код содержит методы для обработки событий элементов управления и извлечения данных из этих элементов.
        private void UpdateMode(Control item) => mode = CalcMode((string?) item.Tag); //устанавливает режим приложения на основании выбранного элемента управления. 
        //получает ссылку на элемент управления и вызывает CalcMode(), чтобы определить соответствующий режим
        private static bool IsMode(Control item, string[] mods) { //проверяет, содержит ли элемент определенный набор режимов.
            var name = (string?) item.Tag; //получает ссылку на элемент управления и на набор режимов
            if (name == null) return false; //определяет, присутствует ли имя элемента в массиве режимов
            return mods.IndexOf(name) != -1; //возвращает true, если имя элемента присутствует в массиве, и false в противном случае;
        }

        private static UserControl? GetUC(Control item) { //рекурсивно переходит по цепочке родительских элементов, пока не найдет UserContro
            while (item.Parent != null) { //который соответствует переданному элементу управления
                if (item is UserControl @UC) return @UC;
                item = (Control) item.Parent;
            }
            return null;
        }
        private static IGate? GetGate(Control item) { 
            var UC = GetUC(item); //вызывает метод GetUC для получения UserControl,
            if (UC is IGate @gate) return @gate; // проверяет, является ли UserControl типом IGate
            return null; //Если да, то метод возвращает объект IGate, иначе возвращает null.
        }

        /*
         * Обработка мыши
         */

        Point moved_pos; //текущее положение перетаскиваемого элемента;
        IGate? moved_item; //объект класса IGate, который был выбран для перетаскивания;
        Point item_old_pos; //старое положение выбранного элемента;
        Size item_old_size; //старый размер выбранного элемента;

        Ellipse? marker_circle; //используется для отображения маркера при режиме добавления линии;
        Distantor? start_dist; //удаленный элемент, от которого начинается добавление линии в режиме добавления удаленной связи
        int marker_mode; //используется для хранения текущего режима при добавлении линии

        Line? old_join; //ссылка на существующую линию, которую нужно удалить;
        bool join_start; //флаг, который определяет, является ли текущий элемент началом линии при добавлении связи на схему
        bool delete_join = false; //флаг, который определяет, нужно ли удалить текущую связь на схеме;

        public bool lock_self_connect = true; //флаг, который определяет, должны ли элементы иметь связи сами с собой.

        public void Press(Control item, Point pos) { // Этот метод обрабатывает событие нажатия на элемент управления
                                                     // Входные параметры: item - элемент управления, на который нажали, pos - координаты нажатия    

            UpdateMode(item); // Обновляем текущий режим редактора


            moved_pos = pos; // Запоминаем координаты нажатия и перемещаемый элемент
            moved_item = GetGate(item);
            tapped = true; // Устанавливаем флаг "нажатия" для перемещения элемента
            if (moved_item != null) item_old_pos = moved_item.GetPos();

            switch (mode) {  //обрабатываем различные режимы редактора
            case 1:
                SaveAllPoses(); //сохранение всех позиций элементов на поле
                break;
            case 3:
                if (moved_item == null) break;
                item_old_size = moved_item.GetBodySize(); //изменение размера элемента
                break;
            case 5 or 6 or 7:
                if (marker_circle == null) break;
                var gate = GetGate(marker_circle) ?? throw new Exception("Что?!"); // создание соединения между элементами
                start_dist = gate.GetPin(marker_circle);

                var circle_pos = start_dist.GetPos(); //устанавливаем координаты маркера на начальную точку соединения
                marker.StartPoint = marker.EndPoint = circle_pos;
                marker.IsVisible = true;
                marker_mode = mode;
                break;
            case 8:
                if (item is not Line @join) break;
                JoinedItems.arrow_to_join.TryGetValue(@join, out var @join2); //соединение двух линий
                if (@join2 == null) break;

                if (marked_line == @join2) { //если линия для соединения уже помечена, то снимаем метку и обновляем маркер
                    marked_line = null;
                    UpdateMarker();
                }

                var dist_a = @join.StartPoint.Hypot(pos); //определяем к какому концу соединяться и запоминаем линию
                var dist_b = @join.EndPoint.Hypot(pos);
                join_start = dist_a > dist_b;
                old_join = @join;

                marker.StartPoint = join_start ? @join.StartPoint : pos; //устанавливаем координаты и режим маркера для соединения
                marker.EndPoint = join_start ? pos : @join.EndPoint;
                marker_mode = CalcMode(join_start ? @join2.A.tag : @join2.B.tag);

                marker.IsVisible = true; //показываем маркер и скрываем линию
                @join.IsVisible = false;
                break;
            }

            Move(item, pos);
        }

        public void FixItem(ref Control res, Point pos, IEnumerable<ILogical> items) {
            foreach (var logic in items) {
                
                var item = (Control) logic;
                var tb = item.TransformedBounds;
                
                if (tb != null && tb.Value.Bounds.TransformToAABB(tb.Value.Transform).Contains(pos) && (string?) item.Tag != "Join") res = item; 
                FixItem(ref res, pos, item.GetLogicalChildren());
            }
        }
        public void Move(Control item, Point pos, bool use_fix = true) {
            

            if (use_fix && (mode == 5 || mode == 6 || mode == 7 || mode == 8)) {
                var tb = canv.TransformedBounds;
                if (tb != null) {
                    item = new Canvas() { Tag = "Scene" };
                    var bounds = tb.Value.Bounds.TransformToAABB(tb.Value.Transform);
                    FixItem(ref item, pos + bounds.TopLeft, canv.Children);
                    
                }
            }

            string[] mods = new[] { "In", "Out", "IO" };
            var tag = (string?) item.Tag;
            if (IsMode(item, mods) && item is Ellipse @ellipse
                && !(marker_mode == 5 && tag == "In" || marker_mode == 6 && tag == "Out" ||
                lock_self_connect && moved_item == GetGate(item))) { 

                if (marker_circle != null && marker_circle != @ellipse) { 
                    marker_circle.Fill = new SolidColorBrush(Color.Parse("#0000"));
                    marker_circle.Stroke = Brushes.Gray;
                }
                marker_circle = @ellipse;
                @ellipse.Fill = Brushes.Lime;
                @ellipse.Stroke = Brushes.Green;
            } else if (marker_circle != null) {
                marker_circle.Fill = new SolidColorBrush(Color.Parse("#0000"));
                marker_circle.Stroke = Brushes.Gray;
                marker_circle = null;
            }

            if (mode == 8) delete_join = (string?) item.Tag == "Deleter";




            var delta = pos - moved_pos;
            if (delta.X == 0 && delta.Y == 0) return;

            if (Math.Pow(delta.X, 2) + Math.Pow(delta.Y, 2) > 9) tapped = false;

            switch (mode) {
            case 1:
                foreach (var item_ in items) {
                    var pose = item_.GetPose();
                    item_.Move(pose + delta, true);
                }
                UpdateMarker();
                break;
            case 2:
                if (moved_item == null) break;
                var new_pos = item_old_pos + delta;
                moved_item.Move(new_pos);
                UpdateMarker();
                break;
            case 3:
                if (moved_item == null) break;
                var new_size = item_old_size + new Size(delta.X, delta.Y);
                moved_item.Resize(new_size);
                UpdateMarker();
                break;
            case 5 or 6 or 7:
                var end_pos = marker_circle == null ? pos : marker_circle.Center(canv);
                marker.EndPoint = end_pos;
                break;
            case 8:
                if (old_join == null) break;
                var p = marker_circle == null ? pos : marker_circle.Center(canv);
                if (join_start) marker.EndPoint = p;
                else marker.StartPoint = p;
                break;
            }
        }

        public bool tapped = false; // Обрабатывается после Release
        public Point tap_pos; // Обрабатывается после Release

        public int Release(Control item, Point pos, bool use_fix = true) {
            Move(item, pos, use_fix);

            switch (mode) {
            case 5 or 6 or 7:
                if (start_dist == null) break;
                if (marker_circle != null) {
                    var gate = GetGate(marker_circle) ?? throw new Exception("Что?!"); 
                    var end_dist = gate.GetPin(marker_circle);

                    var newy = new JoinedItems(start_dist, end_dist);
                    AddToMap(newy.line);
                }
                marker.IsVisible = false;
                marker_mode = 0;
                break;
            case 8:
                if (old_join == null) break;
                JoinedItems.arrow_to_join.TryGetValue(old_join, out var @join);
                if (marker_circle != null && @join != null) {
                    var gate = GetGate(marker_circle) ?? throw new Exception("Что?!"); 
                    var p = gate.GetPin(marker_circle);
                    @join.Delete();

                    var newy = join_start ? new JoinedItems(@join.A, p) : new JoinedItems(p, @join.B);
                    AddToMap(newy.line);
                } else old_join.IsVisible = true;

                marker.IsVisible = false;
                marker_mode = 0;
                old_join = null;

                if (delete_join) @join?.Delete();
                delete_join = false;
                break;
            }

            if (tapped) Tapped(item, pos);

            int res_mode = mode;
            mode = 0;
            moved_item = null;
            return res_mode;
        }

        private void Tapped(Control item, Point pos) {
            tap_pos = pos;

            switch (mode) {
            case 2 or 8:
                if (item is Line @line) {
                    if (!JoinedItems.arrow_to_join.TryGetValue(@line, out var @join)) break;
                    marked_item = null;
                    marked_line = @join;
                    UpdateMarker();
                    break;
                }

                if (moved_item == null) break;

                marked_item = moved_item;
                UpdateMarker();
                break;
            }
        }

        public void WheelMove(Control item, double move, Point pos) {
            int mode = CalcMode((string?) item.Tag);
            double scale = move > 0 ? 1.1 : 1 / 1.1;
            double inv_scale = 1 / scale;

            switch (mode) {
            case 1:
                foreach (var gate in items) {
                    gate.ChangeScale(scale, true);

                    var item_pos = gate.GetPos();
                    var delta = item_pos - pos;
                    delta *= scale;
                    var new_pos = delta + pos;
                    gate.Move(new_pos, false);
                }
                UpdateMarker();
                break;
            case 2:
                var gate2 = GetGate(item);
                if (gate2 == null) return;
                gate2.ChangeScale(inv_scale);
                UpdateMarker();
                break;
            }
        }

        public void KeyPressed(Control _, Key key) {
            switch (key) {
            case Key.Up:
            case Key.Left:
            case Key.Right:
            case Key.Down:
                int dx = key == Key.Left ? -1 : key == Key.Right ? 1 : 0;
                int dy = key == Key.Up ? -1 : key == Key.Down ? 1 : 0;
                marked_item?.Move(marked_item.GetPos() + new Point(dx * 10, dy * 10));
                UpdateMarker();
                break;
            case Key.Delete:
                if (marked_item != null) RemoveItem(marked_item);
                if (marked_line != null) {
                    marked_line.Delete();
                    marked_line = null;
                    UpdateMarker();
                }
                break;
            }
        }


        /*
         * Экспорт и импорт
         */

        public readonly FileHandler filer = new();
        public Scheme? current_scheme;

        public void Export() {
            if (current_scheme == null) return;

            var arr = items.Select(x => x.Export()).ToArray();

            Dictionary<IGate, int> item_to_num = new();
            int n = 0;
            foreach (var item in items) item_to_num.Add(item, n++);
            List<object[]> joins = new();
            foreach (var item in items) joins.Add(item.ExportJoins(item_to_num));

            sim.Clean();
            string states = sim.Export();

            try { current_scheme.Update(arr, joins.ToArray(), states); }
            catch (Exception e) { Log.Write("Save error:\n" + e); }

        }

        public void ImportScheme(bool start = true) {
            if (current_scheme == null) return;

            sim.Stop();
            sim.lock_sim = true;

            RemoveAll();

            List<IGate> list = new();
            foreach (var item in current_scheme.items) {
                if (item is not Dictionary<string, object> @dict) { Log.Write("Не верный тип элемента: " + item); continue; }

                if (!@dict.TryGetValue("id", out var @value)) { Log.Write("id элемента не обнаружен"); continue; }
                if (@value is not int @id) { Log.Write("Неверный тип id: " + @value); continue; }
                var newy = CreateItem(@id);

                newy.Import(@dict);
                AddItem(newy);
                list.Add(newy);
            }
            var items_arr = list.ToArray();

            List<JoinedItems> joinz = new();
            foreach (var obj in current_scheme.joins) {
                object[] join;
                if (obj is List<object> @j) join = @j.ToArray();
                else if (obj is object[] @j2) join = @j2;
                else { Log.Write("Одно из соединений не того типа: " + obj + " " + Utils.Obj2json(obj)); continue; }
                if (join.Length != 6 ||
                    join[0] is not int @num_a || join[1] is not int @pin_a || join[2] is not string @tag_a ||
                    join[3] is not int @num_b || join[4] is not int @pin_b || join[5] is not string @tag_b) { Log.Write("Содержимое списка соединения ошибочно"); continue; }

                var newy = new JoinedItems(new(items_arr[@num_a], @pin_a, tag_a), new(items_arr[@num_b], @pin_b, tag_b));
                AddToMap(newy.line);
                joinz.Add(newy);
            }

            foreach (var join in joinz) join.Update();

            sim.Import(current_scheme.states);
            sim.lock_sim = false;
            if (start) sim.Start();
        }
    }
}
