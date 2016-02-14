﻿using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Mono.Data.Sqlite;
using System.Data;

public class PopupChData : MonoBehaviour
{
    [SerializeField]
    private UILabel chName;
    [SerializeField]
    private UILabel chLevel;
    [SerializeField]
    private UILabel chType;
    [SerializeField]
    private UILabel chDetailScript;
    [SerializeField]
    private GameObject popupObj;

    void Start()
    {
        SetData();
        ScaleUpEffect();
    }

    private void ScaleUpEffect()
    {
        popupObj.transform.localScale = new Vector3(0, 0, 0);
        Vector3 scaleUp = new Vector3(1, 1, 1);
        iTween.ScaleTo(popupObj, iTween.Hash("scale", scaleUp,
            "name", "scaleUp",
            "time", 1.0f,
            "speed", 10.0f,
            "easetype", iTween.EaseType.linear,
            "looptype", iTween.LoopType.none));
    }
    private void ScaleDownEffect()
    {
        popupObj.transform.localScale = new Vector3(1, 1, 1);
        Vector3 scaleUp = new Vector3(0, 0, 0);
        iTween.ScaleTo(popupObj, iTween.Hash("scale", scaleUp,
            "name", "scaleUp",
            "time", 1.0f,
            "speed", 10.0f,
            "easetype", iTween.EaseType.linear,
            "looptype", iTween.LoopType.none));
    }

    public void ClickExit()
    {
        StartCoroutine(PopupExitProcess());
    }

    private delegate void del_InsertNewUserInfo();
    public void ClickGameStart()
    {
        del_InsertNewUserInfo InsertInfo = () =>
        {
            string conn = "URI=file:" + Application.dataPath +
               "/MyAssets/Resources/GameUserDB/userDB.db";

            IDbConnection dbconn = new SqliteConnection(conn);
            IDbCommand dbcmd = dbconn.CreateCommand();
            dbconn.Open(); //Open connection to the database.

            string sqlQuery =
            "INSERT INTO USER_INFO(name, level, type) " +
            "VALUES(" + "'" + chName.text + "'" + ", " +
            "'" + chLevel.text + "'" + ", " +
            "'" + chType.text + "'" + ")";

            dbcmd.CommandText = sqlQuery;
            dbcmd.ExecuteNonQuery();

            dbcmd.Dispose();
            dbconn.Close();
        };
        InsertInfo();

        StartCoroutine(GameLoadingProcess());
    }

    private IEnumerator GameLoadingProcess()
    {
        ScaleDownEffect();
        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadSceneAsync("GameLoading");
    }

    private IEnumerator PopupExitProcess()
    {
        ScaleDownEffect();
        yield return new WaitForSeconds(0.2f);
        SceneManager.UnloadScene("popup_chInfo");
    }

    private void SetData()
    {
        GameObject sceneToSceneData = GameObject.Find("SceneToScene_datas");
        string data;
        sceneToSceneData.GetComponent<SceneToScene_Data>().gameChDatas.TryGetValue("chName", out data);
        chName.text = data;
        sceneToSceneData.GetComponent<SceneToScene_Data>().gameChDatas.TryGetValue("chLevel", out data);
        chLevel.text = data;
        sceneToSceneData.GetComponent<SceneToScene_Data>().gameChDatas.TryGetValue("chType", out data);
        chType.text = data;
        sceneToSceneData.GetComponent<SceneToScene_Data>().gameChDatas.TryGetValue("detailScript", out data);
        chDetailScript.text = data;

    }
}
