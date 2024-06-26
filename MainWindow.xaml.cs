﻿using System;
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
        Timer t; Random r = new Random();                       
        List<Thing> things = new List<Thing> (), thingsToAdd = new List<Thing>(), thingsToRem = new List<Thing>();
        public MainWindow()
        {
            InitializeComponent();
            double maxX = Width, maxY = Height,x,y;
            for(int i = 0; i < r.Next(15, 20); i++)
            {
                x = r.NextDouble()*maxX;
                y = r.NextDouble()*maxY;
                if (r.Next(0, 3) <= 1)
                {
                    AddThing(new Prey(20, x, y));
                }
                else
                {
                    AddThing(new Predator(20, x, y));
                }
            }
            SetTimers();
        }
        private void SetTimers()
        {
            t = new Timer(100);
            t.Elapsed += TimerEvent;
            t.Enabled = true;
        }
        private bool IsPrey(Thing t)
        {
            return t.GetType() == typeof(Prey);
        }
        private void SetTargets()
        {
            double minMag;
            Prey tempPrey = null;
            bool isPrey1, isPrey2;
            Predator tempPred = null;
            foreach(Thing thing1 in things)
            {
                minMag = double.PositiveInfinity;
                isPrey1 = IsPrey(thing1);
                foreach(Thing thing2 in things)
                {
                    isPrey2 = IsPrey(thing2);
                    if (thing1 != thing2)
                    {
                        if (thing1.MagCalc(thing2)[0] < minMag)
                        {
                            if (isPrey2)
                            {
                                tempPrey = (Prey)thing2;
                            }
                            else if (isPrey1)//(and thing2 is a predator)
                            {
                                tempPred = (Predator)thing2;
                            }
                        }
                    }
                }
                thing1.Target = tempPrey;
                if (isPrey1) { ((Prey)thing1).Pred = tempPred; }
            }
        }
        private void CheckCollisions()
        {
            bool isPrey;
            foreach (Thing t in things)
            {
                isPrey = IsPrey(t);
                if (t.GetCollision()&&(!isPrey||((Prey)t).GetCooldown() <= 0))
                {
                    double newX = (t.X + t.Target.X) / 2;
                    double newY = (t.Y + t.Target.Y) / 2;
                    thingsToAdd.Add(new Prey(t.Size, newX, newY));
                    if (!isPrey)
                    {
                        thingsToRem.Add(t.Target);
                        if(((Predator)t).GetTtl() <= 0)
                        {
                            thingsToRem.Add(t);
                        }
                    }
                    t.OffCollision();
                }
            }
        }
        private void RemoveStuff()
        {
            if (thingsToRem.Count > 0)
            {
                foreach (Thing t in thingsToRem)
                {
                    things.Remove(t);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        canvas.Children.Remove(t.Shape);
                    });
                }
                thingsToRem.Clear();
            }
        }
        private void AddStuff()
        {
            if(thingsToAdd.Count > 0)
            {
                foreach(Thing t in thingsToAdd)
                {
                    AddThing(t);
                }
                thingsToAdd.Clear();
            }
        }
        private void TimerEvent(Object source, ElapsedEventArgs e)
        {
            SetTargets();
            CheckCollisions();
            AddStuff();
            RemoveStuff();
            foreach(Thing t in things)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    t.Shape.RenderTransform = t.Move();
                });
            }
        }
        private void AddThing(Thing thing)
        {
            things.Add(thing);
            canvas.Children.Add(thing.Shape);
        }
    }
    public abstract class Thing
    {
        protected double size, x, y, minDist, speed;
        protected SolidColorBrush brush = new SolidColorBrush();
        protected Ellipse shape = new Ellipse();
        protected Prey target;
        protected bool closeToPrey = false;

        public Ellipse Shape { get { return shape; } }
        public double Size { get { return size; } }
        public double X { get { return x; } }
        public double Y { get { return x; } }
        protected abstract void ChangeXY();
        public void OnCollision() { closeToPrey = true; }
        public void OffCollision() { closeToPrey = false; }
        public bool GetCollision() { return closeToPrey; }

        public Thing(double newSize, double newX, double newY)
        {
            size = newSize;
            x = newX;
            y = newY;
            shape.Height = size;
            shape.Width = size;
        }
        public Prey Target
        {
            get { return target; }
            set { target = value; }
        }
        public TranslateTransform Move()
        {
            ChangeXY();
            return new TranslateTransform(x, y);
        }
        public double[] MagCalc(Thing t)
        {
            try
            {
                double diffX = t.X - x;
                double diffY = y - t.Y;
                double magnitude = (double)Math.Sqrt(diffX * diffX + diffY * diffY);
                return new double[3] { magnitude, diffX, diffY };
            }
            catch
            {
                return new double[3] { double.MaxValue, 0, 0 }; //Some sort of inexplicable error has occured so Im returning max magnitude.
            }
        }
    }
    public class Predator : Thing
    {
        int ttl;
        public Predator(double newSize, double newX, double newY) : base(newSize, newX, newY)
        {
            brush.Color = Colors.Red;
            shape.Fill = brush;
            minDist = 20;
            speed = 3;
            ttl = 100;
        }
        protected override void ChangeXY()
        {
            double diffX = target.X - x;
            double diffY = y - target.Y;
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
            spawnCooldown = 10;
        }
        private Predator predator;
        public Predator Pred { set { predator = value; } }
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
        public int GetCooldown()
        {
            return spawnCooldown;
        }
        public void SetSpawnCooldown()
        {
            spawnCooldown =15;
        }
    }
}
