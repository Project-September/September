using September.InGame.Common.Hitbox.Shapes;

namespace September.InGame.Common.Hitbox.Hitboxes
{
    /// <summary>
    /// ボックス形状のヒットボックス。
    /// </summary>
    /// <remarks>
    /// SubclassSelectorで使用可能にするための非ジェネリックインターフェースです。
    /// </remarks>
    public interface IBoxHitbox : IHitbox<Box>
    {
        
    }
}
