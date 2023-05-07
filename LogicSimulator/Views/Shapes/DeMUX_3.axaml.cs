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

        public void Brain(ref bool[] ins, ref bool[] outs) {
            bool yeah = ins[0];
            int num = (ins[1] ? 4 : 0) | (ins[2] ? 2 : 0) | (ins[3] ? 1 : 0);
            for (int i = 0; i < 8; i++) outs[i] = yeah && i == num;
        }
    }
}
