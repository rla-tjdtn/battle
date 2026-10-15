using UnityEngine;
using UnityEngine.UI;
using BlockyBattleRoyale.Core;

namespace BlockyBattleRoyale.UI
{
    public class TutorialHints : MonoBehaviour
    {
        private static readonly string[] Lines =
        {
            "WASD로 이동하세요",
            "마우스로 조준하세요",
            "왼쪽 클릭으로 사격하세요",
            "Space로 회피하세요",
            "F로 아이템을 획득하세요",
            "Tab으로 가방을 여세요. [ 정렬, Backspace 절반 버리기",
            "M으로 전체 지도를 여세요"
        };

        private int step;
        private Text label;
        private bool done;

        private void Start()
        {
            if (GameSettings.TutorialDone)
            {
                done = true;
                return;
            }
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            var go = new GameObject("Tutorial");
            go.transform.SetParent(canvas.transform, false);
            label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 22;
            label.alignment = TextAnchor.UpperCenter;
            label.color = new Color(1f, 0.95f, 0.8f);
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -150);
            rt.sizeDelta = new Vector2(900, 40);
            Show();
        }

        private void Update()
        {
            if (done || label == null) return;
            if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
            {
                GameSettings.TutorialDone = true;
                done = true;
                label.text = "";
                return;
            }
            bool advanced = step switch
            {
                0 => Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0,
                1 => Mathf.Abs(Input.GetAxis("Mouse X")) + Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.2f,
                2 => Input.GetMouseButtonDown(0),
                3 => Input.GetKeyDown(KeyCode.Space),
                4 => Input.GetKeyDown(KeyCode.F),
                5 => Input.GetKeyDown(KeyCode.Tab),
                _ => Input.GetKeyDown(KeyCode.M)
            };
            if (!advanced) return;
            step++;
            if (step >= Lines.Length)
            {
                done = true;
                GameSettings.TutorialDone = true;
                label.text = "";
                return;
            }
            Show();
        }

        private void Show()
        {
            label.text = Lines[step] + "    (Enter 건너뛰기)";
        }
    }
}
