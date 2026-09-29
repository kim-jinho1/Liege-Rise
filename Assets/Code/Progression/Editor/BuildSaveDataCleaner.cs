using System.IO;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR_WIN
using Microsoft.Win32;
#endif

namespace Code.Progression.Editor
{
    /// <summary>
    /// 이미 빌드된 게임의 저장 데이터(PlayerPrefs)를 에디터에서 삭제하는 전용 창입니다.
    /// </summary>
    public class BuildSaveDataCleaner : EditorWindow
    {
        private const string DefaultCompany = "Wa";
        private const string DefaultProduct = "Liege & Rise";

        private string _companyName = DefaultCompany;
        private string _productName = DefaultProduct;
        private string _scanResult = "Scan을 눌러 삭제 대상을 확인하세요.";

        /// <summary>
        /// 빌드본 저장 데이터 삭제 창을 엽니다.
        /// </summary>
        [MenuItem("Tools/Debug/Clear Built Game Save Data")]
        private static void Open()
        {
            BuildSaveDataCleaner window = GetWindow<BuildSaveDataCleaner>(true, "Clear Built Game Save Data");
            window.minSize = new Vector2(420f, 320f);
            window.Show();
        }

        /// <summary>
        /// 회사명/제품명 입력 필드와 스캔·삭제 버튼, 결과 표시 영역을 그립니다.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "빌드 당시 사용한 회사명/제품명을 입력하세요.\n" +
                $"기본값: {DefaultCompany} / {DefaultProduct} (다른 빌드면 직접 수정).",
                MessageType.Info);

            _companyName = EditorGUILayout.TextField("Company Name", _companyName);
            _productName = EditorGUILayout.TextField("Product Name", _productName);

            EditorGUILayout.Space();

#if UNITY_EDITOR_WIN
            EditorGUILayout.LabelField("Registry Key", RegistryPath(_companyName, _productName));
            EditorGUILayout.LabelField("PersistentData", PersistentDataPath(_companyName, _productName));

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan", GUILayout.Height(28f)))
                    _scanResult = Scan(_companyName, _productName);

                GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
                if (GUILayout.Button("Delete", GUILayout.Height(28f)))
                    Delete(_companyName, _productName);
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_scanResult, GUILayout.ExpandHeight(true));
#else
            EditorGUILayout.HelpBox(
                "이 도구는 Windows 에디터 전용입니다 (PlayerPrefs가 레지스트리에 저장됨).",
                MessageType.Warning);
#endif
        }

#if UNITY_EDITOR_WIN
        /// <summary>
        /// 회사명/제품명으로 PlayerPrefs 레지스트리 키 경로를 만듭니다.
        /// </summary>
        private static string RegistryPath(string company, string product)
        {
            return $@"HKCU\Software\{company}\{product}";
        }

        /// <summary>
        /// 빌드본의 Application.persistentDataPath 경로를 만듭니다 (LocalLow).
        /// </summary>
        private static string PersistentDataPath(string company, string product)
        {
            string profile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            return Path.Combine(profile, "AppData", "LocalLow", company, product);
        }

        /// <summary>
        /// 삭제 대상 레지스트리 항목과 persistentDataPath 파일 목록을 조사해 문자열로 반환합니다.
        /// </summary>
        private static string Scan(string company, string product)
        {
            if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(product))
                return "회사명과 제품명을 모두 입력하세요.";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            string subKey = $@"Software\{company}\{product}";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(subKey))
            {
                if (key == null)
                {
                    sb.AppendLine($"[Registry] 없음: {RegistryPath(company, product)}");
                }
                else
                {
                    string[] names = key.GetValueNames();
                    sb.AppendLine($"[Registry] {names.Length}개 항목: {RegistryPath(company, product)}");
                    foreach (string name in names)
                        sb.AppendLine($"  - {name}");
                }
            }

            string dataPath = PersistentDataPath(company, product);
            if (Directory.Exists(dataPath))
            {
                string[] files = Directory.GetFiles(dataPath, "*", SearchOption.AllDirectories);
                sb.AppendLine($"[PersistentData] {files.Length}개 파일: {dataPath}");
                foreach (string file in files)
                    sb.AppendLine($"  - {Path.GetFileName(file)}");
            }
            else
            {
                sb.AppendLine($"[PersistentData] 없음: {dataPath}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 확인 후 빌드본의 레지스트리 키와 persistentDataPath 폴더를 삭제합니다.
        /// </summary>
        private static void Delete(string company, string product)
        {
            if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(product))
            {
                EditorUtility.DisplayDialog("Clear Built Game Save Data", "회사명과 제품명을 모두 입력하세요.", "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Clear Built Game Save Data",
                $"다음 빌드본 저장 데이터를 삭제합니다.\n\n{RegistryPath(company, product)}\n{PersistentDataPath(company, product)}\n\n되돌릴 수 없습니다.",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            string subKey = $@"Software\{company}\{product}";
            if (Registry.CurrentUser.OpenSubKey(subKey) != null)
            {
                Registry.CurrentUser.DeleteSubKeyTree(subKey, false);
                sb.AppendLine($"[Registry] 삭제됨: {RegistryPath(company, product)}");
            }
            else
            {
                sb.AppendLine($"[Registry] 대상 없음: {RegistryPath(company, product)}");
            }

            string dataPath = PersistentDataPath(company, product);
            if (Directory.Exists(dataPath))
            {
                Directory.Delete(dataPath, true);
                sb.AppendLine($"[PersistentData] 삭제됨: {dataPath}");
            }
            else
            {
                sb.AppendLine($"[PersistentData] 대상 없음: {dataPath}");
            }

            Debug.Log($"[BuildSaveDataCleaner] {sb}");
            EditorUtility.DisplayDialog("Clear Built Game Save Data", sb.ToString(), "OK");
        }
#endif
    }
}
