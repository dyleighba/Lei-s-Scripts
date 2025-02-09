using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace IngameScript
{
    public class ButtonBar
    {
        private List<IMyTextSurface> buttons = new List<IMyTextSurface>();
        
        public ButtonBar(IMyTextSurfaceProvider buttonPanelsurfaceProvider)
        {
            if (buttonPanelsurfaceProvider == null)
            {
                throw new System.ArgumentNullException(nameof(buttonPanelsurfaceProvider));
            }
            if (buttonPanelsurfaceProvider.SurfaceCount != 4)
            {
                throw new System.ArgumentException($"Button panel surface count must be 4 ({buttonPanelsurfaceProvider.SurfaceCount})");
            }

            for (int i = 0; i < 4; i++)
            {
                IMyTextSurface textSurface = buttonPanelsurfaceProvider.GetSurface(i);
                buttons.Add(textSurface);
                textSurface.Font = "Monospace";
                textSurface.ContentType = ContentType.TEXT_AND_IMAGE;
                textSurface.FontSize = 6.00f;
                textSurface.TextPadding = 12f;
                textSurface.Alignment = TextAlignment.CENTER;
                textSurface.BackgroundColor = new Color(0.03f, 0.03f, 0.03f); // Dull grey for screen effect
                textSurface.FontColor = new Color(0f, 0.588f, 1f); // Starfleet blue
            }
        }

        public void RenameButtons(string[] buttonNames)
        {
            if (buttonNames == null)
            {
                throw new System.ArgumentNullException(nameof(buttonNames));
            }
            if (buttonNames.Length != 4)
            {
                throw new System.ArgumentException("buttonNames length must be 4");
            }

            for (int i = 0; i < 4; i++)
            {
                buttons[i].WriteText(buttonNames[i]);
            }
        }
    }
}