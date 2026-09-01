using System;
using September.InGame.UI;
using UnityEngine;

namespace September.Common
{
	/// <summary>
	/// InGameDebugからゲーム開始時に適用する時間設定を保持します。
	/// </summary>
	[Serializable]
	public class InGameDebugTimeSettings
	{
		public int PreStartTime = 10;
		public float Duration = 1f;
		public float AfterReadyDelay = 1f;
		public float GameTime = 180f;
		public float TimeRemaining = 30f;
		public float EndGameDelay;

		/// <summary>
		/// 保持している時間設定をゲームのタイマーへ適用します。
		/// </summary>
		public void ApplyTo(GameTimerData timerData)
		{
			if (timerData == null)
				return;

			timerData.PreStartTime = Mathf.Max(0, PreStartTime);
			timerData.Duration = Mathf.Max(0f, Duration);
			timerData.AfterReadyDelay = Mathf.Max(0f, AfterReadyDelay);
			timerData.GameTime = Mathf.Max(0f, GameTime);
			timerData.TimeRemaining = Mathf.Max(0f, TimeRemaining);
			timerData.EndGameDelay = Mathf.Max(0f, EndGameDelay);
		}
	}

	/// <summary>
	/// InGameDebugの時間設定をゲーム開始時に一時適用します。
	/// </summary>
	public static class InGameDebugTimeInjector
	{
		private static InGameDebugTimeSettings _settings;
		private static GameTimerData _appliedTimerData;
		private static TimerDataValues _originalTimerData;

		/// <summary>
		/// 次回のゲーム開始時に適用する時間設定を登録します。
		/// </summary>
		public static void Set(InGameDebugTimeSettings settings)
		{
			_settings = settings;
		}

		/// <summary>
		/// 登録済みの時間設定をタイマーへ適用します。
		/// </summary>
		public static void Apply(GameTimerData timerData)
		{
			if (_settings == null || timerData == null)
				return;

			if (_appliedTimerData != timerData)
			{
				RestoreAppliedTimerData();
				_appliedTimerData = timerData;
				_originalTimerData = new TimerDataValues(timerData);
			}

			_settings.ApplyTo(timerData);
		}

		/// <summary>
		/// 登録した設定を解除し、変更したタイマーを元の値へ戻します。
		/// </summary>
		public static void Clear()
		{
			RestoreAppliedTimerData();
			_settings = null;
		}

		private static void RestoreAppliedTimerData()
		{
			if (_appliedTimerData != null)
				_originalTimerData.ApplyTo(_appliedTimerData);

			_appliedTimerData = null;
		}

		private readonly struct TimerDataValues
		{
			private readonly int _preStartTime;
			private readonly float _duration;
			private readonly float _afterReadyDelay;
			private readonly float _gameTime;
			private readonly float _timeRemaining;
			private readonly float _endGameDelay;

			public TimerDataValues(GameTimerData timerData)
			{
				_preStartTime = timerData.PreStartTime;
				_duration = timerData.Duration;
				_afterReadyDelay = timerData.AfterReadyDelay;
				_gameTime = timerData.GameTime;
				_timeRemaining = timerData.TimeRemaining;
				_endGameDelay = timerData.EndGameDelay;
			}

			public void ApplyTo(GameTimerData timerData)
			{
				timerData.PreStartTime = _preStartTime;
				timerData.Duration = _duration;
				timerData.AfterReadyDelay = _afterReadyDelay;
				timerData.GameTime = _gameTime;
				timerData.TimeRemaining = _timeRemaining;
				timerData.EndGameDelay = _endGameDelay;
			}
		}
	}
}
