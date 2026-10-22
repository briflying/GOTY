using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DarkwoodEditor
{
    /// <summary>
    /// Пересоздаёт сломанный 2D-рендерер и включает 2D-пайплайн для проекта.
    ///
    /// Что было: ассет «2D Render Pipeline Asset» (URP-ассет) ссылается на
    /// Renderer2D-данные, которые кто-то удалил, — из-за этого 2D-свет
    /// (Light 2D, Shadow Caster 2D) в проекте недоступен.
    ///
    /// Что делает скрипт:
    ///   1) создаёт новый ассет Renderer2D.asset (данные 2D-рендерера);
    ///   2) подключает его в «2D Render Pipeline Asset» вместо битой ссылки;
    ///   3) назначает 2D-пайплайн активным в Quality Settings и Graphics Settings.
    ///
    /// Запуск: меню Tools → Игра → Пересоздать 2D-рендерер.
    /// </summary>
    public static class Setup2DRenderer
    {
        const string RendererPath = "Assets/Settings/Renderer2D.asset";
        const string PipelinePath = "Assets/Settings/2D Render Pipeline Asset.asset";

        [MenuItem("Tools/Игра/Пересоздать 2D-рендерер")]
        public static void Run()
        {
            // --- 1. Новый Renderer2D (если уже есть наш — переиспользуем) ---
            var renderer2D = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (renderer2D == null)
            {
                renderer2D = ScriptableObject.CreateInstance<Renderer2DData>();
                renderer2D.name = "Renderer2D";
                AssetDatabase.CreateAsset(renderer2D, RendererPath);
                Debug.Log("Создан ассет: " + RendererPath);
            }

            // --- 2. Вставляем его в 2D URP-ассет вместо висячей ссылки ---
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                Debug.LogError("Не найден ассет: " + PipelinePath + " — запускай в проекте, где он есть.");
                return;
            }

            var so = new SerializedObject(pipeline);
            var list = so.FindProperty("m_RendererDataList");
            if (list == null || list.arraySize == 0)
            {
                if (list == null)
                {
                    Debug.LogError("У ассета нет поля m_RendererDataList — неожиданная версия URP.");
                    return;
                }
                list.InsertArrayElementAtIndex(0);
            }
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer2D;

            // в новых URP есть ещё карта «имя → рендерер», обновляем и её
            var map = so.FindProperty("m_RendererDataMap");
            if (map != null && map.arraySize > 0)
            {
                var rendererProp = map.GetArrayElementAtIndex(0).FindPropertyRelative("m_Renderer");
                if (rendererProp != null) rendererProp.objectReferenceValue = renderer2D;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("«2D Render Pipeline Asset» теперь ссылается на Renderer2D.");

            // --- 3. Делаем 2D-пайплайн активным для всего проекта ---
            SetProjectPipeline(pipeline, "ProjectSettings/QualitySettings.asset");
            SetProjectPipeline(pipeline, "ProjectSettings/GraphicsSettings.asset");
            Debug.Log("2D-пайплайн назначен активным (Quality Settings + Graphics Settings).");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<b>SETUP_2D_OK</b> — 2D-рендерер пересоздан и включён. Проверка: окно Lighting видит 2D-свет.");
        }

        /// <summary>Прописывает pipeline в объект из ProjectSettings через SerializedObject.</summary>
        static void SetProjectPipeline(Object pipeline, string settingsPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(settingsPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("Не найден " + settingsPath);
                return;
            }

            var so = new SerializedObject(assets[0]);
            SerializedProperty prop;
            if (settingsPath.EndsWith("QualitySettings.asset"))
            {
                // у уровней качества поле вложено: m_QualitySettings[0].customRenderPipeline
                var levels = so.FindProperty("m_QualitySettings");
                if (levels == null || levels.arraySize == 0) return;
                prop = levels.GetArrayElementAtIndex(0).FindPropertyRelative("customRenderPipeline");
            }
            else
            {
                // в Graphics Settings поле корневое
                prop = so.FindProperty("m_CustomRenderPipeline");
            }

            if (prop == null) return;
            prop.objectReferenceValue = pipeline;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
