using System.Collections.Generic;
using System.Linq;

namespace Result
{
    public class ScoreTracker
    {
        // インタラクト回数
        private readonly Dictionary<ExhibitType, int> _count =  new();
        private int _stunCount;
        private ExhibitScoreConfig _config;
        
        private int _grapplingHookCount;
        private int _friendExhibitCount;
        
        private readonly Dictionary<ExhibitType, int> _destroyedExhibitCounts = new();

        // スコアの加算処理
        public void AddGrapplingHook() => _grapplingHookCount++;
        public void AddFriendExhibit() => _friendExhibitCount++;

        public int GrapplingHookCount => _grapplingHookCount;
        public int FriendExhibitCount => _friendExhibitCount;

        public ScoreTracker(ExhibitScoreConfig config)
        {
            _config = config;
        }
        
        public void SetConfig(ExhibitScoreConfig config) => _config = config;
        public IReadOnlyDictionary<ExhibitType,int> ExhibitCounts => _count;
        public IReadOnlyDictionary<ExhibitType,int> DestroyedExhibitCounts => _destroyedExhibitCounts;
        
        // Stun
        public void AddStun() => _stunCount++;
        public int StunCount => _stunCount;
        
        // インタラクト回数を追加
        public void AddInteract(ExhibitType t)
        {
            _count.TryAdd(t, 0);
            _count[t]++;
        }
        
        public void SetInteractCount(ExhibitType type, int value)
        {
            _count[type] = value < 0 ? 0 : value;
        }
        
        public int GetInteractCount(ExhibitType type)
        {
            return _count.GetValueOrDefault(type, 0);
        }
        
        public void AddDestroyed(ExhibitType t)
        {
            _destroyedExhibitCounts.TryAdd(t, 0);
            _destroyedExhibitCounts[t]++;
        }
        
        public void MergeFrom(ScoreTracker other)
        {
            if (other == null) 
                return;

            foreach (var kv in other._count)
            {
                _count.TryAdd(kv.Key, 0);
                _count[kv.Key] += kv.Value;
            }
            _stunCount += other._stunCount;
        }
        
        // 合計値を計算
        public int CalcTotal()
        {
            int sum = 0;
            
            sum += _count.Sum(kv => (_config ? _config.GetPoint(kv.Key) : 0) * kv.Value);
            
            const int stunPoint = 150;
            sum += _stunCount * stunPoint;
            
            sum += _destroyedExhibitCounts.Sum(kv => _config .GetDestroyPoint(kv.Key) * kv.Value);

            return sum;
        }

        public void Clear()
        {
            _count.Clear();
            _destroyedExhibitCounts.Clear();
            _stunCount = 0;
            _grapplingHookCount = 0;
            _friendExhibitCount = 0;
        }
    }
}