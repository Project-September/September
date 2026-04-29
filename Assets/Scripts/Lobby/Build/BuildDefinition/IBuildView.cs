namespace September.Lobby
{
    /// <summary>ビルド選択の表示用インターフェース</summary>
    public interface IBuildView
    {
        /// <summary>ビルドルート選択入力用メソッド</summary>
        void OnNextIndex();

        /// <summary>ビルドルート選択入力用メソッド</summary>
        void OnBackIndex();

        /// <summary>ビルドルートを決定するメソッド</summary>
        void OnSelectBuild();

        /// <summary>表示する情報を切り替えるメソッド</summary>
        /// <param name="build">表示する情報</param>
        void VisulaizeBuildInfo(BuildDataBase build);

        /// <summary>ビルドルートを決定するメソッド</summary>
        /// <param name="selected">初めて決定したかどうか</param>
        void VisualizeSelection(bool selected);
    }
}
