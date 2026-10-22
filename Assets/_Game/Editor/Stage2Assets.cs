using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DarkwoodEditor
{
    /// <summary>
    /// Этап 2: генерирует спрайт игрока-плейсхолдера и включает сортировку
    /// спрайтов по оси Y (кто ниже на экране — тот рисуется спереди).
    /// Запуск: Tools → Игра → Этап 2 — спрайт игрока и сортировка.
    /// </summary>
    public static class Stage2Assets
    {
        const string TexDir = "Assets/_Game/Textures";
        const string PlayerTex = TexDir + "/player.png";
        const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";

        [MenuItem("Tools/Игра/Этап 2 — спрайт игрока и сортировка")]
        public static void Run()
        {
            GeneratePlayerSprite();
            SetupYSorting();
            AssetDatabase.Refresh();
            Debug.Log("<b>STAGE2_ASSETS_OK</b> — player.png создан, сортировка по Y включена.");
        }

        // ------------------------------------------------------------------
        // Спрайт игрока: тёмный силуэт 48×64, ноги внизу (пивот — Bottom,
        // чтобы точка объекта совпадала с ногами и Y-сортировка была честной)
        // ------------------------------------------------------------------
        static void GeneratePlayerSprite()
        {
            if (!Directory.Exists(TexDir))
                Directory.CreateDirectory(TexDir);

            const int W = 48, H = 64;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false, false);
            var px = new Color32[W * H];

            var body = new Color32(62, 68, 55, 255);
            var head = new Color32(80, 86, 68, 255);
            var legs = new Color32(52, 58, 47, 255);
            var outline = new Color32(24, 26, 20, 255);

            bool[] mask = new bool[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                mask[y * W + x] = InsideBody(x, y);

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (!mask[i]) { px[i] = new Color32(0, 0, 0, 0); continue; }

                    // контур: если рядом с «дыркой» — тёмная кайма
                    bool edge = !Get(mask, x + 1, y, W, H) || !Get(mask, x - 1, y, W, H) ||
                                !Get(mask, x, y + 1, W, H) || !Get(mask, x, y - 1, W, H);

                    Color32 c = body;
                    if (y >= 44) c = head;
                    else if (y < 22) c = legs;
                    if (edge) c = outline;

                    // лёгкий шум, чтобы фигурка не была плоской
                    float n = 0.92f + Noise(x, y, 5) * 0.16f;
                    Color col = (Color)c * n;
                    col.a = 1f; // умножение задевает и альфу — возвращаем полную плотность
                    px[i] = col;
                }
            }

            tex.SetPixels32(px);
            tex.Apply();

            bool existed = File.Exists(PlayerTex); // если есть — просто перезапишем байты, GUID не сменится
            File.WriteAllBytes(PlayerTex, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(PlayerTex);
            var imp = (TextureImporter)AssetImporter.GetAtPath(PlayerTex);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;

            // пивот у ног: в Unity 6 задаётся через TextureImporterSettings
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteAlignment = 7; // 7 = Bottom
            settings.spritePivot = new Vector2(0.5f, 0f);
            imp.SetTextureSettings(settings);

            imp.spritePixelsPerUnit = 64f;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();

            Debug.Log(existed ? "player.png перезаписан (GUID сохранён)." : "player.png создан.");
        }

        static bool InsideBody(int x, int y)
        {
            // голова
            if (InCircle(x, y, 23.5f, 48.5f, 6.5f)) return true;
            // шея/плечи
            if (y >= 42 && y <= 44 && Mathf.Abs(x - 23.5f) <= 8f) return true;
            // торс — трапеция
            if (y >= 22 && y <= 42)
            {
                float t = (y - 22) / 20f;
                float half = 6f + 2f * t;
                if (Mathf.Abs(x - 23.5f) <= half) return true;
            }
            // руки по бокам
            if (y >= 24 && y <= 41 && ((x >= 14 && x <= 17) || (x >= 30 && x <= 33))) return true;
            // ноги
            if (y >= 1 && y <= 22 && ((x >= 18 && x <= 22) || (x >= 25 && x <= 29))) return true;
            return false;
        }

        static bool InCircle(int x, int y, float cx, float cy, float r)
            => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;

        static bool Get(bool[] m, int x, int y, int w, int h)
            => x >= 0 && x < w && y >= 0 && y < h && m[y * w + x];

        static float Noise(int x, int y, int seed)
        {
            unchecked
            {
                int hsh = x * 374761393 + y * 668265263 + seed * 1442695041;
                hsh = (hsh ^ (hsh >> 13)) * 1274126177;
                hsh ^= hsh >> 16;
                return (hsh & 0xFFFF) / 65535f;
            }
        }

        // ------------------------------------------------------------------
        // Y-сортировка на 2D-рендерере: ось (0,1,0)
        // ------------------------------------------------------------------
        static void SetupYSorting()
        {
            var r2d = AssetDatabase.LoadAssetAtPath<Renderer2DData>(Renderer2DPath);
            if (r2d == null)
            {
                Debug.LogWarning("Renderer2D.asset не найден — сначала запусти этап 0 (2D-рендерер).");
                return;
            }

            var so = new SerializedObject(r2d);
            var mode = so.FindProperty("m_TransparencySortMode");
            var axis = so.FindProperty("m_TransparencySortAxis");
            if (mode == null || axis == null)
            {
                Debug.LogWarning("Поля сортировки не найдены — версия URP отличается, настрой вручную: Renderer2D → Transparency Sort Mode = Custom Axis (0,1,0).");
                return;
            }

            mode.intValue = 1;                            // Custom Axis
            axis.vector3Value = new Vector3(0f, 1f, 0f);  // сортируем по мировому Y
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(r2d);
            Debug.Log("Сортировка прозрачности: Custom Axis (0,1,0) — кто ниже на экране, рисуется спереди.");
        }
    }
}
