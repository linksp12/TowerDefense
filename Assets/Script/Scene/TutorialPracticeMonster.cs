using System;
using UnityEngine;

// 연습 웨이브의 몬스터만 표시한다. 실제 웨이브와 보상/기지 피해를 분리한다.
public class TutorialPracticeMonster : MonoBehaviour
{
    private Action<TutorialPracticeMonster> onRemoved;

    public void SetRemovalCallback(Action<TutorialPracticeMonster> callback)
    {
        onRemoved = callback;
    }

    private void OnDestroy()
    {
        onRemoved?.Invoke(this);
    }
}
