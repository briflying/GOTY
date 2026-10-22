using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarkwoodEditor
{
    /// <summary>
    /// Генерирует плейсхолдер-изометрические тайлы (64×32) для этапа 1.
    /// Мрачная палитра + детерминированный шум. Текстуры намеренно средне-светлые:
    /// темноту даёт глобальный свет 2D, а не сам тайл.
    /// Запуск: Tools → Игра → Сгенерировать плейсхолдер-тайлы.
    /// </summary>
    public static class GeneratePlaceholderTiles
    {
        const string Dir = "Assets/_Game/Textures/Tiles";

        [MenuItem("Tools/Игра/Сгенерировать плейсхолдер-тайлы")]
        public static void Run()
        {
            if (!Directory.Exists(Dir))
                Directory.CreateDirectory(Dir);

            // (имя, базовый цвет, сид вариации)
            Gen("tile_moss_a", new Color32(78, 98, 56, 255), 11);
            Gen("tile_moss_b", new Color32(70, 90, 52, 255), 23);
            Gen("tile_dirt_a", new Color32(102, 86, 66, 255), 37);
            Gen("tile_dirt_b", new Color32(94, 80, 62, 255), 51);
            Gen("tile_stone", new Color32(118, 120, 126, 255), 67);

            AssetDatabase.Refresh();
            Debug.Log("<b>PLACEHOLDER_TILES_OK</b> — 5 тайлов сгенерировано в " + Dir);
        }

        static void Gen(string name, Color32 baseColor, int seed)
        {
            // страховка: папка могла быть удалена/перемещена между вызовами
            if (!Directory.Exists(Dir))
                Directory.CreateDirectory(Dir);

            const int W = 64, H = 32;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false, false);
            var px = new Color32[W * H];

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;

                    // алмаз 2:1: центр посередине, нормируем расстояние ромба
                    float dx = Mathf.Abs(x - (W - 1) * 0.5f) / (W * 0.5f);
                    float dy = Mathf.Abs(y - (H - 1) * 0.5f) / (H * 0.5f);
                    float d = dx + dy;

                    if (d > 1f)
                    {
                        px[i] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    // двухоктавный шум, приведённый к 4 тонам — «зернистость» без мыла
                    float n = Noise(x, y, seed) * 0.65f + Noise(x / 4, y / 4, seed + 7) * 0.35f;
                    n = Mathf.FloorToInt(n * 4f) / 3f;

                    float factor = 0.8f + 0.4f * n;          // вариация тона
                    if (d > 0.93f) factor *= 0.82f;          // тонкий тёмный кант у края

                    Color c = (Color)baseColor * factor;
                    c.a = 1f; // не теряем плотность при умножении
                    px[i] = c;
                }
            }

            tex.SetPixels32(px);
            tex.Apply();

            string path = Dir + "/" + name + ".png";

            // при переимпорте существующего PNG со сменой режима спрайта Unity падает
            // с ArgumentException внутри RegisterSpriteDependency — поэтому удаляем
            // старые файлы и каждый раз создаём ассет заново
            File.Delete(path);
            File.Delete(path + ".meta");

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            // настройки импорта под тайлы: спрайт, PPU 64, без размытия и сжатия
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single; // без этого Unity ставит Multiple и спрайта внутри нет
            imp.spritePixelsPerUnit = 64f;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }

        /// <summary>Быстрый детерминированный шум: одинаковый при каждой генерации.</summary>
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
