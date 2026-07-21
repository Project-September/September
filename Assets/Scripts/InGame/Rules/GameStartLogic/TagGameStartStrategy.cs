using System.Linq;
using September.Common;
using UnityEngine;

namespace September.InGame.Rules
{
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
