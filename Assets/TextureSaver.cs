using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TextureSaver")]
public class TextureSaver : ScriptableObject{
    public Texture2D texture;
    public List<Texture2D> backgroundTextures;
}
