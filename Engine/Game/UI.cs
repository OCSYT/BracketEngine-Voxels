using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine.Core;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Engine.Game
{
    public class UI
    {
        public Desktop UIDesktop;
        private Label FPSLabel;
        public Label BlockLabel;
        public void Start()
        {
            Panel MainPanel = new Panel();
            FPSLabel = new Label
            {
                Text = "FPS: 0"
            };
            MainPanel.Widgets.Add(FPSLabel);

            BlockLabel = new Label
            {
                Text = "Block",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Scale = new Vector2(2, 2),
                Padding = new Thickness(0, 10)
            };
            MainPanel.Widgets.Add(BlockLabel);

            UIDesktop = new Desktop();
            UIDesktop.Root = MainPanel;

        }
        public void Render(GameTime GameTime)
        {
            FPSLabel.Text = "FPS: " + MathF.Round(1/(float)GameTime.ElapsedGameTime.TotalSeconds);
            UIDesktop.Render();
        }
    }
}
