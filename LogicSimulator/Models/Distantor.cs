using Avalonia;
using LogicSimulator.Views.Shapes; 

namespace LogicSimulator.Models { 
    public class Distantor { 
        public readonly int num; 
        public IGate parent; 
        public readonly string tag; 

        public Distantor(IGate parent, int n, string tag) {  
            this.parent = parent;
            num = n; // Например, в AND_2-gate'е:   0 и 1 - входы, 2 - выход
            this.tag = tag;
        }

        public Point GetPos() => parent.GetPinPos(num); 
    }
}
