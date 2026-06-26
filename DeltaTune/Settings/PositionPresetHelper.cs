using System;
using Microsoft.Xna.Framework;

namespace DeltaTune.Settings
{
    public class PositionPresetHelper
    {
        private static readonly Vector2[] presetPositions = new Vector2[6];

        static PositionPresetHelper()
        {
            presetPositions[(int)PositionPreset.TopLeft] = new Vector2(0.025f, 0.025f);
            presetPositions[(int)PositionPreset.TopRight] = new Vector2(0.975f, 0.025f);
            presetPositions[(int)PositionPreset.BottomLeft] = new Vector2(0.025f, 0.975f);
            presetPositions[(int)PositionPreset.BottomRight] = new Vector2(0.975f, 0.975f);
            presetPositions[(int)PositionPreset.Chapter1] = new Vector2(0.96f, 0.22f);
            presetPositions[(int)PositionPreset.Chapter5] = new Vector2(0.975f, 0.7f);
        }

        public static Vector2 GetFractionalPosition(PositionPreset preset)
        {
            return presetPositions[(int)preset];
        }

        public static PositionPreset GetPositionPreset(Vector2 preset)
        {
            if(preset == presetPositions[(int)PositionPreset.TopLeft]) return PositionPreset.TopLeft;
            if(preset == presetPositions[(int)PositionPreset.TopRight]) return PositionPreset.TopRight;
            if(preset == presetPositions[(int)PositionPreset.BottomLeft]) return PositionPreset.BottomLeft;
            if(preset == presetPositions[(int)PositionPreset.BottomRight]) return PositionPreset.BottomRight;
            if(preset == presetPositions[(int)PositionPreset.Chapter1]) return PositionPreset.Chapter1;
            if(preset == presetPositions[(int)PositionPreset.Chapter5]) return PositionPreset.Chapter5;
            
            throw new ArgumentOutOfRangeException(nameof(preset));
        }
    }
}