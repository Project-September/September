using System;

namespace September.Common
{
    /// <summary>多態性を利用するためのインターフェース</summary>
    public interface IBuild { };

    /// <summary>ビルドシステムの機能を定義したインターフェース</summary>
    /// <typeparam name="TCondition">条件達成時に加算する数値のデータ型</typeparam>
    /// <typeparam name="TValue">現在のビルドステータスのデータ型</typeparam>
    public interface IBuild<TCondition, TValue> : IBuild where TCondition : IComparable where TValue : IComparable
    {
        /// <summary>現在のビルドの状態を返すプロパティ</summary>
        TValue CurrentBuild { get; }

        /// <summary>ビルドの状態を更新するメソッド</summary>
        /// <param name="value">条件達成時に加算する数値</param>
        void UpdateBuild(TCondition value);
    }
}
