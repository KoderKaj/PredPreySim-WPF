using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PredPreySim
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Timer t;
        List<Thing> things = new List<Thing> (), thingsToAdd = new List<Thing>(), thingsToRem = new List<Thing>();
        public MainWindow()
        {
            InitializeComponent();
            Predator tempPred = new Predator(40, 0, 50);
            AddThing(tempPred);
            AddThing(new Prey(40, 500, 60));
            AddThing(new Prey(40, 250, 80));
            SetTimers();
        }
        private void SetTimers()
        {
            t = new Timer(100);
            t.Elapsed += TimerEvent;
            t.Enabled = true;
        }
        private double GetX(Ellipse e)
        {
            return (Canvas.GetRight(e) + Canvas.GetLeft(e))/2;
        }
        private double GetY(Ellipse e)
        {
            return (Canvas.GetTop(e) + Canvas.GetBottom(e)) / 2;
        }
        private void TimerEvent(Object source, ElapsedEventArgs e)
        {
            bool isPred;
            Prey target = null, prey;
            foreach(Thing thing in things)
            {
                double minMag = double.PositiveInfinity;
                isPred = thing.GetType() == typeof(Predator);
                foreach (Thing thing2 in things)
                {
                    if (thing2.GetType() == typeof(Prey) && thing != thing2)
                    {
                        prey = (Prey)thing2;
                        if (prey.MagCalc(thing)[0] < minMag)
                        {
                            minMag = prey.MagCalc(thing)[0];
                            target = prey;
                        }
                    }
                }
                thing.SetTarget(target);
                if (isPred)
                {
                    target.SetPred((Predator)thing);
                }
            }
            Predator prdtr; Prey p;
            foreach(Thing t in things)
            {
                if (t.GetType() == typeof(Predator))
                {
                    prdtr = (Predator)t;
                    if (prdtr.GetCollision())
                    {
                        thingsToRem.Add(p.GetTarget());
                        prdtr.ResetTtl();
                        prdtr.OffCollision();
                    }
                    if (prdtr.GetTtl() <= 0)
                    {
                        thingsToRem.Add(p);
                    }
                }
                else
                {
                    p = (Prey)t;
                    if (p.GetCollision())
                    {
                        if (p.GetCooldown() <= 0)
                        {
                            double newX = (p.GetX() + p.GetTarget().GetX()) / 2;
                            double newY = (p.GetY() + p.GetTarget().GetY()) / 2;
                            thingsToAdd.Add(new Prey(p.GetSize(), newX, newY));
                        }
                        t.OffCollision();
                    }
                }
            }
            if (thingsToRem.Count > 0)
            {
                foreach (Thing t in thingsToRem)
                {
                    things.Remove(t);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        canvas.Children.Remove(t.GetShape());
                    });
                }
                thingsToRem.Clear();
            }
            if(thingsToAdd.Count > 0)
            {
                foreach(Thing thing in thingsToAdd)
                {
                    AddThing(thing);
                }
                thingsToAdd.Clear();
            }
            foreach(Thing t in things)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    t.GetShape().RenderTransform = t.Move();
                });
            }
        }
        private void AddThing(Thing thing)
        {
            things.Add(thing);
            canvas.Children.Add(thing.GetShape());
        }
    }
    public abstract class Thing
    {
        protected double size, x, y, minDist, speed;
        protected SolidColorBrush brush = new SolidColorBrush();
        protected Ellipse shape = new Ellipse();
        protected Prey target;
        protected bool closeToPrey = false;
        public Thing(double newSize, double newX, double newY)
        {
            size = newSize;
            x = newX;
            y = newY;
            shape.Height = size;
            shape.Width = size;
        }
        public TranslateTransform Move()
        {
            ChangeXY();
            return new TranslateTransform(x, y);
        }
        public Ellipse GetShape()
        {
            return shape;
        }
        public double GetSize()
        {
            return size;
        }
        public double GetX()
        {
            return x;
        }
        public double GetY()
        {
            return y;
        }
        public void SetTarget(Prey closest)
        {
            target = closest;
        }
        public Prey GetTarget()
        {
            return target;
        }
        protected abstract void ChangeXY();
        public void OnCollision()
        {
            closeToPrey = true;
        }
        public void OffCollision()
        {
            closeToPrey = false;
        }
        public bool GetCollision()
        {
            return closeToPrey;
        }
    }
    public class Predator : Thing
    {
        int ttl;
        public Predator(double newSize, double newX, double newY) : base(newSize, newX, newY)
        {
            brush.Color = Colors.Red;
            shape.Fill = brush;
            minDist = 50;
            speed = 3;
            ttl = 50;
        }
        protected override void ChangeXY()
        {
            double diffX = target.GetX() - x;
            double diffY = y - target.GetY();
            double magnitude = (double)Math.Sqrt(diffX * diffX + diffY * diffY);
            if (magnitude > minDist)
            {
                diffX /= magnitude;
                diffY /= magnitude;
                x += diffX * speed;
                y -= diffY * speed;
            }
            else { closeToPrey = true; }
            ttl--;
        }
        public int GetTtl()
        {
            return ttl;
        }
        public void ResetTtl()
        {
            ttl = 50;
        }
    }
    public class Prey : Thing
    {
        int spawnCooldown;
        public Prey(double newSize, double newX, double newY) : base(newSize, newX, newY)
        {
            brush.Color = Colors.Blue;
            shape.Fill = brush;
            minDist = 40;
            speed = 2;
        }
        private Predator predator;
        public void SetPred(Predator p)
        {
            predator = p;
        }
        protected override void ChangeXY()
        {
            double[] predVals = MagCalc(predator);//magnitude, diffX, diffY
            double[] preyVals = MagCalc(target);
            if (preyVals[0] < predVals[0])
            {
                preyVals[1] /= preyVals[0];
                preyVals[2] /= preyVals[0];
                if (preyVals[0] > minDist)
                {
                    x += preyVals[1] * speed;
                    y -= preyVals[2] * speed;
                }
                else if (closeToPrey) { closeToPrey = false; } //To prevent "double bookings" (when two prey are close the only spawn one new prey)
                else 
                {
                    closeToPrey = true; target.OnCollision();
                    x -= preyVals[1] * speed;
                    y += preyVals[2] * speed;
                }
            }
            else
            {
                if (predVals[0] > minDist)
                {
                    predVals[1] /= predVals[0];
                    predVals[2] /= predVals[0];
                    x -= predVals[1] * speed;
                    y += predVals[2] * speed;
                }
                else { predator.OnCollision(); }
            }
            if (spawnCooldown > 0)
            {
                spawnCooldown--;
            }
        }
        public double[] MagCalc(Thing t)
        {
            try
            {
                double diffX = t.GetX() - x;
                double diffY = y - t.GetY();
                double magnitude = (double)Math.Sqrt(diffX * diffX + diffY * diffY);
                return new double[3] { magnitude, diffX, diffY };
            }
            catch
            {
                return new double[3] { double.MaxValue, 0, 0 }; //Some sort of inexplicable error has ooccured so Im returning max magnitude.
            }
        }
        public int GetCooldown()
        {
            return spawnCooldown;
        }
        public void SetSpawnCooldown()
        {

        }
    }
}
