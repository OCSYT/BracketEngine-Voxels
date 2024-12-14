using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Engine.Core.ECS;
using Engine.Core.Physics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Components.Physics;
using Engine.Core.Components.Rendering;

namespace Engine.Components
{
    public class Player : Component
    {
        public Camera CameraObj;
        public float Sensitivity = 1;
        private float MouseX = 0;
        private float MouseY = 0;
        public float Speed = 100;
        private Vector3 ForwardDir = Vector3.Forward;
        private Vector3 RightDir = Vector3.Right;
        public Player()
        {

        }
        public Player(RigidBody Body, Camera CameraObj, float Sensitivity = 1, float Speed = 50, float Jump = 5, float MaxVelocity = 100, float Height = 5)
        {
            this.CameraObj = CameraObj;
            this.Sensitivity = Sensitivity;
            this.Speed = Speed;
        }

        public override void Start()
        {

        }

        public override void MainUpdate(GameTime gameTime)
        {
            if (!EngineManager.Instance.IsActive) return;

            GraphicsDeviceManager Graphics = EngineManager.Instance.Graphics;

            Vector2 CurrentMousePosition = Mouse.GetState().Position.ToVector2();
            Vector2 MouseDelta = CurrentMousePosition - new Vector2(Graphics.GraphicsDevice.Viewport.Width / 2, Graphics.GraphicsDevice.Viewport.Height / 2);

            MouseX -= MouseDelta.X * (Sensitivity / 100000);
            MouseY = MathHelper.Clamp(MouseY - MouseDelta.Y * (Sensitivity / 100000), MathHelper.ToRadians(-90), MathHelper.ToRadians(90));

            CameraObj.Transform.Rotation = Quaternion.CreateFromYawPitchRoll(MouseX, MouseY, 0);
            Quaternion BodyDir = Quaternion.CreateFromYawPitchRoll(MouseX, 0, 0);
            ForwardDir = Vector3.Transform(Vector3.Forward, BodyDir);
            RightDir = Vector3.Transform(Vector3.Right, BodyDir);
        }

        public override void FixedUpdate(GameTime GameTime)
        {

            KeyboardState State = Keyboard.GetState();

            Vector3 LocalVel = Vector3.Zero;
            if (EngineManager.Instance.IsActive)
            {
                EngineManager.Instance.LockMouse();
                if (State.IsKeyDown(Keys.W))
                {
                    LocalVel += ForwardDir * Speed;
                }
                if (State.IsKeyDown(Keys.S))
                {
                    LocalVel -= ForwardDir * Speed;
                }
                if (State.IsKeyDown(Keys.A))
                {
                    LocalVel -= RightDir * Speed;
                }
                if (State.IsKeyDown(Keys.D))
                {
                    LocalVel += RightDir * Speed;
                }
                if (State.IsKeyDown(Keys.Space))
                {
                    LocalVel += Vector3.Up * Speed;
                }
                if (State.IsKeyDown(Keys.LeftShift)){
                    LocalVel -= Vector3.Up * Speed;
                }
            }
            Transform.Position += LocalVel * (float)GameTime.ElapsedGameTime.TotalSeconds;


            CameraObj.Transform.Position = Transform.Position;
        }
    }
}
