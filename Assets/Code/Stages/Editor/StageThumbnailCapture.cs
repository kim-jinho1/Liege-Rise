using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Code.Stages.Editor
{
    /// <summary>
    /// 열려 있는 스테이지 씬의 지형을 StageSO의 맵 경계에 맞춰 찍어 썸네일 PNG로 저장합니다.
    /// </summary>
    public class StageThumbnailCapture : EditorWindow
    {
        /// <summary>
        /// 어느 칸에 쓸 썸네일을 찍는지입니다. 칸마다 비율이 달라 따로 찍어야 합니다.
        /// </summary>
        private enum Target
        {
            Detail, // 상세 패널 872 × 340
            Row,    // 목록 행 116 × 116
        }

        /// <summary>
        /// 맵을 썸네일 칸에 맞추는 방식입니다.
        /// </summary>
        private enum Framing
        {
            Cover,      // 칸을 꽉 채운다. 넘치는 축은 잘린다
            FullHeight, // 맵 세로를 다 담고 가로 부족분은 경계 밖 지형으로 채운다
            Fit,        // 맵 전체를 담고 남는 곳은 배경색이 된다
        }

        private const string OutputFolder = "Assets/_Graphics/UI/thumbnails";

        private const int TerrainLayers = (1 << 3) | (1 << 4) | (1 << 6) | (1 << 16);
        private const int ObjectLayers = (1 << 7) | (1 << 8) | (1 << 9) | (1 << 13) | (1 << 15);

        private StageSO _stage;
        private Target _target = Target.Detail;
        private Framing _framing = Framing.Cover;
        private int _supersample = 2;
        private bool _includeObjects;
        private bool _assignToStage = true;
        private float _padding = 0.02f;
        private Color _background = new(0.27450982f, 0.67058825f, 0.6666667f, 1f);

        // DetailPanel과 StageRow의 썸네일 칸 실제 크기. 출력 비율이 여기서 나온다
        private int OutWidth => _target == Target.Detail ? 872 : 116;
        private int OutHeight => _target == Target.Detail ? 340 : 116;
        private string Suffix => _target == Target.Detail ? "_Thumb" : "_Row";
        private string FieldPath => _target == Target.Detail
            ? "<Thumbnail>k__BackingField"
            : "<ListThumbnail>k__BackingField";

        private float Aspect => (float)OutWidth / OutHeight;

        /// <summary>
        /// 썸네일 촬영 창을 엽니다.
        /// </summary>
        [MenuItem("Tools/Debug/Capture Stage Thumbnail")]
        private static void Open()
        {
            StageThumbnailCapture window = GetWindow<StageThumbnailCapture>(true, "Capture Stage Thumbnail");
            window.minSize = new Vector2(440f, 380f);
            window.Show();
        }

        private void OnEnable()
        {
            AutoFillFromOpenScene();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "열려 있는 스테이지 씬을 전용 카메라로 RenderTexture에 렌더합니다.\n" +
                "게임 뷰가 아니므로 UI는 애초에 안 담기고, 씬 뷰와 달리 카메라 배경색이 칠해집니다.",
                MessageType.Info);

            _stage = (StageSO)EditorGUILayout.ObjectField("Stage", _stage, typeof(StageSO), false);

            if (GUILayout.Button("열린 씬에서 다시 찾기"))
                AutoFillFromOpenScene();

            EditorGUILayout.Space();

            _target = (Target)EditorGUILayout.EnumPopup("Target", _target);
            _framing = (Framing)EditorGUILayout.EnumPopup("Framing", _framing);
            _supersample = EditorGUILayout.IntSlider("Supersample", _supersample, 1, 8);

            if (_framing == Framing.Fit)
                _padding = EditorGUILayout.Slider("Padding", _padding, 0f, 0.2f);

            _includeObjects = EditorGUILayout.Toggle("건물·자원까지 포함", _includeObjects);
            _assignToStage = EditorGUILayout.Toggle("StageSO에 자동 배선", _assignToStage);
            _background = EditorGUILayout.ColorField("Background", _background);

            if (GUILayout.Button("씬 카메라에서 배경색 읽기"))
                ReadBackgroundFromScene();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("출력", Describe(), EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_stage == null))
            {
                if (GUILayout.Button("Capture", GUILayout.Height(32f)))
                    Capture();
            }

            if (_includeObjects)
            {
                EditorGUILayout.HelpBox(
                    "건물·자원은 MapGenerator가 플레이 중에 뿌립니다. 에디터 모드에서는 씬에 없으므로 " +
                    "이 옵션은 플레이 중에 찍을 때만 의미가 있습니다(안개가 대부분을 가립니다).",
                    MessageType.Warning);
            }
        }

        private string Describe()
        {
            if (_stage == null)
                return "StageSO를 지정하세요.";

            Vector2 map = _stage.MapSize;
            Vector2 view = ViewSizeFor(map);

            string note = view.y < map.y - 0.01f || view.x < map.x - 0.01f
                ? $"세로 {Mathf.Max(0f, map.y - view.y) * 0.5f:0.#} · 가로 {Mathf.Max(0f, map.x - view.x) * 0.5f:0.#} 유닛씩 잘림"
                : view.x > map.x + 0.01f || view.y > map.y + 0.01f
                    ? "경계 밖까지 담김(주변 지형이 없으면 배경색)"
                    : "잘림 없음";

            return $"{OutWidth * _supersample} × {OutHeight * _supersample} px\n" +
                   $"맵 {map.x} × {map.y} → 담기는 범위 {view.x:0.#} × {view.y:0.#} ({note})\n" +
                   PathFor();
        }

        private string PathFor() => $"{OutputFolder}/{_stage.Scene}{Suffix}.png";

        private float OrthoSizeFor(Vector2 map)
        {
            return _framing switch
            {
                // 두 축 요구치 중 작은 쪽을 쓰면 칸이 꽉 차고 넘치는 축이 잘린다
                Framing.Cover => Mathf.Min(map.y * 0.5f, map.x / Aspect * 0.5f),
                Framing.FullHeight => map.y * 0.5f,
                _ => Mathf.Max(map.y * 0.5f, map.x / Aspect * 0.5f) * (1f + _padding),
            };
        }

        private Vector2 ViewSizeFor(Vector2 map)
        {
            float ortho = OrthoSizeFor(map);
            return new Vector2(ortho * 2f * Aspect, ortho * 2f);
        }

        private void AutoFillFromOpenScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            foreach (string guid in AssetDatabase.FindAssets("t:StageSO"))
            {
                StageSO candidate = AssetDatabase.LoadAssetAtPath<StageSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (candidate != null && candidate.Scene.ToString() == sceneName)
                {
                    _stage = candidate;
                    break;
                }
            }

            ReadBackgroundFromScene();
        }

        private void ReadBackgroundFromScene()
        {
            Camera main = Camera.main;
            if (main == null)
                return;

            // 씬 카메라의 알파는 0이다. 그대로 쓰면 PNG 배경이 투명해진다
            _background = new Color(main.backgroundColor.r, main.backgroundColor.g, main.backgroundColor.b, 1f);
        }

        private void Capture()
        {
            int width = OutWidth * _supersample;
            int height = OutHeight * _supersample;

            RenderTexture rt = new(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            GameObject holder = EditorUtility.CreateGameObjectWithHideFlags(
                "~StageThumbnailCamera", HideFlags.HideAndDontSave, typeof(Camera));
            Texture2D texture = null;

            try
            {
                Camera cam = holder.GetComponent<Camera>();
                ConfigureCamera(cam, rt);
                Render(cam, rt);
                texture = ReadBack(rt, width, height);
                WritePng(texture.EncodeToPNG());
            }
            finally
            {
                if (texture != null)
                    DestroyImmediate(texture);

                DestroyImmediate(holder);
                rt.Release();
                DestroyImmediate(rt);
            }
        }

        private void ConfigureCamera(Camera cam, RenderTexture rt)
        {
            cam.enabled = false;
            cam.orthographic = true;
            cam.orthographicSize = OrthoSizeFor(_stage.MapSize);
            cam.aspect = Aspect;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(_background.r, _background.g, _background.b, 1f);
            cam.cullingMask = TerrainLayers | (_includeObjects ? ObjectLayers : 0);
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 500f;
            cam.targetTexture = rt;

            Vector2 center = _stage.MapCenter;
            cam.transform.SetPositionAndRotation(new Vector3(center.x, center.y, -100f), Quaternion.identity);
        }

        private static void Render(Camera cam, RenderTexture rt)
        {
            UniversalRenderPipeline.SingleCameraRequest request = new() { destination = rt };

            if (RenderPipeline.SupportsRenderRequest(cam, request))
                RenderPipeline.SubmitRenderRequest(cam, request);
            else
                cam.Render();
        }

        private static Texture2D ReadBack(RenderTexture rt, int width, int height)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();

            RenderTexture.active = previous;
            return texture;
        }

        private void WritePng(byte[] png)
        {
            Directory.CreateDirectory(OutputFolder);
            string path = PathFor();
            File.WriteAllBytes(path, png);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ApplyImportSettings(path);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (_assignToStage && sprite != null)
                AssignThumbnail(sprite);

            Debug.Log($"[StageThumbnailCapture] 저장했습니다: {path}", sprite);
        }

        private static void ApplyImportSettings(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private void AssignThumbnail(Sprite sprite)
        {
            SerializedObject so = new(_stage);
            SerializedProperty property = so.FindProperty(FieldPath);

            if (property == null)
                return;

            property.objectReferenceValue = sprite;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(_stage);
        }
    }
}
