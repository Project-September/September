using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace GameEvent
{
    /// <summary>
    /// データクラス
    /// </summary>
    [Serializable]
    public class GameEventData
    {
        [Serializable]
        public class ParamData
        {
            public string Key;
            public string TypeName;
            public string Data;
        }

        [SerializeField]
        protected List<ParamData> Payload = new List<ParamData>();

        public void DataPack<T>(string Key, T data)
        {
            Payload.Add(new ParamData()
            {
                Key = Key,
                TypeName = typeof(T).Name,
                Data = data.ToString()
            });
        }
    }
}