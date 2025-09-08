using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트 사용을 위해 추가
using TMPro;         // TextMeshPro 사용을 위해 추가

public class AchievementEntryUI : MonoBehaviour
{
    // Inspector에서 연결할 UI 요소들
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;          // 업적 이름을 표시할 TextMeshPro 오브젝트
    public TextMeshProUGUI descriptionText;   // 업적 설명을 표시할 TextMeshPro 오브젝트
    public TextMeshProUGUI rewardText;        // 보상 정보를 표시할 TextMeshPro 오브젝트
    public Image checkmarkImage;        // 달성 여부 체크마크를 표시할 Image 오브젝트 (달성 시에만 활성화)

    // 진행 상황 관련 UI 요소는 사용하지 않으므로 주석 처리 또는 삭제
    // public Image achievementIconImage;
    // public Slider progressSlider;
    // public TextMeshProUGUI progressValueText;

    /// <summary>
    /// 업적 항목 UI를 특정 업적 데이터로 설정합니다.
    /// </summary>
    /// <param name="achievementDefinition">표시할 업적의 정의 데이터</param>
    /// <param name="isAchieved">해당 업적의 달성 여부</param>
    public void Setup(AchievementDefinitionRecord achievementDefinition, bool isAchieved) // currentProgressString 매개변수 제거
    {
        Debug.Log("======= AchievementEntryUI.Setup 함수 진짜 진짜 실행 시작! =======");
        if (achievementDefinition == null)
        {
            Debug.LogError("AchievementDefinitionRecord가 null입니다. 업적 UI를 설정할 수 없습니다.");
            gameObject.SetActive(false); // 오류 시 항목 숨김 처리
            return;
        }
        Debug.Log($"Setup 호출: ID='{achievementDefinition.achievementIdText}', 이름='{achievementDefinition.achievementName}', 설명='{achievementDefinition.description}', 달성여부='{isAchieved}'");
        // 1. 업적 이름 설정
        if (nameText != null)
        {
            nameText.text = achievementDefinition.achievementName;
            Debug.Log($"실제로 이름 칸에 쓴 글자: '{nameText.text}'");
        }
        else
        {
            Debug.LogWarning($"[{achievementDefinition.achievementName}] NameText가 연결되지 않았습니다.");
        }

        // 2. 업적 설명 설정
        // 미달성 시에는 설명을 보여주고, 달성 시에는 설명을 숨기거나 "달성 완료!"로 변경할 수 있습니다.
        // 여기서는 달성 시 설명을 "달성 완료!"로 변경하고, 체크마크를 주된 표시로 사용합니다.
        if (descriptionText != null)
        {
            if (isAchieved)
            {
                descriptionText.text = "달성 완료!"; // 또는 원하는 다른 텍스트
                Debug.Log($"실제로 설명 칸에 쓴 글자: '{descriptionText.text}'");
                // descriptionText.fontStyle = FontStyles.Italic; // 예시: 달성 시 폰트 스타일 변경
            }
            else
            {
                descriptionText.text = achievementDefinition.description;
                Debug.Log($"실제로 설명 칸에 쓴 글자: '{descriptionText.text}'");
                // descriptionText.fontStyle = FontStyles.Normal; // 예시: 미달성 시 기본 폰트 스타일
            }
        }
        else
        {
            Debug.LogWarning($"[{achievementDefinition.achievementName}] DescriptionText가 연결되지 않았습니다.");
        }

        // 3. 보상 정보 설정
        if (rewardText != null)
        {
            if (achievementDefinition.rewardGold > 0)
            {
                rewardText.text = $"보상: 골드 +{achievementDefinition.rewardGold}";
                rewardText.gameObject.SetActive(true);
            }
            else
            {
                rewardText.text = ""; // 보상이 없으면 비워둠
                rewardText.gameObject.SetActive(false); // 또는 비활성화
            }
        }
        else
        {
            Debug.LogWarning($"[{achievementDefinition.achievementName}] RewardText가 연결되지 않았습니다.");
        }

        // 4. 달성 여부 체크마크 이미지 설정 (가장 중요한 부분)
        if (checkmarkImage != null)
        {
            checkmarkImage.gameObject.SetActive(isAchieved); // 달성했으면 체크마크 활성화, 아니면 비활성화
                                                             // (CheckmarkImage의 Source Image에는 미리 체크마크 스프라이트가 할당되어 있어야 함)
        }
        else
        {
            Debug.LogWarning($"[{achievementDefinition.achievementName}] CheckmarkImage가 연결되지 않았습니다.");
        }

        // (선택 사항) 달성 여부에 따라 UI 전체의 시각적 스타일 변경
        Image backgroundImage = GetComponent<Image>(); // 이 스크립트가 붙은 오브젝트에 Image 컴포넌트가 배경 역할을 한다고 가정
        if (backgroundImage != null)
        {
            // 예시: 달성 시 배경색을 연하게 변경하거나, 미달성 시 기본색으로.
            // 현재 코드(0.8f, 0.8f, 0.8f, 0.7f)는 달성 시 약간 어둡고 반투명하게 하는 것이었네요.
            // 원하시는 스타일로 수정하시면 됩니다. 예를 들어 달성 시 더 밝은 톤이나 특별한 색상으로 변경할 수 있습니다.
            backgroundImage.color = isAchieved ? new Color(0.85f, 1f, 0.85f, 0.9f) : Color.white; // 달성 시 연한 초록빛, 미달성 시 흰색 배경
        }
    }
}