namespace September.Lobby
{
    /// <summary>ビルド選択の表示用インターフェース</summary>
    public interface IBuildView
    {
        /// <summary>ビルドルート選択入力用メソッド</summary>
        void NextIndex();

        /// <summary>ビルドルート選択入力用メソッド</summary>
        void BackIndex();

        /// <summary>ビルドルートを決定するメソッド</summary>
        void SelectBuild();

        /// <summary>表示する情報を切り替えるメソッド</summary>
        /// <param name="index">選択中のインデックス</param>
        void VisualizeBuildInfo(int index);

        /// <summary>ビルドルートを決定するメソッド</summary>
        /// <param name="selected">すでに決定しているかどうか</param>
        /// <param name="index">決定したビルドルートのインデックス</param>
        void VisualizeSelection(bool selected, int index);
    }
}
