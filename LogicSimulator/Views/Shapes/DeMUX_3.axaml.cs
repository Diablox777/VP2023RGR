using Avalonia.Controls;
using System.ComponentModel;

namespace LogicSimulator.Views.Shapes {
    public partial class DeMUX_3: GateBase, IGate, INotifyPropertyChanged {
        public override int TypeId => 4; 

        public override UserControl GetSelf() => this; 
        protected override IGate GetSelfI => this;
        protected override int[][] Sides => new int[][] {
            System.Array.Empty<int>(),
            new int[] { 0 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 0, 0, 0 }
        };

        protected override void Init() => InitializeComponent(); 

        /*
         * Мозги
         */

        public void Brain(ref bool[] ins, ref bool[] outs)
        { //содержит логику работы данного устройства, получает на вход два массива - ins и outs - со значениями входов и выходов соответственно.
            bool yeah = ins[0]; //Входной сигнал yeah устанавливается равным первому элементу массива
            int num = (ins[1] ? 4 : 0) | (ins[2] ? 2 : 0) | (ins[3] ? 1 : 0); //устанавливается равной числу, полученному из трёх последующих битов массива ins.
            for (int i = 0; i < 8; i++) outs[i] = yeah && i == num; //В цикле происходит установка всех элементов массива outs в true
        } //если входной сигнал yeah равен true и номер текущего элемента i равен num
    }
}
