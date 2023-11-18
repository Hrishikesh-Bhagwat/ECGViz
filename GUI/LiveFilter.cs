using NumSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBCI_GUI
{
    internal class LiveFilter
    {
        private List<double> xs;
        private List<double> ys;

        private List<double> a;
        private List<double> b;
        public LiveFilter(List<double> _b, List<double> _a) {
            a=new List<double>();
            b=new List<double>();

            for (int i = 0; i < _b.Count; i++) {
                b.Add(_b[i]);
            }

            for (int i = 0; i < _a.Count; i++) {
                a.Add(_a[i]);
            }

            xs = new List<double>();
            ys = new List<double>();
            for (int i = 0; i < b.Count; i++) {
                xs.Add(0);
            }
            for (int i = 0;i< a.Count-1; i++) {
                ys.Add(0);
            }
        }

        public void AddToXs(double value) {
            xs.Insert(0,value);
            if (xs.Count > b.Count) {
                xs.RemoveAt(xs.Count-1);
            }
        }
        public void AddToYs(double value) {
            ys.Insert(0, value);
            ys.RemoveAt(ys.Count-1);
        }

        public static double dot(double[] x1, double[] x2) {
            double sum = 0;
            for (int i = 0; i < x1.Length; i++) {
                sum += x1[i] * x2[i];
            }
            return sum;
        }

        public double Process(double value) {
            AddToXs(value);
            double y = dot(b.ToArray(),xs.ToArray())-dot(a.Skip(1).ToArray(),ys.ToArray());
            y /= (double)a[0];
            AddToYs(y);
            return y;
        }

        public void reset() {
            for (int i = 0; i < xs.Count; i++) {
                xs[i] = 0;
            }
            for (int i = 0; i < ys.Count; i++)
            {
                ys[i] = 0;
            }
        }
    }
}
