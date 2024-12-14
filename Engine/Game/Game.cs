using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Engine.Core.ECS;
using Engine.Core.Physics;
using Engine.Core;
using Engine.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using Engine.Core.Rendering;
using Engine.Core.Components;
using Engine.Core.Components.Rendering;
using Engine.Core.Components.Physics;
using Aether.Animation;
using Myra.Graphics2D.UI;
using Myra;
namespace Engine.Game
{
    public class Game : EngineManager
    {
        private Entity CameraEntity;
        private Entity PlayerEntity;
        private LightComponent DirectionalLight;
        public override void Awake()
        {
            Debug = true;
        }
        public override void Start()
        {
            LightManager.Instance.AmbientColor = new Color(.2f,.2f,.2f);
            CreateCamera();
            CreatePlayer();
            DirectionalLight = CreateDirectionalLight();
            Entity ChunkEntity = ECSManager.Instance.CreateEntity();
            ECSManager.Instance.AddComponent(ChunkEntity, new ChunkManager
            {
                Cam = CameraEntity
            });
        }


        public override void MainUpdate(GameTime GameTime)
        {
        }

        public override void FixedUpdate(GameTime GameTime)
        {

        }


        public override void Render(GameTime GameTime)
        {
            Graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            BasicEffect Effect = new BasicEffect(Graphics.GraphicsDevice) { AmbientLightColor = Vector3.One / 4 };
            Camera CameraObj = ECSManager.Instance.GetComponent<Camera>(CameraEntity);

            if (CameraObj != null)
            {
                Effect.View = CameraObj.GetViewMatrix();
                Effect.Projection = CameraObj.GetProjectionMatrix();
                ECSManager.Instance.CallRenderOnComponents(Effect, CameraObj.GetViewMatrix(), CameraObj.GetProjectionMatrix(), GameTime);
            }
        }

        //Spawning Objects

        private void CreateCamera()
        {
            CameraEntity = ECSManager.Instance.CreateEntity();
            Camera Cam = new Camera();
            ECSManager.Instance.AddComponent(CameraEntity, Cam);
        }

        private LightComponent CreateDirectionalLight()
        {
            Entity LightEntity = ECSManager.Instance.CreateEntity();
            LightComponent Light = new LightComponent
            {
                Color = Color.White,
                Intensity = 1
            };
            ECSManager.Instance.AddComponent(LightEntity, Light);
            Light.Transform.Rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(45), MathHelper.ToRadians(45), 0);
            return Light;
        }

        private void CreatePlayer()
        {
            PlayerEntity = ECSManager.Instance.CreateEntity();

            PlayerEntity.Transform.Position = Vector3.Up * 50;
            Player Controller = new Player()
            {
                CameraObj = ECSManager.Instance.GetComponent<Camera>(CameraEntity),
            };
            ECSManager.Instance.AddComponent(PlayerEntity, Controller);
        }
    }
}
