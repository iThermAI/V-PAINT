using UnityEngine;
using RenderHeads.Media.AVProVideo;

public class ApplyVideoTexture1 : MonoBehaviour
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
