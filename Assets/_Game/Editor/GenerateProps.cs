using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarkwoodEditor
{
    /// <summary>
    /// Генерирует пропсы-плейсхолдеры. Сейчас: костёр (спрайт-объект)
    /// и выжженная земля под него (тайл для палитры).
    /// Запуск: Tools → Игра → Сгенерировать костёр.
    /// </summary>
    public static class GenerateProps
    {
        const string TexDir = "Assets/_Game/Textures";
        const string TilesDir = TexDir + "/Tiles";

        [MenuItem("Tools/Игра/Сгенерировать костёр")]
        public static void Campfire()
        {
            EnsureDirs();
            GenerateCampfireSprite();
            GenerateBurnTile();
            AssetDatabase.Refresh();
            Debug.Log("<b>PROPS_CAMPFIRE_OK</b> — campfire.png (спрайт) и tile_burn.png (тайл) готовы.");
        }

        static void EnsureDirs()
        {
            if (!Directory.Exists(TexDir)) Directory.CreateDirectory(TexDir);
            if (!Directory.Exists(TilesDir)) Directory.CreateDirectory(TilesDir);
        }

        // ------------------------------------------------------------------
        // Костёр: 48×64, пивот у ног (Bottom) — как у игрока, для Y-сортировки
        // ------------------------------------------------------------------
        static void GenerateCampfireSprite()
        {
            const int W = 48, H = 64;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false, false);
            var px = new Color32[W * H];

            var stone = new Color32(92, 92, 98, 255);
            var stoneDark = new Color32(68, 68, 74, 255);
            var log = new Color32(74, 52, 36, 255);
            var ember = new Color32(255, 190, 90, 255);
            var flameOuter = new Color32(196, 78, 28, 255);
            var flameMid = new Color32(238, 140, 42, 255);
            var flameCore = new Color32(255, 224, 148, 255);

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    Color32 c = new Color32(0, 0, 0, 0);

                    // 1) камни вокруг
                    if (InStone(x, y))
                        c = Noise(x, y, 31) > 0.5f ? stone : stoneDark;

                    // 2) брёвна крест-накрест
                    if (OnLog(x, y, +1f) || OnLog(x, y, -1f))
                        c = log;

                    // 3) тлеющие угли между брёвнами
                    if (y >= 8 && y <= 14 && Mathf.Abs(x - 23.5f) <= 7f && Noise(x, y, 7) > 0.86f)
                        c = ember;

                    // 4) пламя в три слоя: внешний → средний → ядро
                    if (InFlame(x, y, 9f, 10, 54)) c = flameOuter;
                    if (InFlame(x, y, 6f, 12, 48)) c = flameMid;
                    if (InFlame(x, y, 3f, 14, 42)) c = flameCore;

                    px[i] = c;
                }
            }

            tex.SetPixels32(px);
            tex.Apply();

            WriteSprite(tex, TexDir + "/campfire.png", bottomPivot: true);
        }

        static bool InStone(int x, int y)
        {
            // пять валунчиков по дуге вокруг основания
            return InCircle(x, y, 10, 7, 3.4f) || InCircle(x, y, 16, 3, 2.6f) ||
                   InCircle(x, y, 24, 2, 2.2f) || InCircle(x, y, 32, 3, 2.6f) ||
                   InCircle(x, y, 38, 7, 3.4f);
        }

        static bool OnLog(int x, int y, float dir)
        {
            // диагональная полоса через центр, y от 6 до 26
            if (y < 6 || y > 26) return false;
            float d = (x - 23.5f) - dir * (y - 16f) * 0.7f;
            return Mathf.Abs(d) <= 2.6f;
        }

        /// <summary>Капля пламени: widest в нижней трети, остриё сверху, рваные края шумом.</summary>
        static bool InFlame(int x, int y, float hwMax, int yBase, int yTop)
        {
            float t = (y - yBase) / (float)(yTop - yBase);
            if (t < 0f || t > 1f) return false;

            float hw = hwMax * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.6f));
            float wobble = (Noise(x, y, 91) - 0.5f) * 2f; // рваный край ±1px
            return Mathf.Abs(x - 23.5f) <= hw + wobble;
        }

        static bool InCircle(int x, int y, float cx, float cy, float r)
            => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;

        // ------------------------------------------------------------------
        // Тайл выжженной земли: 64×32, как остальные тайлы
        // ------------------------------------------------------------------
        static void GenerateBurnTile()
        {
            const int W = 64, H = 32;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false, false);
            var px = new Color32[W * H];

            var charcoal = new Color32(46, 42, 40, 255);
            var ash = new Color32(88, 84, 78, 255);

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    float dx = Mathf.Abs(x - (W - 1) * 0.5f) / (W * 0.5f);
                    float dy = Mathf.Abs(y - (H - 1) * 0.5f) / (H * 0.5f);
                    float d = dx + dy;

                    if (d > 1f) { px[i] = new Color32(0, 0, 0, 0); continue; }

                    float n = Noise(x, y, 77) * 0.65f + Noise(x / 4, y / 4, 84) * 0.35f;
                    n = Mathf.FloorToInt(n * 4f) / 3f;

                    Color32 c = n > 0.8f ? ash : charcoal; // редкие пятна пепла
                    float factor = 0.8f + 0.4f * n;
                    if (d > 0.93f) factor *= 0.82f;
                    Color shaded = (Color)c * factor;
                    shaded.a = 1f; // не теряем плотность при умножении
                    px[i] = shaded;
                }
            }

            tex.SetPixels32(px);
            tex.Apply();

            WriteSprite(tex, TilesDir + "/tile_burn.png", bottomPivot: false);
        }

        // ------------------------------------------------------------------
        // Общая запись PNG + импорт (Single, PPU 64, Point, без сжатия)
        // ------------------------------------------------------------------
        static void WriteSprite(Texture2D tex, string path, bool bottomPivot)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG()); // GUID существующего файла сохраняется
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 64f;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.alphaIsTransparency = true;

            if (bottomPivot)
            {
                var settings = new TextureImporterSettings();
                imp.ReadTextureSettings(settings);
                settings.spriteAlignment = 7; // Bottom
                settings.spritePivot = new Vector2(0.5f, 0f);
                imp.SetTextureSettings(settings);
            }

            imp.SaveAndReimport();
        }

        static float Noise(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }
    }
}
