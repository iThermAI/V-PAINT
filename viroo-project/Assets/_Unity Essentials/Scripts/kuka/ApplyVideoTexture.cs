using UnityEngine;
using RenderHeads.Media.AVProVideo;

public class ApplyVideoTexture : MonoBehaviour
{
    public MediaPlayer mediaPlayer;
    public Material videoMaterial;

    void Update()
    {
        if (mediaPlayer != null && mediaPlayer.TextureProducer != null)
        {
            Texture tex = mediaPlayer.TextureProducer.GetTexture();
            if (tex != null)
                videoMaterial.mainTexture = tex;
        }
    }
}
