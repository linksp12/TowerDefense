using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static int Money; // 어디서든 접근 가능한 정적 변수
    public int startMoney = 100; // 시작할 때 주는 돈

    void Start()
    {
        Money = startMoney;
    }
}
