using UnityEngine;

namespace ReinforceGame
{
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] ReinforceSettings settings;
        [SerializeField] GameObject editorPreview;
        void Awake()
        {
            if (editorPreview) { editorPreview.SetActive(false); Destroy(editorPreview); }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.045f, .055f, .065f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.015f, .02f, .025f);
            RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .055f;
            if (!settings) settings = Resources.Load<ReinforceSettings>("ReinforceSettings");
            if (!settings || !settings.workshop)
            {
                Debug.LogError("ReinforceGame: Generate Workshop in the Editor first.");
                enabled = false;
                return;
            }
            var room = Instantiate(settings.workshop);
            room.name = "Reinforce Workshop";
            room.GetComponent<WorkshopGame>().Initialize(settings);
        }
    }
}
