using UnityEngine;

namespace QuestDemonMR
{
    public static class PixelArtFactory
    {
        private static Sprite _demonSprite;

        public static Sprite GetDemonSprite()
        {
            if (_demonSprite != null)
            {
                return _demonSprite;
            }

            _demonSprite = Resources.Load<Sprite>("Art/emberfiend-sprite-v2");
            if (_demonSprite != null)
            {
                return _demonSprite;
            }

            const int width = 32;
            const int height = 48;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Emberfiend_RuntimeSprite",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            var coal = new Color32(34, 31, 43, 255);
            var slate = new Color32(65, 60, 76, 255);
            var ember = new Color32(255, 70, 12, 255);
            var flame = new Color32(255, 170, 20, 255);
            var cyan = new Color32(47, 255, 226, 255);
            var bone = new Color32(158, 126, 103, 255);

            Rect(pixels, width, 10, 28, 12, 12, coal);
            Rect(pixels, width, 8, 24, 16, 8, slate);
            Rect(pixels, width, 6, 17, 20, 10, coal);
            Rect(pixels, width, 9, 9, 6, 10, slate);
            Rect(pixels, width, 17, 9, 6, 10, slate);
            Rect(pixels, width, 7, 2, 7, 8, coal);
            Rect(pixels, width, 18, 2, 7, 8, coal);
            Rect(pixels, width, 2, 17, 5, 15, slate);
            Rect(pixels, width, 25, 17, 5, 15, slate);
            Rect(pixels, width, 6, 36, 4, 9, bone);
            Rect(pixels, width, 22, 36, 4, 9, bone);
            Rect(pixels, width, 3, 41, 4, 5, bone);
            Rect(pixels, width, 25, 41, 4, 5, bone);
            Rect(pixels, width, 12, 32, 3, 3, cyan);
            Rect(pixels, width, 18, 32, 3, 3, cyan);
            Rect(pixels, width, 14, 25, 4, 2, flame);
            Rect(pixels, width, 14, 19, 4, 6, ember);
            Rect(pixels, width, 7, 20, 3, 2, ember);
            Rect(pixels, width, 22, 20, 3, 2, ember);
            Rect(pixels, width, 10, 12, 3, 2, ember);
            Rect(pixels, width, 19, 12, 3, 2, ember);
            Rect(pixels, width, 7, 3, 7, 2, ember);
            Rect(pixels, width, 18, 3, 7, 2, ember);

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            _demonSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), 32f, 0, SpriteMeshType.FullRect);
            _demonSprite.name = "Emberfiend_RuntimeSprite";
            return _demonSprite;
        }

        private static void Rect(Color32[] pixels, int width, int x, int y, int rectWidth, int rectHeight, Color32 color)
        {
            for (var py = y; py < y + rectHeight; py++)
            {
                for (var px = x; px < x + rectWidth; px++)
                {
                    if (px >= 0 && px < width && py >= 0 && py < pixels.Length / width)
                    {
                        pixels[py * width + px] = color;
                    }
                }
            }
        }
    }
}
