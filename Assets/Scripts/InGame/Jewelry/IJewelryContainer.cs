namespace InGame.Jewelry.Common
{
    public interface IJewelryContainer
    {
        /// <summary>
        /// 宝石を拾う
        /// </summary>
        /// <param name="jewelry">取得する宝石</param>
        /// <remarks>宝石の破棄や取得演出の再生はこのメソッド内で行われます</remarks>
        public void PickUp(IJewelry jewelry);

        /// <summary>
        /// 宝石をドロップする
        /// </summary>
        /// <param name="dropAmount">ドロップさせる個数</param>
        /// <param name="resultDropped">ドロップした宝石のデータを格納する配列（メソッド内から書き込み）</param>
        /// <returns>配列に書き込んだ数（配列のサイズより大きくなりません）</returns>
        public int DropJewelry(int dropAmount, IJewelry[] resultDropped = null);

        public int GetJewelryCount(JewelryType jewelryType);
    }
}
