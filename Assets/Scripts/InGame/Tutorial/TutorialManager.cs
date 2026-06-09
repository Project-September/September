using Fusion;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace September.InGame.Tutorial
{
    public class TutorialManager : NetworkBehaviour
    {
        /// <summary>チュートリアルでプレイさせたいアクション配列</summary>
        [SerializeReference, SubclassSelector] private List<TutorialActionBase> _tutorialActions;

        public override void Spawned()
        {
            base.Spawned();
        }

        private void Start()
        { 
            _tutorialActions[0].OnStart();
        }

        private void Update()
        {
            _tutorialActions[0].OnUpdate();
        }
    }
}
