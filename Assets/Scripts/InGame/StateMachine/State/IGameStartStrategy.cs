using System.Linq;
using UnityEngine;

namespace September.Common
{
    public interface IGameStartStrategy
    {
        public void OnGameStarted();
    }

    /// <summary>
    /// 鬼ごっこ開始時の処理
    /// </summary>
    public class TagGameStartStrategy : IGameStartStrategy
    {
        public void OnGameStarted()
        {
            ChooseOgre();
        }

        /// <summary>
        /// 鬼を抽選するメソッド
        /// </summary>
        private void ChooseOgre()
        {
            var dic = PlayerDatabase.Instance.PlayerDataDic;
            if (dic.Count <= 0) return;

            var index = Random.Range(0, dic.Count);
            var ogreKey = dic.ToArray()[index].Key;
            var data = dic.Get(ogreKey);
            data.IsOgre = true;
            PlayerDatabase.Instance.PlayerDataDic.Set(ogreKey, data);
            PlayerDatabase.Instance.Server_AddOgreCount(ogreKey);
        }
    }
}
