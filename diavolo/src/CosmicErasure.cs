using System.Collections.Generic;
using UnityEngine;
using RoR2;
using UnityEngine.Rendering.PostProcessing;
namespace Diavolo
{
    // Marks imported character visuals and temporary fragments as visible in the void.
    public sealed class ErasureVisible : MonoBehaviour {}
    public sealed class CosmicErasure : MonoBehaviour
    {
        private CharacterBody owner; private TemporalController time;
        private GroundHologram ground;
        private PostProcessLayer cameraPostProcess;private bool postProcessWasEnabled;
        private GameObject sky; private Material skyMaterial,rockMaterial;
        private Renderer[] scenery=new Renderer[0]; private Terrain[] terrains=new Terrain[0];
        private float began,nextFloorProbe;private Vector3 groundCenter; private bool active,caching,cached;
        private Camera alteredCamera; private CameraClearFlags originalClear; private Color originalBackground;
        private readonly Dictionary<Renderer,bool> restore=new Dictionary<Renderer,bool>();
        private readonly Dictionary<Terrain,bool> restoreTerrain=new Dictionary<Terrain,bool>();
        private readonly Dictionary<Terrain,bool> restoreFoliage=new Dictionary<Terrain,bool>();
        private readonly List<GameObject> rocks=new List<GameObject>();
        public bool Active {get{return active;}}
        private void Awake(){owner=GetComponent<CharacterBody>();time=GetComponent<TemporalController>();}
        private System.Collections.IEnumerator CacheScenery(){
            caching=true;
            yield return null;var all=Object.FindObjectsOfType<Renderer>();var world=new List<Renderer>(all.Length);
            for(int i=0;i<all.Length;i++){if(Plugin.LowVfx.Value){caching=false;yield break;}var r=all[i];if(r && !(r is ParticleSystemRenderer) && !r.GetComponentInParent<ErasureVisible>() && !r.GetComponentInParent<CharacterBody>() && !r.GetComponentInParent<CharacterModel>())world.Add(r);if(i%128==127)yield return null;}
            scenery=world.ToArray();terrains=Object.FindObjectsOfType<Terrain>();
            cached=true;caching=false;
        }
        private void OnEnable(){SceneCamera.onSceneCameraPreCull+=BeforeCamera;SceneCamera.onSceneCameraPostRender+=AfterCamera;}
        private bool IsLocal(){foreach(LocalUser user in LocalUserManager.readOnlyLocalUsersList)if(user.cachedBody==owner)return true;return false;}
        private void Update(){
            bool wanted=!Plugin.LowVfx.Value && Plugin.CosmicWorld.Value && time && time.Erasing && owner && owner.healthComponent && owner.healthComponent.alive && IsLocal();
            if(wanted && !cached && !caching)StartCoroutine(CacheScenery());
            if(wanted && cached && !active)Begin();else if(!wanted && active)End();
            if(!active)return;
            // The stage exposure/color grading was brightening the replacement sky and
            // changing as the character crossed it. Keep this camera's erasure render stable.
            foreach(LocalUser user in LocalUserManager.readOnlyLocalUsersList)if(user.cachedBody==owner && user.cameraRigController && user.cameraRigController.sceneCam){
                var layer=user.cameraRigController.sceneCam.GetComponent<PostProcessLayer>();
                if(cameraPostProcess!=layer){RestorePostProcess();cameraPostProcess=layer;if(layer)postProcessWasEnabled=layer.enabled;}
                if(cameraPostProcess)cameraPostProcess.enabled=false;
            }

        }
        private void Begin(){
            Shader shader=DiavoloPresentation.Asset<Shader>("Assets/CosmicVoid.shader");if(!shader || !shader.isSupported)return;
            active=true;began=Time.time;
            Shader groundShader=DiavoloPresentation.Asset<Shader>("Assets/GroundHologram.shader");if(groundShader && groundShader.isSupported)ground=new GroundHologram(groundShader);
            sky=GameObject.CreatePrimitive(PrimitiveType.Sphere);sky.name="Erased-time cosmic void";
            Collider collider=sky.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            sky.AddComponent<ErasureVisible>();skyMaterial=new Material(shader);sky.GetComponent<Renderer>().sharedMaterial=skyMaterial;sky.SetActive(false);
            Shader solid=DiavoloPresentation.Asset<Shader>("Assets/CrimsonModel.shader");
            if(solid){rockMaterial=new Material(solid);rockMaterial.color=new Color(.18f,.13f,.17f);SpawnFragments();}
        }
        private void SpawnFragments(){
            for(int i=0;i<24;i++){
                Vector2 ring=Random.insideUnitCircle*24f;Vector3 probe=owner.corePosition+new Vector3(ring.x,12,ring.y);RaycastHit hit;
                if(!Physics.Raycast(probe,Vector3.down,out hit,24f,LayerIndex.world.mask,QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.point.y-owner.footPosition.y)>6f)continue;
                var rock=GameObject.CreatePrimitive(PrimitiveType.Cube);rock.name="Erased world fragment";var collider=rock.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
                rock.AddComponent<ErasureVisible>();rock.transform.position=hit.point-Vector3.up*.15f;rock.transform.rotation=Random.rotation;
                rock.transform.localScale=new Vector3(Random.Range(.4f,1.4f),Random.Range(.3f,.8f),Random.Range(.4f,1.4f));
                rock.GetComponent<Renderer>().sharedMaterial=rockMaterial;
                rock.AddComponent<ErasureFragment>();rocks.Add(rock);
            }
        }
        private void BeforeCamera(SceneCamera camera){
            if(!active || !camera || !camera.cameraRigController || camera.cameraRigController.targetBody!=owner || !sky)return;
            Restore();
            alteredCamera=camera.camera;originalClear=alteredCamera.clearFlags;originalBackground=alteredCamera.backgroundColor;
            alteredCamera.clearFlags=CameraClearFlags.SolidColor;alteredCamera.backgroundColor=Color.black;
            sky.transform.position=camera.camera.transform.position;sky.transform.localScale=Vector3.one*Mathf.Min(1600,camera.camera.farClipPlane*1.5f);sky.SetActive(true);
            float radius=Mathf.Lerp(0,3000,Mathf.Clamp01((Time.time-began)/.85f));
            foreach(Renderer r in scenery){
                if(!r)continue;
                if(r.bounds.SqrDistance(owner.corePosition)>radius*radius)continue;
                restore[r]=r.forceRenderingOff;r.forceRenderingOff=true;
            }
            if(Time.time-began>.35f)foreach(Terrain terrain in terrains)if(terrain){restoreTerrain[terrain]=terrain.drawHeightmap;terrain.drawHeightmap=false;restoreFoliage[terrain]=terrain.drawTreesAndFoliage;terrain.drawTreesAndFoliage=false;}
            if(Time.time>=nextFloorProbe){nextFloorProbe=Time.time+.2f;groundCenter=owner.footPosition;RaycastHit floor;if(Physics.Raycast(groundCenter+Vector3.up*2,Vector3.down,out floor,1000,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))groundCenter.y=floor.point.y;}
            groundCenter.x=owner.footPosition.x;groundCenter.z=owner.footPosition.z;
            if(ground!=null)ground.Draw(camera.camera,restore,terrains,groundCenter,Mathf.Clamp01((Time.time-began-.25f)/.6f));
        }
        private void AfterCamera(SceneCamera camera){Restore();if(sky)sky.SetActive(false);}
        private void Restore(){if(ground!=null)ground.Detach();if(alteredCamera){alteredCamera.clearFlags=originalClear;alteredCamera.backgroundColor=originalBackground;alteredCamera=null;}foreach(var pair in restore)if(pair.Key)pair.Key.forceRenderingOff=pair.Value;restore.Clear();foreach(var pair in restoreTerrain)if(pair.Key)pair.Key.drawHeightmap=pair.Value;restoreTerrain.Clear();foreach(var pair in restoreFoliage)if(pair.Key)pair.Key.drawTreesAndFoliage=pair.Value;restoreFoliage.Clear();}
        private void RestorePostProcess(){if(cameraPostProcess)cameraPostProcess.enabled=postProcessWasEnabled;cameraPostProcess=null;}
        private void End(){RestorePostProcess();active=false;Restore();if(ground!=null){ground.Dispose();ground=null;}if(sky)Destroy(sky);if(skyMaterial)Destroy(skyMaterial);foreach(var rock in rocks)if(rock)Destroy(rock);rocks.Clear();if(rockMaterial)Destroy(rockMaterial);}
        private void OnDisable(){SceneCamera.onSceneCameraPreCull-=BeforeCamera;SceneCamera.onSceneCameraPostRender-=AfterCamera;End();}
    }
    public sealed class ErasureFragment : MonoBehaviour
    {
        private float born;private Vector3 size,spin;private float speed;
        private void Awake(){born=Time.time;size=transform.localScale;spin=Random.onUnitSphere*120;speed=Random.Range(2f,5f);}
        private void Update(){float age=Time.time-born;transform.position+=Vector3.up*speed*Time.deltaTime;transform.Rotate(spin*Time.deltaTime);transform.localScale=size*Mathf.Clamp01((1.4f-age)/.65f);if(age>=1.4f)Destroy(gameObject);}
    }
}
