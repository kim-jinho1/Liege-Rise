using System.Collections.Generic;
using Code.Core.Bus;
using Code.GameEvents.System;
using ObjectPool.RunTime;
using UnityEngine;

namespace Code.FogSystem
{
    /// <summary>
    /// 적 유닛/건물이 시야에서 벗어날 때 잔상(PlaceHolder)을 생성하고,
    /// 다시 시야에 들어올 때 잔상을 숨기는 매니저입니다.
    /// </summary>
    public class PlaceHolderManager : MonoBehaviour
    {
        [SerializeField] private PoolManagerMono poolManager;
        [SerializeField] private PoolingItemSO placeHolderItem;

        private readonly Dictionary<Transform, PlaceHolder> _placeHolderDict = new();

        private void Awake()
        {
            Bus<VisibleStateChangeEvent>.RegisterForAll(HandleVisibleStateChange);
            Bus<PlaceHolderDestroyEvent>.RegisterForAll(HandlePlaceHolderDestroy);
        }

        private void OnDestroy()
        {
            Bus<VisibleStateChangeEvent>.UnRegisterForAll(HandleVisibleStateChange);
            Bus<PlaceHolderDestroyEvent>.UnRegisterForAll(HandlePlaceHolderDestroy);
        }

        /// <summary>
        /// 잔상이 풀로 반환될 때(원본 파괴 후 재가시) 딕셔너리 엔트리를 제거해 누수를 막습니다.
        /// 원본 Transform 키가 이미 파괴됐을 수 있어 값(PlaceHolder)으로 역조회합니다.
        /// </summary>
        private void HandlePlaceHolderDestroy(PlaceHolderDestroyEvent evt)
        {
            Transform staleKey = null;
            foreach (KeyValuePair<Transform, PlaceHolder> pair in _placeHolderDict)
            {
                if (pair.Value != evt.Placeholder)
                    continue;

                staleKey = pair.Key;
                break;
            }

            if (staleKey != null)
                _placeHolderDict.Remove(staleKey);
        }

        private void HandleVisibleStateChange(VisibleStateChangeEvent evt)
        {
            if (_placeHolderDict.TryGetValue(evt.Transform, out PlaceHolder placeHolder))
            {
                // 다시 숨을 때마다 마지막으로 본 모습을 새로 베낀다. 안 그러면 처음 숨었을 때의
                // 위치·진하기가 그대로 남아, 옅어지던 잔해가 되살아난 것처럼 보인다.
                // 이미 등록된 잔상이라 스폰이 아니라 갱신 경로로 부른다
                if (!evt.IsVisible)
                    placeHolder.Capture(evt.Transform, evt.Renderer);

                placeHolder.gameObject.SetActive(!evt.IsVisible);
            }
            else if (!evt.IsVisible)
            {
                PlaceHolder newHolder = poolManager.Pop<PlaceHolder>(placeHolderItem);

                newHolder.Owner = evt.Owner;  // Owner를 먼저 채운다 — SetTransformAndSprite가 그 채널로 스폰 이벤트를 발행한다.
                newHolder.ParentObject = evt.Transform.gameObject;
                newHolder.SetTransformAndSprite(evt.Transform, evt.Renderer);
                _placeHolderDict.Add(evt.Transform, newHolder);
            }
        }
    }
}
