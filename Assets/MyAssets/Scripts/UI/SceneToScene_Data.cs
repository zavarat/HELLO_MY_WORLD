﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// scene 사이의 데이터 통신을 위한 클래스.
/// </summary>
public class SceneToScene_Data : MonoBehaviour {

    private Dictionary<string, string> _gameDatas;
    public Dictionary<string, string> gameDatas
    {
        get { return _gameDatas; }
    }
    
    public void SetData(string key, string value)
    {
        _gameDatas.Add(key, value);
    }

    public void ClearElement()
    {
        _gameDatas.Clear();
    }

    void Start()
    {
        _gameDatas = new Dictionary<string, string>();
        DontDestroyOnLoad(gameObject);
    }
}
