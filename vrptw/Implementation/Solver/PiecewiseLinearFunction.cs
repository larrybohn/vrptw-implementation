using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Implementation.Solver
{
    public class Triplet
    {
        public int Item1 { get; set; }
        public int Item2 { get; set; }
        public int Item3 { get; set; }
        public static Triplet Create(int item1, int item2, int item3)
        {
            return new Triplet { Item1 = item1, Item2 = item2, Item3 = item3 }; 
        }
    }

    public class PiecewiseLinearFunction
    {
        public List<Triplet> values { get; set; }

        public PiecewiseLinearFunction()
        {
            values = new List<Triplet>();
            values.Add(Triplet.Create(int.MinValue, int.MaxValue, 0));
        }

        public PiecewiseLinearFunction(List<Triplet> values)
        {
            this.values = values;
        }

        public static PiecewiseLinearFunction PenaltyFunction(int startTime, int endTime, int penalty)
        {
            var values = new List<Triplet>(3);
            values.Add(Triplet.Create(int.MinValue, startTime, penalty));
            values.Add(Triplet.Create(startTime, endTime, 0));
            values.Add(Triplet.Create(endTime, int.MaxValue, penalty));

            return new PiecewiseLinearFunction(values);
        }

        public PiecewiseLinearFunction Shift(int value) {

            var v = new List<Triplet>(values.Count);

            for (int i = 0; i < values.Count; ++i)
            {
                v.Add(
                    Triplet.Create(
                        values[i].Item1 == int.MinValue ? int.MinValue : values[i].Item1 - value,
                        values[i].Item2 == int.MaxValue ? int.MaxValue : values[i].Item2 - value,
                        values[i].Item3)
                );
            }

            return new PiecewiseLinearFunction(v);
        }

        public PiecewiseLinearFunction Min()
        {
            var v = new List<Triplet>(values.Count);

            int currentMin = int.MaxValue;

            for (int i = 0; i < values.Count; ++i)
            {
                if (values[i].Item3 < currentMin)
                {
                    v.Add(Triplet.Create(values[i].Item1, values[i].Item2, values[i].Item3));
                    currentMin = values[i].Item3;
                }
                else
                {
                    v[v.Count - 1].Item2 = values[i].Item2;
                }
            }

            return new PiecewiseLinearFunction(v);
        }

        public PiecewiseLinearFunction Add(PiecewiseLinearFunction f)
        {
            var g = f.Clone();
            foreach (var x in values)
            {
                g = g.AddPiece(x);
            }

            return g;
        }

        public PiecewiseLinearFunction AddPiece(Triplet piece)
        {
            var v = this.values.ToArray().ToList();

            if (piece.Item3 == 0 || piece.Item1 >= piece.Item2)
            {
                return this.Clone();
            }

            for (int i = 0; i < v.Count; ++i)
            {
                if (piece.Item1 > v[i].Item1 && piece.Item1 < v[i].Item2)
                {
                    v.Insert(i, Triplet.Create(v[i].Item1, piece.Item1, v[i].Item3));
                    v[i+1].Item1 = piece.Item1;                    
                    break;
                }
            }

            for (int i = 0; i < v.Count; ++i)
            {
                if (piece.Item2 > v[i].Item1 && piece.Item2 < v[i].Item2)
                {
                    v.Insert(i, Triplet.Create(v[i].Item1, piece.Item2, v[i].Item3));
                    v[i + 1].Item1 = piece.Item2;
                    break;
                }
            }

            for (int i = 0; i < v.Count; ++i)
            {
                if (v[i].Item1 >= piece.Item1)
                {
                    v[i].Item3 += piece.Item3;
                }

                if (v[i].Item2 == piece.Item2)
                {
                    break;
                }
            }

            return new PiecewiseLinearFunction(v);

        }

        public int GetValue(int x)
        {
            foreach (var v in values)
            {
                if (x >= v.Item1 && x <= v.Item2)
                {
                    return v.Item3;
                }
            }
            return 0;
        }

        public PiecewiseLinearFunction Clone()
        {
            return new PiecewiseLinearFunction(values.ToArray().ToList());
        }

        
    }
}
