using UnityEngine;

namespace Code.Sequences.Guide
{
    public enum MousePart
    {
        Body,
        LeftButton,
        RightButton,
    }

    /// <summary>
    /// 안내 손이 쓰는 그림을 런타임에 픽셀로 그립니다. 에셋도 폰트도 필요 없게 하기 위해서입니다.
    /// 전부 흰색이라 색은 Image.color로 입힙니다.
    /// </summary>
    public static class GuideCursorArt
    {
        private const int MouseWidth = 32;
        private const int MouseHeight = 44;
        private const int MouseCorner = 10;
        private const int MouseOutline = 2;
        private const float ButtonSplit = 0.55f;   // 아래에서 이 비율 위가 버튼 두 개

        private static Sprite _solid;

        /// <summary>
        /// 1픽셀 흰 사각형입니다. 선·상자 테두리를 늘려 그릴 때 씁니다.
        /// </summary>
        public static Sprite Solid()
        {
            if (_solid != null)
                return _solid;

            Texture2D texture = NewTexture(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            return _solid = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// 클릭 순간 퍼지는 원 테두리입니다.
        /// </summary>
        public static Sprite Ring(int size, int thickness)
        {
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            float center = size * 0.5f;
            float outer = center - 1f;
            float inner = outer - thickness;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                float alpha = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// 마우스 픽토그램의 한 부분입니다. 몸통 테두리, 왼쪽 버튼 면, 오른쪽 버튼 면을 따로 그려 각각 색을 입힙니다.
        /// </summary>
        public static Sprite Mouse(MousePart part)
        {
            Texture2D texture = NewTexture(MouseWidth, MouseHeight);
            Color32[] pixels = new Color32[MouseWidth * MouseHeight];
            int splitY = Mathf.RoundToInt(MouseHeight * ButtonSplit);
            int half = MouseWidth / 2;

            for (int y = 0; y < MouseHeight; y++)
            for (int x = 0; x < MouseWidth; x++)
            {
                bool inside = InRoundedRect(x, y, 0);
                bool inner = InRoundedRect(x, y, MouseOutline);
                bool divider = y > splitY && x >= half - 1 && x <= half;
                bool split = y >= splitY - 1 && y <= splitY;

                bool on = part switch
                {
                    MousePart.Body => inside && (!inner || divider || split),
                    MousePart.LeftButton => inner && y > splitY + 1 && x < half - 1,
                    MousePart.RightButton => inner && y > splitY + 1 && x > half,
                    _ => false
                };

                pixels[y * MouseWidth + x] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, MouseWidth, MouseHeight), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// 키캡 바탕입니다. 모서리가 둥근 흰 사각형입니다.
        /// </summary>
        public static Sprite RoundedBox(int size, int radius)
        {
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool on = InRoundedRect(x, y, 0, size, size, radius);
                pixels[y * size + x] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// 오른쪽을 가리키는 삼각형입니다. 방향키 키캡에 회전해서 씁니다 — 화살표 글리프는 폰트에 없을 수 있습니다.
        /// </summary>
        public static Sprite ArrowRight(int size)
        {
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // 왼쪽 변이 밑변, 오른쪽 꼭짓점이 끝. x가 커질수록 허용 높이가 줄어든다
                float allowed = half * (1f - x / (float)size);
                bool on = x >= size * 0.15f && x <= size * 0.85f && Mathf.Abs(y + 0.5f - half) <= allowed * 0.9f;
                pixels[y * size + x] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// 게임 커서 텍스처를 스프라이트로 바꿉니다. 피벗을 핫스팟에 두어 포인터 끝이 좌표에 오게 합니다.
        /// </summary>
        public static Sprite Pointer(Texture2D texture, Vector2 hotspot)
        {
            Vector2 pivot = new(hotspot.x / texture.width, 1f - hotspot.y / texture.height);
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivot, 100f);
        }

        private static bool InRoundedRect(int x, int y, int shrink)
            => InRoundedRect(x, y, shrink, MouseWidth, MouseHeight, MouseCorner);

        private static bool InRoundedRect(int x, int y, int shrink, int width, int height, int radius)
        {
            float px = x + 0.5f, py = y + 0.5f;
            float left = shrink, right = width - shrink, bottom = shrink, top = height - shrink;
            if (px < left || px > right || py < bottom || py > top)
                return false;

            float r = Mathf.Max(0f, radius - shrink);
            float dx = Mathf.Max(0f, Mathf.Max(left + r - px, px - (right - r)));
            float dy = Mathf.Max(0f, Mathf.Max(bottom + r - py, py - (top - r)));
            return dx * dx + dy * dy <= r * r;
        }

        private static Texture2D NewTexture(int width, int height)
            => new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "GuideCursorArt"
            };
    }
}
