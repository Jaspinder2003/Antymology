using UnityEngine;
using Antymology.Terrain;
using Antymology.Components.Agents;

namespace Antymology.Components.UI
{
    public class NestUI : MonoBehaviour
    {
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private bool _stylesInitialized = false;

        private void InitStyles()
        {
            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.7f));
            _boxStyle.padding = new RectOffset(10, 10, 10, 10);

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 16;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = Color.white;

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 13;
            _labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            _stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = color;
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            if (!_stylesInitialized) InitStyles();

            float panelWidth = 240f;
            float panelHeight = 200f;

            GUILayout.BeginArea(new Rect(10, 10, panelWidth, panelHeight), _boxStyle);

            GUILayout.Label("Antymology", _headerStyle);
            GUILayout.Space(4);

            // Nest block count
            int nestCount = WorldManager.Instance != null ? WorldManager.Instance.NestBlockCount : 0;
            GUILayout.Label($"Nest Blocks:  {nestCount}", _labelStyle);

            // Generation info
            if (Configuration.EvolutionManager.Instance != null)
            {
                var evo = Configuration.EvolutionManager.Instance;
                GUILayout.Label($"Generation:   {evo.GenerationCount}", _labelStyle);
                GUILayout.Label($"Time Left:    {evo.TimeRemaining:F1}s", _labelStyle);
            }

            // Ant count
            int antCount = AntManager.Instance != null ? AntManager.Instance.AntCount : 0;
            GUILayout.Label($"Ants Alive:   {antCount}", _labelStyle);

            // Mulch consumed
            int mulch = AntManager.Instance != null ? AntManager.Instance.MulchConsumed : 0;
            GUILayout.Label($"Mulch Eaten:  {mulch}", _labelStyle);

            // Ants on acid
            int onAcid = AntManager.Instance != null ? AntManager.Instance.AntsOnAcid : 0;
            GUILayout.Label($"Ants on Acid: {onAcid}", _labelStyle);

            GUILayout.EndArea();
        }
    }
}
