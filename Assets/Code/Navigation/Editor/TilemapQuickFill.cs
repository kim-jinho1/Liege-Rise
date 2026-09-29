using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace Code.Navigation.Editor
{
    public class TilemapQuickFill : EditorWindow
    {
        [SerializeField] private VisualTreeAsset root = default;

        private Tilemap _tilemap;
        private TileBase _tileToFill;

        private int _width = 140;
        private int _height = 140;

        [MenuItem("Tools/Tilemap Fill Window")]
        public static void ShowWindow()
        {
            GetWindow<TilemapQuickFill>("타일맵 채우기");
        }

        private void CreateGUI()
        {
            if (root == null)
            {
                Debug.LogError($"[{name}] root is null!", this);
                return;
            }

            VisualElement tree = root.CloneTree();

            SetupObjectField(tree, "TileMapField", _tilemap, val => _tilemap = val);
            SetupObjectField(tree, "TileField", _tileToFill, val => _tileToFill = val);

            SetupIntegerField(tree, "SizeX", _width, val => _width = val);
            SetupIntegerField(tree, "SizeY", _height, val => _height = val);

            tree.Q<Button>("CreateButton").clickable.clicked += HandleCreate;
            rootVisualElement.Add(tree);
        }

        private void SetupObjectField<T>(VisualElement visualElement, string fieldName, T initialValue, System.Action<T> onValueChanged) where T : Object
        {
            ObjectField field = visualElement.Q<ObjectField>(fieldName);
            if (field == null)
                return;

            field.objectType = typeof(T);
            field.value = initialValue;
            field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue as T));
        }

        private void SetupIntegerField(VisualElement visualElement, string fieldName, int initialValue, System.Action<int> onValueChanged)
        {
            IntegerField field = visualElement.Q<IntegerField>(fieldName);
            if (field == null)
                return;

            field.value = initialValue;
            field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
        }

        private void HandleCreate()
        {
            if (_tilemap == null)
            {
                EditorUtility.DisplayDialog("ERROR", "Tilemap is null!", "OK");
                return;
            }


            if (_tileToFill == null)
            {
                EditorUtility.DisplayDialog("ERROR", "TileBase is null!", "OK");
                return;
            }

            Undo.RecordObject(_tilemap, "Fill Tilemap Area");

            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    _tilemap.SetTile(new Vector3Int(x, y, 0), _tileToFill);

            SceneView.RepaintAll();
            Debug.Log($"<color=cyan>[성공]</color> {_width}x{_height} 영역을 채웠습니다.");
        }
    }
}
