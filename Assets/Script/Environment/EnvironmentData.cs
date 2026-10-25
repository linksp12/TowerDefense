using System;
using UnityEngine;

// 환경 하나당 에셋 하나. 속성별 보정률 테이블을 가진다.
[CreateAssetMenu(fileName = "Env_", menuName = "Game/Environment Data")]
public class EnvironmentData : ScriptableObject
{
    [Serializable]
    public struct ElementModifier
    {
        public ElementType element;
        [Tooltip("0.2 = +20%, -0.2 = -20%")]
        public float rate;
    }

    [Header("기본 정보")]
    public EnvironmentType type;
    public string displayName;
    [TextArea] public string description;

    [Header("속성별 보정률")]
    public ElementModifier[] modifiers;

    // 해당 속성의 보정률 반환. 목록에 없으면 보정 없음(0).
    public float GetRate(ElementType element)
    {
        if (modifiers == null) return 0f;

        for (int i = 0; i < modifiers.Length; i++)
        {
            if (modifiers[i].element == element)
                return modifiers[i].rate;
        }
        return 0f;
    }

    // 스테이지 진입 안내 UI에서 "추천 속성" 표시용 (보정률이 양수인 속성)
    public bool IsFavorable(ElementType element) => GetRate(element) > 0f;
}
