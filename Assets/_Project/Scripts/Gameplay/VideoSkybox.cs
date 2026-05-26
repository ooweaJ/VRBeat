using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoSkybox : MonoBehaviour
{
    [SerializeField] RenderTexture renderTexture;
    [SerializeField] string        videoFileName = "motion.mp4";

    void Awake()
    {
        var vp = GetComponent<VideoPlayer>();
        vp.source          = VideoSource.Url;
        vp.url             = System.IO.Path.Combine(Application.dataPath, "Material", videoFileName);
        vp.targetTexture   = renderTexture;
        vp.isLooping       = true;
        vp.playOnAwake     = true;
        vp.renderMode      = VideoRenderMode.RenderTexture;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        vp.Play();
    }
}
