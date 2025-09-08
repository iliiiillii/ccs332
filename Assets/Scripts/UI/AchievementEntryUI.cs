using UnityEngine;
using UnityEngine.UI; // Image ������Ʈ ����� ���� �߰�
using TMPro;         // TextMeshPro ����� ���� �߰�

public class AchievementEntryUI : MonoBehaviour
{
    // Inspector���� ������ UI ��ҵ�
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;          // ���� �̸��� ǥ���� TextMeshPro ������Ʈ
    public TextMeshProUGUI descriptionText;   // ���� ������ ǥ���� TextMeshPro ������Ʈ
    public TextMeshProUGUI rewardText;        // ���� ������ ǥ���� TextMeshPro ������Ʈ
    public Image checkmarkImage;        // �޼� ���� üũ��ũ�� ǥ���� Image ������Ʈ (�޼� �ÿ��� Ȱ��ȭ)

    // ���� ��Ȳ ���� UI ��Ҵ� ������� �����Ƿ� �ּ� ó�� �Ǵ� ����
    // public Image achievementIconImage;
    // public Slider progressSlider;
    // public TextMeshProUGUI progressValueText;

    /// <summary>
    /// ���� �׸� UI�� Ư�� ���� �����ͷ� �����մϴ�.
    /// </summary>
    /// <param name="achievementDefinition">ǥ���� ������ ���� ������</param>
    /// <param name="isAchieved">�ش� ������ �޼� ����</param>
    public void Setup(AchievementDefinitionRecord achievementDefinition, bool isAchieved) // currentProgressString �Ű����� ����
    {
        Debug.Log("======= AchievementEntryUI.Setup �Լ� ��¥ ��¥ ���� ����! =======");
        if (achievementDefinition == null)
        {
            Debug.LogError("AchievementDefinitionRecord�� null�Դϴ�. ���� UI�� ������ �� �����ϴ�.");
            gameObject.SetActive(false); // ���� �� �׸� ���� ó��
            return;
        }
        Debug.Log($"Setup ȣ��: ID='{achievementDefinition.achievementIdText}', �̸�='{achievementDefinition.achievementName}', ����='{achievementDefinition.description}', �޼�����='{isAchieved}'");
        // 1. ���� �̸� ����
        if (nameText != null)
        {
            nameText.text = achievementDefinition.achievementName;
            Debug.Log($"������ �̸� ĭ�� �� ����: '{nameText.text}'");
        }
        else
        {
            Debug.LogWarning($"[{achievementDefinition.achievementName}] NameText�� ������� �ʾҽ��ϴ�.");
        }

        // 2. ���� ���� ����
        // �̴޼� �ÿ��� ������ �����ְ�, �޼� �ÿ��� ������ ����ų� "�޼� �Ϸ�!"�� ������ �� �ֽ��ϴ�.
        // ���⼭�� �޼� �� ������ "�޼� �Ϸ�!"�� �����ϰ�, üũ��ũ�� �ֵ� ǥ�÷� ����մϴ�.
        if (descriptionText != null)
        {
            if (isAchieved)
            {
                descriptionText.text = "�޼� �Ϸ�!"; // �Ǵ� ���ϴ� �ٸ� �ؽ�Ʈ
                Debug.Log($"������ ���� ĭ�� �� ����: '{descriptionText.text}'");
                // descriptionText.fontStyle = FontStyles.Italic; // ����: �޼� �� ��Ʈ ��Ÿ�� ����
            }
            else
            {
                descriptionText.text = achievementDefinition.description;
                Debug.Log($"������ ���� ĭ�� �� ����: '{descriptionText.text}'");
                // descriptionText.fontStyle = FontStyles.Normal; // ����: �̴޼� �� �⺻ ��Ʈ ��Ÿ��
            }
        }
        else
        {
            Debug.LogWarning($"[{achievementDefinition.achievementName}] DescriptionText�� ������� �ʾҽ��ϴ�.");
        }

        // 3. ���� ���� ����
        if (rewardText != null)
        {
            if (achievementDefinition.rewardGold > 0)
            {
                rewardText.text = $"����: ��� +{achievementDefinition.rewardGold}";
                rewardText.gameObject.SetActive(true);
            }
            else
            {
                rewardText.text = ""; // ������ ������ �����
                rewardText.gameObject.SetActive(false); // �Ǵ� ��Ȱ��ȭ
            }
        }
        else
        {
            Debug.LogWarning($"[{achievementDefinition.achievementName}] RewardText�� ������� �ʾҽ��ϴ�.");
        }

        // 4. �޼� ���� üũ��ũ �̹��� ���� (���� �߿��� �κ�)
        if (checkmarkImage != null)
        {
            checkmarkImage.gameObject.SetActive(isAchieved); // �޼������� üũ��ũ Ȱ��ȭ, �ƴϸ� ��Ȱ��ȭ
                                                             // (CheckmarkImage�� Source Image���� �̸� üũ��ũ ��������Ʈ�� �Ҵ�Ǿ� �־�� ��)
        }
        else
        {
            Debug.LogWarning($"[{achievementDefinition.achievementName}] CheckmarkImage�� ������� �ʾҽ��ϴ�.");
        }

        // (���� ����) �޼� ���ο� ���� UI ��ü�� �ð��� ��Ÿ�� ����
        Image backgroundImage = GetComponent<Image>(); // �� ��ũ��Ʈ�� ���� ������Ʈ�� Image ������Ʈ�� ��� ������ �Ѵٰ� ����
        if (backgroundImage != null)
        {
            // ����: �޼� �� ������ ���ϰ� �����ϰų�, �̴޼� �� �⺻������.
            // ���� �ڵ�(0.8f, 0.8f, 0.8f, 0.7f)�� �޼� �� �ణ ��Ӱ� �������ϰ� �ϴ� ���̾��׿�.
            // ���Ͻô� ��Ÿ�Ϸ� �����Ͻø� �˴ϴ�. ���� ��� �޼� �� �� ���� ���̳� Ư���� �������� ������ �� �ֽ��ϴ�.
            backgroundImage.color = isAchieved ? new Color(0.85f, 1f, 0.85f, 0.9f) : Color.white; // �޼� �� ���� �ʷϺ�, �̴޼� �� ��� ���
        }
    }
}