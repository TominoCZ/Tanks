using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tanky
{
    public partial class Form1 : Form
    {
        private List<Entity> _entities = new List<Entity>();
        private Tank t1;
        private Tank t2;
        private float _floor = 400;

        private List<Keys> _keysDown = new List<Keys>();

        private bool _t1GoingLeft = true;
        private bool _t1Checked = false;

        private bool _t2GoingLeft = true;
        private bool _t2Checked = false;

        public Form1()
        {
            InitializeComponent();

            DoubleBuffered = true;

            t1 = new Tank(100, _floor);

            t2 = new Tank(ClientSize.Width - 100, _floor);
        }

        protected override void OnShown(EventArgs e)
        {
            new Thread(() =>
            {
                while (true)
                {
                    if (Created && IsHandleCreated && Visible && !Disposing)
                        BeginInvoke((MethodInvoker)Invalidate);

                    Thread.Sleep(15);
                }
            })
            { IsBackground = true }.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            GameLoop();

            for (var index = 0; index < _entities.Count; index++)
            {
                var entity = _entities[index];
                entity.Render(e.Graphics);
            }

            e.Graphics.DrawLine(Pens.Black, 0, _floor, ClientSize.Width, _floor);

            DrawBallisticCurve(t1, e.Graphics);
            DrawBallisticCurve(t2, e.Graphics);

            t1.Render(e.Graphics);
            t2.Render(e.Graphics);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && !_keysDown.Contains(Keys.Space))
            {
                var dir = t1.AimDir;
                var pos = t1.BarrelTipPos;

                //var mouse = PointToClient(Cursor.Position);
                //var mouseVec = new Vector2(mouse.X, mouse.Y);

                var dist = 1;//250 / (mouseVec - t1.Pos).Length();

                var p = new Projectile(pos.X, pos.Y, dir.X, dir.Y, dist * 0.00075f);

                _entities.Add(p);
            }

            if (e.KeyCode == Keys.NumPad1 && !_keysDown.Contains(Keys.NumPad1))
            {
                var dir = t2.AimDir;
                var pos = t2.BarrelTipPos;

                //var mouse = PointToClient(Cursor.Position);
                //var mouseVec = new Vector2(mouse.X, mouse.Y);

                var dist = 1;//250 / (mouseVec - t1.Pos).Length();

                var p = new Projectile(pos.X, pos.Y, dir.X, dir.Y, dist * 0.00075f);

                _entities.Add(p);
            }

            if (!_keysDown.Contains(e.KeyCode))
                _keysDown.Add(e.KeyCode);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W || e.KeyCode == Keys.S)
                _t1Checked = false;
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
                _t2Checked = false;

            _keysDown.Remove(e.KeyCode);
        }
        
        private void GameLoop()
        {
            if (_keysDown.Contains(Keys.W))
            {
                if (!_t1Checked)
                {
                    _t1GoingLeft = t1.AimAngle <= 90;

                    _t1Checked = true;
                }

                t1.AimAngle += _t1GoingLeft ? 1 : -1;
            }
            if (_keysDown.Contains(Keys.S))
            {
                if (!_t1Checked)
                {
                    _t1GoingLeft = t1.AimAngle <= 90;

                    _t1Checked = true;
                }

                t1.AimAngle -= _t1GoingLeft ? 1 : -1;
            }
            if (_keysDown.Contains(Keys.A))
            {
                t1.Motion.X = Math.Max(t1.Motion.X - 0.75f, -15);
            }
            if (_keysDown.Contains(Keys.D))
            {
                t1.Motion.X = Math.Min(t1.Motion.X + 0.75f, 15);
            }

            if (_keysDown.Contains(Keys.Up))
            {
                if (!_t2Checked)
                {
                    _t2GoingLeft = t2.AimAngle <= 90;

                    _t2Checked = true;
                }

                t2.AimAngle += _t2GoingLeft ? 1 : -1;
            }
            if (_keysDown.Contains(Keys.Down))
            {
                if (!_t2Checked)
                {
                    _t2GoingLeft = t2.AimAngle <= 90;

                    _t2Checked = true;
                }

                t2.AimAngle -= _t2GoingLeft ? 1 : -1;
            }
            if (_keysDown.Contains(Keys.Left))
            {
                t2.Motion.X = Math.Max(t2.Motion.X - 0.75f, -15);
            }
            if (_keysDown.Contains(Keys.Right))
            {
                t2.Motion.X = Math.Min(t2.Motion.X + 0.75f, 15);
            }

            for (var index = 0; index < _entities.Count; index++)
            {
                var entity = _entities[index];
                entity.Update();
            }

            t1.Update();
            t2.Update();

            var mouse = PointToClient(Cursor.Position);
        }

        private void DrawBallisticCurve(Tank tank, Graphics g)
        {
            List<PointF> curve = new List<PointF>();

            var t = 0;

            //var mouse = PointToClient(Cursor.Position);
            //var mouseVec = new Vector2(mouse.X, mouse.Y);

            var dist = 1;//250 / (mouseVec - t1.Pos).Length();

            while (true)
            {
                var vec = MathUtil.BallisticCurve(tank.BarrelTipPos, tank.AimDir, 0.00075f * dist, t);

                if (vec.Y >= _floor && curve.Count > 0)
                {
                    var pf = curve.Last();

                    var last = new Vector2(pf.X, pf.Y);
                    var dir = Vector2.Normalize(last - vec);

                    last += dir * (last.Y - _floor);

                    vec.X = last.X;
                    vec.Y = last.Y;

                    curve.Add(new PointF(vec.X, vec.Y));
                    break;
                }

                curve.Add(new PointF(vec.X, vec.Y));

                t += 5;
            }

            Pen p = new Pen(Color.FromArgb(80, Color.Black), 1)
            {
                DashStyle = DashStyle.Custom,
                DashPattern = new float[] { 10, 5 },
                DashCap = DashCap.Round
            };

            g.DrawLines(p, curve.ToArray());
        }
    }

    class Projectile : Entity
    {
        private float _time;
        private float _timeStep = 14;

        private float _gravity = 0.00075f;

        private Vector2 _shotDir;

        private Vector2 _shotPos;

        public Projectile(float x, float y, float dirX, float dirY, float gravity) : base(x, y)
        {
            _shotDir = new Vector2(dirX, dirY);
            _shotPos = Pos;
            _gravity = gravity;
        }

        public override void Update()
        {
            _time += _timeStep;

            Pos = MathUtil.BallisticCurve(_shotPos, _shotDir, _gravity, _time);

            if (Pos.Y >= 400)
            {
                Pos.Y = 400;

                _timeStep *= 0.925f;

                CanCollide = false;
            }
        }

        public override void Render(Graphics g)
        {
            g.FillEllipse(Brushes.Black, Pos.X - 5, Pos.Y - 5, 10, 10);
        }
    }

    class Entity
    {
        public Vector2 Pos;
        public Vector2 Motion;

        public bool CanCollide = true;

        protected Entity(float x, float y)
        {
            Pos = new Vector2(x, y);
        }

        public virtual void Update()
        {
            Pos += Motion;

            Motion *= 0.85f;
        }

        public virtual void Render(Graphics g)
        {

        }
    }

    class Tank : Entity
    {
        private float _aimAngle;

        public float AimAngle
        {
            get
            {
                return _aimAngle;
            }
            set
            {
                _aimAngle = Math.Max(Math.Min(value, 180), 0);
                var rad = Math.PI / 180 * _aimAngle;

                Console.WriteLine(_aimAngle);

                var x = (float)Math.Cos(rad);
                var y = (float)Math.Sin(rad);

                AimDir = new Vector2(x, -y);
            }
        }
        public Vector2 AimDir;
        public Vector2 BarrelTipPos;

        public float BarrelLength = 20;

        public Tank(float x, float y) : base(x, y)
        {
            AimAngle = 45;
        }

        public override void Update()
        {
            base.Update();

            var center = new Vector2(Pos.X, Pos.Y - 38);

            BarrelTipPos = center + AimDir * (BarrelLength + 20);
        }

        public override void Render(Graphics g)
        {
            var pos = Pos;

            pos.Y -= 38;

            var p = new Pen(Color.Blue, 8);

            g.DrawLine(p, pos.X, pos.Y, BarrelTipPos.X, BarrelTipPos.Y);
            g.FillEllipse(Brushes.Red, pos.X - 20, pos.Y - 20, 40, 40);
            g.FillRectangle(Brushes.Black, pos.X - 50, pos.Y, 100, 38);
        }
    }

    class MathUtil
    {
        public static Vector2 BallisticCurve(Vector2 from, Vector2 dir, float gravity, float time)
        {
            var a = -Math.Atan2(dir.Y, dir.X);

            var vx = (float)Math.Cos(a);
            var vy = (float)-Math.Sin(a);

            var vec = new Vector2(vx, vy + gravity * time) * time;

            return from + vec;
        }
    }
}
