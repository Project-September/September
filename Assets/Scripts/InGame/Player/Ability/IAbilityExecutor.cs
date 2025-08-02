using System.Collections.Generic;
using Fusion;
using September.Common;

namespace InGame.Player.Ability
{
    public interface IAbilityExecutor
    {
        public Dictionary<int, List<AbilityBase>> ActiveAbilities { get; }
        void RequestAbilityExecution(AbilityContext context);
    }
}
