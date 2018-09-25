using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tanky
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var g = new Game())
            {
                g.Run(20, 60);
            }
        }
    }

    class Game : GameWindow
    {
        private List<Entity> _entities = new List<Entity>();
        private Tank t1;
        private Tank t2;
        private float _floor = 500;

        private List<Key> _keysDown = new List<Key>();

        private bool _t1GoingLeft = true;
        private bool _t1Checked = false;

        private bool _t2GoingLeft = true;
        private bool _t2Checked = false;

        public Game():base(640, 480, new OpenTK.Graphics.GraphicsMode(32, 1, 0, 8), "Tanks")
        {
            t1 = new Tank(100, _floor);

            t2 = new Tank(ClientSize.Width - 100, _floor);
        }

        protected override void OnLoad(EventArgs e)
        {
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.Multisample);

            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
        }

        protected override void OnResize(EventArgs e)
        {
            var mat = Matrix4.CreateOrthographicOffCenter(0, Width, Height, 0, 0, 1);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref mat);

            GL.Viewport(ClientRectangle);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.ClearColor(1, 1, 1, 1);

            GameLoop();

            for (var index = 0; index < _entities.Count; index++)
            {
                var entity = _entities[index];
                entity.Render();
            }

            GL.Color3(0, 0, 0);
            GLHelper.DrawLine(0, _floor, ClientSize.Width, _floor, 1);

            GL.Color3(0, 0, 0);
            DrawBallisticCurve(t1);
            DrawBallisticCurve(t2);

            t1.Render();
            t2.Render();

            SwapBuffers();
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Space && !_keysDown.Contains(Key.Space))
            {
                var dir = t1.AimDir;
                var pos = t1.BarrelTipPos;

                //var mouse = PointToClient(Cursor.Position);
                //var mouseVec = new Vector2(mouse.X, mouse.Y);

                var dist = 1;//250 / (mouseVec - t1.Pos).Length();

                var p = new Projectile(pos.X, pos.Y, dir.X, dir.Y, dist * 0.00075f);

                _entities.Add(p);
            }

            if (e.Key == (Key)4 && !_keysDown.Contains((Key)4))
            {
                var dir = t2.AimDir;
                var pos = t2.BarrelTipPos;

                //var mouse = PointToClient(Cursor.Position);
                //var mouseVec = new Vector2(mouse.X, mouse.Y);

                var dist = 1;//250 / (mouseVec - t1.Pos).Length();

                var p = new Projectile(pos.X, pos.Y, dir.X, dir.Y, dist * 0.00075f);

                _entities.Add(p);
            }

            if (!_keysDown.Contains(e.Key))
                _keysDown.Add(e.Key);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.W || e.Key == Key.S)
                _t1Checked = false;
            if (e.Key == Key.Up || e.Key == Key.Down)
                _t2Checked = false;

            _keysDown.Remove(e.Key);
        }

        private void GameLoop()
        {
            if (_keysDown.Contains(Key.W))
            {
                if (!_t1Checked)
                {
                    _t1GoingLeft = t1.AimAngle <= 90;

                    _t1Checked = true;
                }

                t1.AimAngle += _t1GoingLeft ? 1 : -1;
            }
            if (_keysDown.Contains(Key.S))
            {
                if (!_t1Checked)
                {
                    _t1GoingLeft = t1.AimAngle <= 90;

                    _t1Checked = true;
                }

                t1.AimAngle -= _t1GoingLeft ? 1 : -1;
            }
            if (_keysDown.Contains(Key.A))
            {
                t1.Motion.X = Math.Max(t1.Motion.X - 0.75f, -15);
            }
            if (_keysDown.Contains(Key.D))
            {
                t1.Motion.X = Math.Min(t1.Motion.X + 0.75f, 15);
            }

            if (_keysDown.Contains(Key.Up))
            {
                if (!_t2Checked)
                {
                    _t2GoingLeft = t2.AimAngle <= 90;

                    _t2Checked = true;
                }

                t2.AimAngle += _t2GoingLeft ? 1 : -1;
            }
            if (_keysDown.Contains(Key.Down))
            {
                if (!_t2Checked)
                {
                    _t2GoingLeft = t2.AimAngle <= 90;

                    _t2Checked = true;
                }

                t2.AimAngle -= _t2GoingLeft ? 1 : -1;
            }
            if (_keysDown.Contains(Key.Left))
            {
                t2.Motion.X = Math.Max(t2.Motion.X - 0.75f, -15);
            }
            if (_keysDown.Contains(Key.Right))
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

            //var s = Mouse.GetState();

            // var mouse = PointToClient(new System.Drawing.Point(s.X, s.Y));
        }

        private void DrawBallisticCurve(Tank tank)
        {
            List<float> curve = new List<float>();

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

                    var last = new Vector2(curve[curve.Count - 2], curve[curve.Count - 1]);
                    var dir = Vector2.Normalize(last - vec);

                    last += dir * (last.Y - _floor);

                    vec.X = last.X;
                    vec.Y = last.Y;

                    curve.Add(vec.X);
                    curve.Add(vec.Y);
                    break;
                }

                curve.Add(vec.X);
                curve.Add(vec.Y);

                t += 5;
            }
            
            GLHelper.DrawLines(1, curve.ToArray());
        }
    }

    class Projectile : Entity
    {
        private float _time;
        private float _timeStep = 7;

        private float _gravity = 0.00075f;

        private Vector2 _shotDir;

        private Vector2 _shotPos;

        public Projectile(float x, float y, float dirX, float dirY, float gravity) : base(x, y)
        {
            _shotDir = new Vector2(dirX, dirY);
            _shotPos = Pos;
            _gravity = gravity;

            new Thread(() =>
            {
                while (true)
                {
                    Move();
                    Thread.Sleep(8);
                }
            })
            { IsBackground = true }.Start();
        }

        public override void Update()
        {

        }

        private void Move()
        {
            _time += _timeStep;

            Pos = MathUtil.BallisticCurve(_shotPos, _shotDir, _gravity, _time);

            if (Pos.Y >= 500)
            {
                Pos.Y = 500;

                _timeStep *= 0.925f;

                CanCollide = false;
            }
        }

        public override void Render()
        {
            GLHelper.FillEllipse(Pos.X - 5, Pos.Y - 5, 10, 10);
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

        public virtual void Render()
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

        public override void Render()
        {
            var pos = Pos;

            pos.Y -= 38;

            GL.Color3(0, 0, 1f);
            GLHelper.DrawLine(pos.X, pos.Y, BarrelTipPos.X, BarrelTipPos.Y, 8);

            GL.Color3(1f, 0, 0);
            GLHelper.FillEllipse(pos.X - 20, pos.Y - 20, 40, 40);

            GL.Color3(0, 0, 0);
            GLHelper.FillRectangle(pos.X - 50, pos.Y, 100, 38);
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

    static class GLHelper
    {
        public static void DrawLine(float x1, float y1, float x2, float y2, float w)
        {
            GL.LineWidth(w);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex2(x1, y1);
            GL.Vertex2(x2, y2);
            GL.End();
            GL.LineWidth(1);
        }

        public static void DrawLines(float w, params float[] values)
        {
            GL.LineWidth(w);
            GL.Begin(PrimitiveType.Lines);
            for (int i = 0; i < values.Length; i += 2)
            {
                GL.Vertex2(values[i], values[i + 1]);
            }
            GL.End();
            GL.LineWidth(1);
        }

        public static void FillEllipse(float x, float y, float w, float h)
        {
            GL.Begin(PrimitiveType.Polygon);

            w *= 0.5f;
            h *= 0.5f;

            for (int i = 0; i < 25; i++)
            {
                var a = i / 25f * MathHelper.TwoPi;

                var vx = x + w + Math.Cos(a) * w;
                var vy = y + h + -Math.Sin(a) * h;

                GL.Vertex2(vx, vy);
            }
            GL.End();
        }

        public static void FillRectangle(float x, float y, float w, float h)
        {
            GL.Begin(PrimitiveType.Quads);
            GL.Vertex2(x, y);
            GL.Vertex2(x, y + h);
            GL.Vertex2(x + w, y + h);
            GL.Vertex2(x + w, y);
            GL.End();
        }
    }
}
