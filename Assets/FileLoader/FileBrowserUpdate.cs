using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FileBrowserUpdate : MonoBehaviour{

    public static Action<Texture2D> projectUploaded;
    public static Action<Texture2D> textureUploaded;

    Texture2D loadedImage;

    //public void LoadImage() {
    //    FileBrowser browser = new FileBrowser();
    //    browser.OpenFileBrowser(GetProperties(), path => {
    //        //Load image from local path with UWR
    //        StartCoroutine(WaitImageLoaded(path));
    //    });
    //}

    //public void LoadProject() {
    //    FileBrowser browser = new FileBrowser();
    //    browser.OpenFileBrowser(GetProperties(), path => {
    //        //Load image from local path with UWR
    //        StartCoroutine(WaitProjectLoaded(path));
    //    });
    //}

    //public BrowserProperties GetProperties() {
    //    var bp = new BrowserProperties("BrowserProps");
    //    bp.filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";
    //    bp.filterIndex = 0;
    //    return bp;
    //}

    IEnumerator WaitProjectLoaded(string path) {
        yield return StartCoroutine(LoadImage(path));
        projectUploaded?.Invoke(loadedImage);
    }

    IEnumerator WaitImageLoaded(string path) {
        yield return StartCoroutine(LoadImage(path));
        textureUploaded?.Invoke(loadedImage);
    }

    IEnumerator LoadImage(string path){
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path)){
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError){
                Debug.Log(uwr.error);
            }
            else{
                loadedImage = DownloadHandlerTexture.GetContent(uwr);
            }
        }
    }
}
