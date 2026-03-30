using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트를 제어하기 위해 반드시 필요합니다.

public class MoneyUI : MonoBehaviour
{
    public Text moneyText; // 우리가 만든 UI Text 오브젝트를 담을 칸입니다.

    void Update()
    {
        // PlayerStats 스크립트에 저장된 Money 변수값을 가져와서 글자로 보여줍니다.
        moneyText.text = "$" + PlayerStats.Money.ToString();
    }
}
