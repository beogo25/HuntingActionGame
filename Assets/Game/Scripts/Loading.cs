using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{
    public string sceneName;
    void Start()
    {
        StartCoroutine(LoadAsynSceneCoroutine());
    }

    IEnumerator LoadAsynSceneCoroutine()
    {
        yield return new WaitForSeconds(1);
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        while (!operation.isDone)
        {
            if(operation.progress>=0.9f)
            {
                break;
            }
        }
        yield return new WaitForSeconds(2);
        operation.allowSceneActivation = true;
        CharacterMove.instance.gameObject.transform.localPosition = new Vector3(510, 20, 380);
        CharacterMove.instance.gameObject.transform.GetChild(0).transform.localPosition = Vector3.zero;
        CharacterMove.instance.gameObject.transform.GetChild(0).GetComponent<Player>().Attack_AirSlashStart();
        if(CharacterMove.instance.gameObject.transform.GetChild(0).GetComponent<Player>().isGround == true)
        {
            CharacterMove.instance.gameObject.transform.GetChild(0).GetComponent<Player>().Attack_AirSlashEnd();
        }
    }
}
