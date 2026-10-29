using UnityEngine;

/// <summary>타워 외형 한 벌: 원래 그림과 바꿀 그림의 짝(같은 순서). 기준점·크기는 원래 그림과 같게 만든다.</summary>
[CreateAssetMenu(fileName = "TowerSkinSet", menuName = "TowerDefense/Tower Skin Set")]
public class TowerSkinSet : ScriptableObject
{
    public Sprite[] originals = System.Array.Empty<Sprite>();
    public Sprite[] skins = System.Array.Empty<Sprite>();
}
