using UnityEngine.Playables;

namespace September.InGame.Common
{
    /// <summary>
    /// Inputに接続されたPlayableをループ再生します
    /// </summary>
    public class LoopPlayableBehaviour : PlayableBehaviour
    {
        public override void PrepareFrame(Playable playable, FrameData info)
        {
            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                Playable input = playable.GetInput(i);
                double duration = input.GetDuration();
                double time = playable.GetTime();
                input.SetTime(time % duration);
            }
        }
    }
}
