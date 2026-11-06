using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GrowingCity
{
    public sealed class CityPrototype : MonoBehaviour
    {
        [Header("City assets — originals are read-only")]
        [SerializeField] GameObject[] homes, midrises, towers;
        [SerializeField] GameObject shopPrefab, officePrefab;
        [SerializeField] GameObject fuelDepotPrefab,ammunitionDepotPrefab,powerStationPrefab;
        [SerializeField] Material fuelDepotMaterial,ammunitionDepotMaterial,powerStationMaterial;
        [SerializeField] Material volatileBuildingMaterial,buildingFragmentMaterial;
        [SerializeField] GameObject aircraftPrefab;
        [SerializeField] CityCombinationSettings combinationSettings;
        [SerializeField] GameObject roadStraight, roadIntersection, roadCorner, roadTee, roadEnd;
        [SerializeField] Material fallbackMaterial, lotMaterial, rubbleMaterial, flashMaterial, ringMaterial, districtMaterial;
        [Header("Scene references")]
        [SerializeField] Camera cityCamera;
        [SerializeField] CityLanguage language;
        [SerializeField] Text nuclearCooldownText;
        NuclearCooldown nuclearCooldown=new NuclearCooldown();
        string L(string key)=>language?language.Get(key):key;
        string F(string key,params object[] values)=>language?language.Format(key,values):key;
        void RefreshLanguage(){lastCount=lastRuins=lastDistricts=-1;lastRadiusLevel=lastBombLevel=lastAircraftLevel=lastDropRateLevel=lastFormationLevel=lastSpeedLevel=lastArmorLevel=lastHealthLevel=-1;feedback=L("drag");feedbackUntil=0;if(simulation!=null)UpdateHud();}
        public void CycleLanguage(){if(language)language.Cycle();}
        [SerializeField] Transform worldRoot, ground;
        [SerializeField] Text scoreText, cityText, statusText, weaponText, speedText, developmentText, developmentGainText;
        [SerializeField] Text[] goldRewardTexts;
        [SerializeField] Button rangeUpgradeButton,bombUpgradeButton;
        [SerializeField] Text rangeUpgradeInfo,bombUpgradeInfo;
        [SerializeField] Button aircraftUpgradeButton;
        [SerializeField] Text aircraftUpgradeInfo,automaticGrowthInfo;
        [SerializeField] Button dropRateUpgradeButton,formationUpgradeButton;
        [SerializeField] Text dropRateUpgradeInfo,formationUpgradeInfo;
        [SerializeField] Button speedUpgradeButton,armorUpgradeButton,healthUpgradeButton;
        [SerializeField] Text speedUpgradeInfo,armorUpgradeInfo,healthUpgradeInfo,sortieStatusText;
        [SerializeField] RectTransform[] aircraftHealthRoots;
        [SerializeField] Image[] aircraftHealthFills;
        [SerializeField] Image sortieChargeFill;
        float[] aircraftHealthWidths;
        float sortieChargeWidth;
        [SerializeField] RectTransform[] dragGuideSegments;
        [SerializeField,Min(1)] float dragArrowHeadSize=18;
        [Header("Editable prototype tuning")]
        [SerializeField,Min(8)] float cellSize=16;
        [SerializeField,Range(1,8)] float streetWidth=2;
        [SerializeField,Range(1,4)] int roadBlockSize=2;
        [SerializeField,Range(.65f,.98f)] float buildingFill=.87f;
        [SerializeField,Range(20,50)] float cameraPitch=27;
        [SerializeField,Range(.6f,1.1f)] float cameraZoom=.8f;
        [SerializeField,Min(.1f)] float growthInterval=1.15f;
        [SerializeField,Min(0)] float rebuildDelay=4.5f;
        [SerializeField,Min(0)] int maxBuildings=0;
        [SerializeField,Min(1)] int initialCityCells=96;
        [SerializeField,Range(8,64)] int mapSizeCells=24;
        [SerializeField,Range(1,8)] int growthBatchSize=4;
        bool snapCityCamera;
        [SerializeField] int seed=104729;
        [SerializeField,Range(0,200)] int maxDebris=100;
        [SerializeField,Min(1)] float bombDamage=60,nuclearDamage=150;
        [SerializeField,Min(.1f)] float bombRadiusCells=1.5f,nuclearRadiusCells=3.5f;
        [SerializeField,Min(.1f)] float sortieRechargeSeconds=10;
        readonly Dictionary<Cell,BuildingView> views=new Dictionary<Cell,BuildingView>();
        readonly Dictionary<Cell,GameObject> scars=new Dictionary<Cell,GameObject>();
        readonly Dictionary<string,GameObject> roads=new Dictionary<string,GameObject>();
        readonly Dictionary<string,GameObject> districtPaths=new Dictionary<string,GameObject>();
        readonly Queue<GameObject> debris=new Queue<GameObject>();
        readonly Queue<GameObject> structuralDebris=new Queue<GameObject>();
        readonly List<CityShockwave> effects=new List<CityShockwave>();
        readonly List<BuildingView> falling=new List<BuildingView>();
        readonly List<CityAirstrike> airstrikes=new List<CityAirstrike>();
        sealed class SortieOrder {public AirstrikePlan Plan;public Vector3[] Offsets;public Vector3 Entry,Exit;public float Altitude,Scale,Radius,Damage;public double Health,Armor;public bool Nuclear;}
        sealed class PendingChain {public ChainExplosion Chain;public double At;}
        readonly List<PendingChain> pendingChains=new List<PendingChain>();
        readonly Queue<SortieOrder> sortieOrders=new Queue<SortieOrder>();
        SortieCharge sortieCharge;
        CityDefense defense;
        readonly List<CityBuildingBurst> buildingBursts=new List<CityBuildingBurst>();
        CityGoldHud goldHud;
        bool dragging,draggingTouch,dragNuclear;
        bool roadsDirty;
        int dragPointerId;
        Vector2 dragScreenStart;
        Vector3 dragStart,dragEnd;
        CitySimulation simulation;
        double simulationTime;
        float speed=1,resumeSpeed=1;
        bool nuclear;
        Vector3 focus,focusVelocity;
        float cameraDistance=56,distanceVelocity,shake;
        long lastGold=-1;
        int lastRadiusLevel=-1,lastBombLevel=-1;
        int lastAircraftLevel=-1;
        int lastDropRateLevel=-1,lastFormationLevel=-1;
        int lastSpeedLevel=-1,lastArmorLevel=-1,lastHealthLevel=-1;
        long lastDevelopment=-1;
        float displayedDevelopment, developmentFrom, developmentAge=2, gainAge=2;
        Vector3 developmentScale;
        Vector2 gainOrigin;
        int lastCount=-1,lastRuins=-1,lastDistricts=-1;
        string feedback="";
        double feedbackUntil;
        AudioSource impactAudio;
        AudioClip bombAudio,nukeAudio,goldAudio;
        CityRoadBatch roadBatch;
        public CitySimulation Simulation => simulation;
        public Camera CityCamera => cityCamera;
        sealed class HitSurface {public Renderer Renderer;public int Index;public Color Color;public MaterialPropertyBlock Original,Flash;}
        sealed class BuildingView { public Lot Lot; public GameObject Root; public Bounds Bounds; public float Age; public Quaternion Rotation; public Vector3 Position; public Vector3 FallDirection; public bool Falling;public bool Fractured;public int CollapseMode;public float CollapseDuration;public Vector3 CollapsePivot,BlastPoint; public bool Evolving; public bool Downgrading;public float HitAge=1;public Transform Visual;public Vector3 VisualPosition;public Quaternion VisualRotation;public HitSurface[] Surfaces; }
        void Start()
        {
            if(language){language.Initialize(CityLanguage.SystemCode());language.Changed+=RefreshLanguage;}
            if(!cityCamera || !worldRoot || homes==null || homes.Length==0 || !fallbackMaterial || !aircraftPrefab) {
                Debug.LogError(L("setupError"),this); enabled=false; return;
            }
            Application.targetFrameRate=60;
            if(developmentText)developmentScale=developmentText.rectTransform.localScale;
            if(developmentGainText){gainOrigin=developmentGainText.rectTransform.anchoredPosition;developmentGainText.canvasRenderer.SetAlpha(0);}
            goldHud=new CityGoldHud(goldRewardTexts,scoreText);
            if(aircraftHealthFills!=null){aircraftHealthWidths=new float[aircraftHealthFills.Length];for(int i=0;i<aircraftHealthFills.Length;i++)if(aircraftHealthFills[i])aircraftHealthWidths[i]=aircraftHealthFills[i].rectTransform.rect.width;}
            if(sortieChargeFill)sortieChargeWidth=sortieChargeFill.rectTransform.rect.width;
            impactAudio=gameObject.AddComponent<AudioSource>();impactAudio.playOnAwake=false;impactAudio.volume=.16f;
            bombAudio=MakeImpact("Bomb impact",.65f,90);nukeAudio=MakeImpact("Nuclear impact",1.45f,45);
            goldAudio=MakeGoldChime();
            ResetCity();
        }
        public void ResetCity()
        {
            // This root contains only prototype-generated world objects, never the user's scene objects.
            foreach(Transform child in worldRoot) Destroy(child.gameObject);
            if(roadBatch!=null)roadBatch.Dispose();roadBatch=new CityRoadBatch(worldRoot);
            CancelDrag();foreach(var run in airstrikes)run.Dispose();airstrikes.Clear();
            defense?.Dispose();sortieOrders.Clear();sortieCharge=new SortieCharge(sortieRechargeSeconds);if(aircraftHealthRoots!=null)foreach(var root in aircraftHealthRoots)if(root)root.gameObject.SetActive(false);
            pendingChains.Clear();buildingBursts.Clear();goldHud?.Reset();
            views.Clear();scars.Clear();roads.Clear();districtPaths.Clear();debris.Clear();structuralDebris.Clear();effects.Clear();falling.Clear();
            simulationTime=0;shake=0;roadsDirty=false;nuclearCooldown=new NuclearCooldown();
            simulation=new CitySimulation(seed){GrowthInterval=growthInterval,RecoveryDelay=rebuildDelay,MaxLiving=maxBuildings,EnableCombinations=true,AutomaticGrowth=true,EnableSpecialBuildings=true,MapSize=mapSizeCells,GrowthBatchSize=growthBatchSize,ScatteredGrowth=true,ProgressiveBirthLevels=true};
            if(combinationSettings)combinationSettings.Apply(simulation);
            simulation.SeedCity(Mathf.Max(1,initialCityCells));SynchronizeBuildings();
            foreach(var view in views.Values){view.Age=2;view.Evolving=false;view.Root.transform.localScale=Vector3.one;view.Root.transform.position=view.Position;}snapCityCamera=true;
            defense=new CityDefense(worldRoot,cellSize,rubbleMaterial,ringMaterial,()=>simulation.Buildings.Values,MissileRoof,AirTargets,SpawnAirExplosion,OnAircraftHit);
            feedback=L("welcome");feedbackUntil=3;
            lastGold=-1;lastDevelopment=-1;lastCount=lastRuins=lastDistricts=-1;lastRadiusLevel=lastBombLevel=-1;
            lastAircraftLevel=-1;
            lastDropRateLevel=lastFormationLevel=-1;
            lastSpeedLevel=lastArmorLevel=lastHealthLevel=-1;
            gainAge=2;if(developmentGainText)developmentGainText.canvasRenderer.SetAlpha(0);
            UpdateHud();
            AnimateDevelopment(0);
        }
        void Update()
        {
            if(simulation==null)return;
            float dt=Time.deltaTime; simulationTime+=dt*speed;
            if(speed>0) {int evolutions=simulation.TotalEvolutions,sets=simulation.DistrictCount;var added=simulation.Step(simulationTime);if(added!=null){SynchronizeBuildings();if(added.IsSpecial){feedback=L("specialSpawn");feedbackUntil=simulationTime+3;}if(simulation.TotalEvolutions>evolutions){feedback=L("evolved");feedbackUntil=simulationTime+3;}else if(simulation.DistrictCount>sets){feedback=L("district");feedbackUntil=simulationTime+3;}}}
            sortieCharge.Tick(dt*speed);nuclearCooldown.Tick(speed>0?dt:0);HandleInput();
            for(int i=airstrikes.Count-1;i>=0;i--)if(airstrikes[i].Tick(dt*speed))airstrikes.RemoveAt(i);
            PlayPendingChains();defense.Tick(dt*speed,simulationTime);SpawnQueuedSorties();UpdateAircraftHud();
            if(roadsDirty){ReconcileRoads();roadsDirty=false;}
            foreach(var view in views.Values){AnimateBirth(view,dt*speed);AnimateHit(view,dt);}
            for(int i=falling.Count-1;i>=0;i--) {
                var view=falling[i];AnimateHit(view,dt);view.Age+=dt;float t=Mathf.Clamp01(view.Age/view.CollapseDuration);
                var pose=DestructionMotion.Pose(view.CollapseMode,t*DestructionMotion.Duration(view.CollapseMode),view.Bounds.size.y,Mathf.Max(view.Bounds.size.x,view.Bounds.size.z));
                var rotation=Quaternion.AngleAxis((float)pose.Angle,Vector3.Cross(Vector3.up,view.FallDirection))*Quaternion.AngleAxis((float)pose.Yaw,Vector3.up);
                view.Root.transform.position=view.CollapsePivot+rotation*(view.Position-view.CollapsePivot)+view.FallDirection*(float)pose.Travel+Vector3.up*(float)(pose.Lift-pose.Drop);view.Root.transform.rotation=rotation*view.Rotation;
                if(!view.Fractured&&t>=DestructionMotion.BreakFraction(view.CollapseMode)){view.Fractured=true;if(maxDebris>0)CityFracture.Emit(view.Visual,worldRoot,view.BlastPoint,cellSize,view.Lot.Style,view.Downgrading?2:3,buildingFragmentMaterial,ringMaterial,cityCamera,RegisterStructuralPiece);SpawnBuildingBurst(view.Root.transform.position+Vector3.up*cellSize*.3f,cellSize*.65f,true);view.Root.SetActive(false);}
                if(t>=1){Destroy(view.Root);falling.RemoveAt(i);}
            }
            UpdateHud();
            AnimateDevelopment(dt);
            goldHud?.Tick(dt);
        }
        void HandleInput()
        {
            var keyboard=Keyboard.current;
            if(keyboard!=null) {
                if(keyboard.digit1Key.wasPressedThisFrame)SelectBomb();
                if(keyboard.digit2Key.wasPressedThisFrame)SelectNuke();
                if(keyboard.spaceKey.wasPressedThisFrame)TogglePause();
                if(keyboard.escapeKey.wasPressedThisFrame){CancelDrag();return;}
            }
            if(speed==0){CancelDrag();return;}
            Vector2 pointer=default;bool down=false,held=false,up=false,touchInput=false;int pointerId=-1;
            if(draggingTouch || (!dragging&&Touchscreen.current!=null&&Touchscreen.current.primaryTouch.press.wasPressedThisFrame)){
                if(Touchscreen.current==null){CancelDrag();return;}var touch=Touchscreen.current.primaryTouch;touchInput=true;pointerId=touch.touchId.ReadValue();
                if(dragging&&pointerId!=dragPointerId){CancelDrag();return;}
                pointer=touch.position.ReadValue();down=touch.press.wasPressedThisFrame;held=touch.press.isPressed;up=touch.press.wasReleasedThisFrame;
            }else if(Mouse.current!=null){pointer=Mouse.current.position.ReadValue();down=Mouse.current.leftButton.wasPressedThisFrame;held=Mouse.current.leftButton.isPressed;up=Mouse.current.leftButton.wasReleasedThisFrame;}
            bool overUI=EventSystem.current&&(touchInput?EventSystem.current.IsPointerOverGameObject(pointerId):EventSystem.current.IsPointerOverGameObject());
            if(down&&!dragging){
                if(overUI||!cityCamera.pixelRect.Contains(pointer))return;
                GroundPoint(pointer,out dragStart);
                dragging=true;draggingTouch=touchInput;dragPointerId=pointerId;dragScreenStart=pointer;dragEnd=dragStart;dragNuclear=nuclear;
            }
            if(!dragging)return;
            bool valid=GroundPoint(pointer,out var point);
            if(valid)dragEnd=point;
            DrawDragGuide(pointer,cityCamera.pixelRect.Contains(pointer)&&!overUI);
            if(up){bool validStart=GroundPoint(dragScreenStart,out var a);var b=dragEnd;bool useNuke=dragNuclear;bool launch=validStart&&valid&&!overUI&&(pointer-dragScreenStart).sqrMagnitude>=144&&(b-a).magnitude>=cellSize*.25f;CancelDrag();if(launch)LaunchAirstrike(a,b,useNuke);}
            else if(!held)CancelDrag();
        }
        bool GroundPoint(Vector2 pointer,out Vector3 point)
        {
            point=default;if(!cityCamera.pixelRect.Contains(pointer))return false;
            var ray=cityCamera.ScreenPointToRay(pointer);var plane=new Plane(Vector3.up,Vector3.zero);if(!plane.Raycast(ray,out float distance))return false;point=ray.GetPoint(distance);return true;
        }
        void CancelDrag(){dragging=false;draggingTouch=false;SetDragGuideVisible(false);}
        void SetDragGuideVisible(bool visible){if(dragGuideSegments==null)return;foreach(var line in dragGuideSegments)if(line)line.gameObject.SetActive(visible);}
        void DrawDragGuide(Vector2 pointer,bool valid)
        {
            if(!valid||(pointer-dragScreenStart).sqrMagnitude<144||dragGuideSegments==null||dragGuideSegments.Length!=3){SetDragGuideVisible(false);return;}
            var canvas=dragGuideSegments[0].parent as RectTransform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,dragScreenStart,null,out var start)||!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,pointer,null,out var end)){SetDragGuideVisible(false);return;}
            var arrow=DragGuideGeometry.Arrow(new RoutePoint(start.x,start.y),new RoutePoint(end.x,end.y),dragArrowHeadSize);
            SetDragGuideVisible(true);PlaceGuide(dragGuideSegments[0],arrow[0],arrow[1]);PlaceGuide(dragGuideSegments[1],arrow[1],arrow[2]);PlaceGuide(dragGuideSegments[2],arrow[1],arrow[3]);
        }
        static void PlaceGuide(RectTransform line,RoutePoint a,RoutePoint b)
        {
            var start=new Vector2((float)a.X,(float)a.Y);var end=new Vector2((float)b.X,(float)b.Y);var delta=end-start;
            line.anchoredPosition=(start+end)*.5f;line.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,delta.magnitude);line.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
        void OnApplicationFocus(bool focus){if(!focus)CancelDrag();else if(language)language.RefreshSystem();}
        void OnDisable(){CancelDrag();}
        float FlightAltitude(){float altitude=cellSize*1.2f;foreach(var view in views.Values)altitude=Mathf.Max(altitude,view.Bounds.max.y+cellSize*.3f);return altitude;}
        bool DirectionalTargets(Vector3 a,Vector3 b,out Vector3 start,out Vector3 end)
        {
            double half=Math.Max(cellSize*4,cameraDistance*1.3);
            DirectionalRoute.AcrossRectangle(new RoutePoint(a.x,a.z),new RoutePoint(b.x,b.z),focus.x-half,focus.z-half,focus.x+half,focus.z+half,out var first,out var last);
            start=new Vector3((float)first.X,0,(float)first.Y);end=new Vector3((float)last.X,0,(float)last.Y);
            float altitude=FlightAltitude();
            foreach(var plane in GeometryUtility.CalculateFrustumPlanes(cityCamera)){
                float da=plane.GetDistanceToPoint(start+Vector3.up*altitude),db=plane.GetDistanceToPoint(end+Vector3.up*altitude);
                if(da<0&&db<0)return false;if(da>=0&&db>=0)continue;
                var crossing=Vector3.Lerp(start,end,da/(da-db));if(da<0)start=crossing;else end=crossing;
            }
            return (end-start).sqrMagnitude>=cellSize*cellSize*.0625f;
        }
        void OffscreenFlight(Vector3 start,Vector3 end,Vector3[] offsets,out Vector3 entry,out Vector3 exit)
        {
            var a=cityCamera.WorldToViewportPoint(start);var b=cityCamera.WorldToViewportPoint(end);
            float margin=.25f+simulation.Upgrades.AircraftLevel*.025f;
            var middle=(start+end)*.5f;var screenMiddle=cityCamera.WorldToViewportPoint(middle);foreach(var offset in offsets){var point=cityCamera.WorldToViewportPoint(middle+offset);margin=Mathf.Max(margin,.35f+Mathf.Max(Mathf.Abs(point.x-screenMiddle.x),Mathf.Abs(point.y-screenMiddle.y))*2);}
            DirectionalRoute.AcrossRectangle(new RoutePoint(a.x,a.y),new RoutePoint(b.x,b.y),-margin,-margin,1+margin,1+margin,out var first,out var last);
            entry=AirPoint(first,start.y,Mathf.Max(cellSize*2,(a.z+b.z)*.5f));exit=AirPoint(last,start.y,Mathf.Max(cellSize*2,(a.z+b.z)*.5f));
        }
        Vector3 AirPoint(RoutePoint point,float altitude,float depth)
        {
            var viewport=new Vector3((float)point.X,(float)point.Y,depth);var ray=cityCamera.ViewportPointToRay(viewport);var plane=new Plane(Vector3.up,new Vector3(0,altitude,0));
            if(plane.Raycast(ray,out float distance)&&distance<cityCamera.farClipPlane*2)return ray.GetPoint(distance);
            return cityCamera.ViewportToWorldPoint(viewport);
        }
        void LaunchAirstrike(Vector3 start,Vector3 end,bool useNuke)
        {
            if(!aircraftPrefab)return;
            if(useNuke?!nuclearCooldown.Ready:!sortieCharge.Ready){feedback=useNuke?F("nuclearCooldown",Math.Ceiling(nuclearCooldown.Remaining)):F("recharge",sortieCharge.Remaining.ToString("0.0"));feedbackUntil=simulationTime+2;return;}
            if(!DirectionalTargets(start,end,out start,out end)){feedback=L("drag");feedbackUntil=simulationTime+3;return;}
            var plan=new AirstrikePlan(start.x,start.z,end.x,end.z,cellSize,useNuke,simulation.Upgrades.BombLevel,true,simulation.Upgrades.DropRate,simulation.Upgrades.SpeedMultiplier);
            float radiusMultiplier=(float)simulation.Upgrades.RadiusMultiplier;
            float damageMultiplier=(float)simulation.Upgrades.DamageMultiplier;
            int count=simulation.Upgrades.AircraftCount;var offsets=new Vector3[count];var forward=(end-start).normalized;var right=Vector3.Cross(Vector3.up,forward);
            for(int i=0;i<count;i++){var offset=VFormation.Offset(i,cellSize*1.45);offsets[i]=right*(float)offset.X-forward*(float)offset.Y;}
            float altitude=FlightAltitude();
            var airStart=start+Vector3.up*altitude;var airEnd=end+Vector3.up*altitude;
            OffscreenFlight(airStart,airEnd,offsets,out var entry,out var exit);
            if(useNuke){if(!nuclearCooldown.Launch())return;}else if(!sortieCharge.Launch())return;
            sortieOrders.Enqueue(new SortieOrder{Plan=plan,Offsets=offsets,Altitude=altitude,Nuclear=useNuke,Radius=radiusMultiplier,Damage=damageMultiplier,Scale=1+simulation.Upgrades.AircraftLevel*.025f,Health=simulation.Upgrades.AircraftHealth,Armor=simulation.Upgrades.Armor,Entry=entry,Exit=exit});SpawnQueuedSorties();
            focusVelocity=Vector3.zero;distanceVelocity=0;
            feedback=F("inbound",count,plan.BombsPerSecond.ToString("0.0"));feedbackUntil=simulationTime+3;
        }
        void SpawnQueuedSorties()
        {
            int active=0;foreach(var run in airstrikes)active+=run.Units.Length;
            while(sortieOrders.Count>0&&active+sortieOrders.Peek().Offsets.Length<=42){var order=sortieOrders.Dequeue();int count=order.Offsets.Length;var members=new GameObject[count];
                for(int i=0;i<count;i++){var aircraft=new GameObject("Bomber "+(i+1));aircraft.transform.SetParent(worldRoot,false);var model=Instantiate(aircraftPrefab,aircraft.transform);PrepareModel(model);var bounds=BoundsOf(model);model.transform.localScale*=cellSize*1.8f/Mathf.Max(.1f,bounds.size.magnitude);bounds=BoundsOf(model);model.transform.position+=aircraft.transform.position-bounds.center;aircraft.transform.localScale=Vector3.one*order.Scale;members[i]=aircraft;}
                airstrikes.Add(new CityAirstrike(order.Plan,worldRoot,members,order.Offsets,order.Altitude,cellSize,order.Nuclear,rubbleMaterial,(p,n)=>Detonate(p,n,order.Radius,order.Damage),order.Entry,order.Exit,order.Health,order.Armor,cityCamera,OnAircraftArrival));active+=count;
            }
        }
        IEnumerable<AircraftUnit> AirTargets(){foreach(var run in airstrikes)foreach(var unit in run.Units)yield return unit;}
        Vector3 MissileRoof(Lot lot){if(views.TryGetValue(lot.Cell,out var view))return new Vector3(view.Bounds.center.x,view.Bounds.max.y+.5f,view.Bounds.center.z);return World(lot.Cell)+Vector3.up*cellSize;}
        void OnAircraftArrival(AircraftUnit unit){sortieCharge.Arrive();feedback=L("arrived");feedbackUntil=simulationTime+2;}
        void OnAircraftHit(AircraftUnit unit,bool destroyed){shake=Mathf.Min(1.5f,shake+(destroyed?.45f:.12f));if(destroyed){SpawnBuildingBurst(unit.Position,cellSize*.5f,true);if(impactAudio)impactAudio.PlayOneShot(bombAudio,.5f);}}
        void SpawnAirExplosion(Vector3 position,float radius){effects.RemoveAll(e=>!e);if(effects.Count>=12){Destroy(effects[0].gameObject);effects.RemoveAt(0);}var root=new GameObject("Air defense explosion");root.transform.SetParent(worldRoot,false);root.transform.position=position;var effect=root.AddComponent<CityShockwave>();effect.Initialize(radius,false,flashMaterial,ringMaterial);effects.Add(effect);}
        void UpdateAircraftHud()
        {
            if(aircraftHealthRoots==null||aircraftHealthFills==null||aircraftHealthWidths==null)return;int slot=0;foreach(var unit in AirTargets()){if(!unit.Targetable||slot>=aircraftHealthRoots.Length||slot>=aircraftHealthFills.Length)continue;int index=slot++;var root=aircraftHealthRoots[index];var fill=aircraftHealthFills[index];if(!root||!fill)continue;var viewport=cityCamera.WorldToViewportPoint(unit.Position);bool visible=viewport.z>0&&viewport.x>=0&&viewport.x<=1&&viewport.y>=0&&viewport.y<=1;root.gameObject.SetActive(visible);if(!visible)continue;RectTransformUtility.ScreenPointToLocalPointInRectangle(root.parent as RectTransform,cityCamera.WorldToScreenPoint(unit.Position+Vector3.up*cellSize*.45f),null,out var point);root.anchoredPosition=point;fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,aircraftHealthWidths[index]*(float)(unit.Health.Current/unit.Health.Maximum));}
            for(int i=slot;i<aircraftHealthRoots.Length;i++)if(aircraftHealthRoots[i])aircraftHealthRoots[i].gameObject.SetActive(false);
        }
        public void Detonate(Vector3 point,bool useNuke)
            => Detonate(point,useNuke,simulation==null?1:(float)simulation.Upgrades.RadiusMultiplier,simulation==null?1:(float)simulation.Upgrades.DamageMultiplier);
        void Detonate(Vector3 point,bool useNuke,float radiusMultiplier,float damageMultiplier)
        {
            if(simulation==null || speed==0)return;
            float radius=(useNuke?nuclearRadiusCells:bombRadiusCells)*cellSize*radiusMultiplier;
            var result=simulation.DamageBlast(point.x/cellSize,point.z/cellSize,radius/cellSize,simulationTime,(useNuke?nuclearDamage:bombDamage)*damageMultiplier);
            foreach(var chain in result.Explosions)if(pendingChains.Count<48)pendingChains.Add(new PendingChain{Chain=chain,At=simulationTime+Math.Min(.9,chain.Depth*.12)});
            foreach(var hit in result.Hit)if(views.TryGetValue(hit.Cell,out var struck)){struck.HitAge=0;AnimateHit(struck,0);}
            foreach(var lot in result.Destroyed) {
                if(!views.TryGetValue(lot.Cell,out var view))continue;
                views.Remove(lot.Cell);BeginCollapse(view,point,false);
                SpawnBuildingBurst(view.Bounds.center,cellSize*.65f,true);
            }
            var cleared=new List<Cell>(result.DestroyedCells);
            foreach(var change in result.Downgrades){
                if(views.TryGetValue(change.Before.Cell,out var old)){views.Remove(change.Before.Cell);BeginCollapse(old,point,true);SpawnBuildingBurst(old.Bounds.center,cellSize*.5f,false);}
                AddBuilding(change.After);var replacement=views[change.After.Cell];replacement.Evolving=false;replacement.HitAge=0;
                cleared.AddRange(change.FreedCells);
            }
            foreach(var cell in cleared){
                var position=World(cell);var crater=GameObject.CreatePrimitive(PrimitiveType.Cylinder);crater.name="Recovering lot "+cell;
                crater.transform.SetParent(worldRoot,false);crater.transform.position=position+Vector3.up*.03f;crater.transform.localScale=new Vector3(cellSize*.76f,.035f,cellSize*.76f);
                Destroy(crater.GetComponent<Collider>());crater.GetComponent<Renderer>().sharedMaterial=rubbleMaterial;scars[cell]=crater;
                ScatterDebris(position,point,cell.GetHashCode());
            }
            if(cleared.Count>0)roadsDirty=true;
            SpawnShockwave(point,radius,useNuke);
            if(impactAudio)impactAudio.PlayOneShot(useNuke?nukeAudio:bombAudio);
            shake=Mathf.Min(1.5f,shake+(useNuke?1.1f:result.Destroyed.Count>0?.48f:result.Downgrades.Count>0?.3f:result.Hit.Count>0?.15f:.06f));
            if(result.Points>0){goldHud?.Earn(result.Points,point+Vector3.up*cellSize,cityCamera);if(impactAudio)impactAudio.PlayOneShot(goldAudio,.45f);}
            feedback=result.Explosions.Count>0?F("chain",result.Explosions.Count,result.Points.ToString("N0")):result.Hit.Count==0?L("empty"):result.Points>0?F("reward",result.Points.ToString("N0"),result.DevelopmentLost):result.Downgrades.Count>0?F("downgrade",result.DevelopmentLost):L("hit");
            feedbackUntil=simulationTime+3;
        }
        void BeginCollapse(BuildingView view,Vector3 blast,bool downgrade)
        {
            view.Falling=true;view.Downgrading=downgrade;view.Age=0;view.BlastPoint=blast;view.Position=view.Root.transform.position;view.Rotation=view.Root.transform.rotation;view.Root.transform.localScale=Vector3.one;
            var direction=view.Position-blast;direction.y=0;if(direction.sqrMagnitude<.1f){float angle=(view.Lot.Style%360)*Mathf.Deg2Rad;direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));}view.FallDirection=direction.normalized;
            view.CollapseMode=downgrade?1:DestructionMotion.Mode(view.Lot.Style);view.CollapseDuration=downgrade?.7f:(float)DestructionMotion.Duration(view.CollapseMode);view.CollapsePivot=view.Position+view.FallDirection*Mathf.Max(view.Bounds.size.x,view.Bounds.size.z)*.42f;
            foreach(var collider in view.Root.GetComponentsInChildren<Collider>())collider.enabled=false;falling.Add(view);
        }
        void RegisterStructuralPiece(GameObject piece)
        {
            while(structuralDebris.Count>=36){var old=structuralDebris.Dequeue();if(old)Destroy(old);}structuralDebris.Enqueue(piece);
            while(debris.Count>=Mathf.Max(1,maxDebris)){var old=debris.Dequeue();if(old)Destroy(old);}debris.Enqueue(piece);
        }
        Material SpecialMaterial(BuildingKind kind)=>kind==BuildingKind.FuelDepot?fuelDepotMaterial:kind==BuildingKind.AmmunitionDepot?ammunitionDepotMaterial:powerStationMaterial;
        void PlayPendingChains()
        {
            if(speed<=0)return;int played=0;for(int i=0;i<pendingChains.Count&&played<3;){var pending=pendingChains[i];if(simulationTime<pending.At){i++;continue;}pendingChains.RemoveAt(i);played++;
                var chain=pending.Chain;var point=World(chain.Cell);effects.RemoveAll(e=>!e);if(effects.Count>=12){Destroy(effects[0].gameObject);effects.RemoveAt(0);}
                var root=new GameObject("Chain explosion / "+chain.Kind);root.transform.SetParent(worldRoot,false);root.transform.position=point+Vector3.up*.3f;var effect=root.AddComponent<CityShockwave>();effect.Initialize((float)chain.Radius*cellSize,false,SpecialMaterial(chain.Kind),ringMaterial);effects.Add(effect);
                SpawnBuildingBurst(point+Vector3.up*cellSize*.5f,cellSize,true);shake=Mathf.Min(1.5f,shake+.32f);if(impactAudio)impactAudio.PlayOneShot(bombAudio,.55f);
            }
        }
        void AddBuilding(Lot lot)
        {
            foreach(var cell in lot.Cells)if(scars.TryGetValue(cell,out var scar)){Destroy(scar);scars.Remove(cell);}
            Footprint(lot,out var center,out var size);
            var root=new GameObject(lot.Kind+" / "+lot.Cells.Count+" cells / "+lot.Generation);root.transform.SetParent(worldRoot,false);root.transform.position=center;
            var pad=GameObject.CreatePrimitive(PrimitiveType.Cube);pad.name="Shared foundation";pad.transform.SetParent(root.transform,false);pad.transform.localPosition=new Vector3(0,-.08f,0);pad.transform.localScale=new Vector3(size.x,.12f,size.z);pad.GetComponent<Renderer>().sharedMaterial=lotMaterial;Destroy(pad.GetComponent<Collider>());
            var catalogue=lot.Kind==BuildingKind.Apartment?midrises:lot.Kind==BuildingKind.Tower?towers:homes;if(catalogue==null||catalogue.Length==0)catalogue=homes;
            var prefab=lot.Kind==BuildingKind.Shop&&shopPrefab?shopPrefab:lot.Kind==BuildingKind.Office&&officePrefab?officePrefab:lot.Kind==BuildingKind.HouseA?homes[0]:lot.Kind==BuildingKind.HouseB?homes[Mathf.Min(1,homes.Length-1)]:catalogue[lot.Style%catalogue.Length];
            if(lot.IsSpecial){prefab=lot.Kind==BuildingKind.FuelDepot?fuelDepotPrefab:lot.Kind==BuildingKind.AmmunitionDepot?ammunitionDepotPrefab:powerStationPrefab;pad.GetComponent<Renderer>().sharedMaterial=SpecialMaterial(lot.Kind);}
            var model=Instantiate(prefab,root.transform);
            model.transform.localRotation=Quaternion.identity;
            PrepareModel(model);
            if(lot.IsSpecial&&volatileBuildingMaterial)foreach(var renderer in model.GetComponentsInChildren<Renderer>()){var mats=renderer.sharedMaterials;for(int i=0;i<mats.Length;i++)mats[i]=volatileBuildingMaterial;renderer.sharedMaterials=mats;}
            var bounds=BoundsOf(model);float sx=size.x/Mathf.Max(.1f,bounds.size.x),sz=size.z/Mathf.Max(.1f,bounds.size.z);model.transform.localScale=Vector3.Scale(model.transform.localScale,new Vector3(sx,Mathf.Min(sx,sz),sz));
            bounds=BoundsOf(model);float maxHeight=cellSize*(2.2f+lot.Tier*.6f);if(bounds.size.y>maxHeight){var s=model.transform.localScale;s.y*=maxHeight/bounds.size.y;model.transform.localScale=s;}
            // Fit local axes before turning. A rectangular two-cell building can only turn 0/180 degrees.
            model.transform.localRotation=Quaternion.Euler(0,(lot.Cells.Count==2?(lot.Style%2)*2:lot.Style%4)*90,0);
            bounds=BoundsOf(model);model.transform.position+=new Vector3(root.transform.position.x-bounds.center.x,.02f-bounds.min.y,root.transform.position.z-bounds.center.z);
            bounds=BoundsOf(model);
            var collider=root.AddComponent<BoxCollider>();collider.center=root.transform.InverseTransformPoint(bounds.center);collider.size=bounds.size;root.layer=8;
            var surfaces=new List<HitSurface>();foreach(var renderer in model.GetComponentsInChildren<Renderer>()){var mats=renderer.sharedMaterials;for(int i=0;i<mats.Length;i++){var original=new MaterialPropertyBlock();renderer.GetPropertyBlock(original,i);var flash=new MaterialPropertyBlock();renderer.GetPropertyBlock(flash,i);surfaces.Add(new HitSurface{Renderer=renderer,Index=i,Original=original,Flash=flash,Color=mats[i]&&mats[i].HasProperty("_BaseColor")?mats[i].GetColor("_BaseColor"):Color.white});}}
            var view=new BuildingView{Lot=lot,Root=root,Bounds=bounds,Position=root.transform.position,Evolving=lot.Tier>0,Visual=model.transform,VisualPosition=model.transform.localPosition,VisualRotation=model.transform.localRotation,Surfaces=surfaces.ToArray()};views.Add(lot.Cell,view);
            root.transform.localScale=Vector3.one*.001f;
        }
        void Footprint(Lot lot,out Vector3 center,out Vector3 size)
        {
            int minX=int.MaxValue,maxX=int.MinValue,minZ=int.MaxValue,maxZ=int.MinValue;foreach(var cell in lot.Cells){minX=Mathf.Min(minX,cell.X);maxX=Mathf.Max(maxX,cell.X);minZ=Mathf.Min(minZ,cell.Z);maxZ=Mathf.Max(maxZ,cell.Z);}
            center=new Vector3((minX+maxX)*cellSize*.5f,0,(minZ+maxZ)*cellSize*.5f);size=new Vector3((maxX-minX+1)*cellSize-cellSize*(1-buildingFill),.12f,(maxZ-minZ+1)*cellSize-cellSize*(1-buildingFill));
        }
        void SynchronizeBuildings()
        {
            var removed=new List<Cell>();foreach(var pair in views)if(!simulation.Buildings.ContainsKey(pair.Value.Lot.Generation)){Destroy(pair.Value.Root);removed.Add(pair.Key);}foreach(var cell in removed)views.Remove(cell);
            foreach(var lot in simulation.Buildings.Values)if(!views.ContainsKey(lot.Cell))AddBuilding(lot);
            ReconcileRoads();
        }
        void PrepareModel(GameObject model)
        {
            foreach(var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))Destroy(collider);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)) {
                var mats=renderer.sharedMaterials;
                for(int i=0;i<mats.Length;i++)if(!mats[i] || !mats[i].shader || !mats[i].shader.name.StartsWith("Universal Render Pipeline/",StringComparison.Ordinal))mats[i]=fallbackMaterial;
                renderer.sharedMaterials=mats;
            }
        }
        static Bounds BoundsOf(GameObject model)
        {
            var renderers=model.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return new Bounds(model.transform.position,Vector3.one);
            var bounds=renderers[0].bounds;for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);return bounds;
        }
        void AnimateHit(BuildingView view,float dt)
        {
            if(view.HitAge>=.28f||!view.Visual)return;
            view.HitAge+=dt;float strength=1-Mathf.Clamp01(view.HitAge/.28f);
            view.Visual.localPosition=view.VisualPosition+new Vector3(Mathf.Sin(view.HitAge*125),0,Mathf.Cos(view.HitAge*111))*cellSize*.035f*strength;
            view.Visual.localRotation=view.VisualRotation*Quaternion.Euler(0,0,Mathf.Sin(view.HitAge*95)*4*strength);
            foreach(var surface in view.Surfaces){if(!surface.Renderer)continue;
                if(strength<=0)surface.Renderer.SetPropertyBlock(surface.Original.isEmpty?null:surface.Original,surface.Index);
                else {surface.Flash.SetColor("_BaseColor",Color.Lerp(surface.Color,new Color(1,.04f,.025f),strength*.9f));surface.Renderer.SetPropertyBlock(surface.Flash,surface.Index);}
            }
            if(strength<=0){view.Visual.localPosition=view.VisualPosition;view.Visual.localRotation=view.VisualRotation;}
        }
        void SpawnBuildingBurst(Vector3 point,float size,bool destroyed)
        {
            buildingBursts.RemoveAll(b=>!b);if(buildingBursts.Count>=8){Destroy(buildingBursts[0].gameObject);buildingBursts.RemoveAt(0);}
            var root=new GameObject(destroyed?"Building explosion":"Building downgrade burst");root.transform.SetParent(worldRoot,false);root.transform.position=point;
            var burst=root.AddComponent<CityBuildingBurst>();burst.Initialize(size,flashMaterial,rubbleMaterial,destroyed);buildingBursts.Add(burst);
        }
        void AnimateBirth(BuildingView view,float dt)
        {
            if(view.Evolving){
                if(view.Age>=1.1f)return;
                float before=view.Age;view.Age+=dt;float evolutionT=Mathf.Clamp01(view.Age/1.1f);
                float scale=GrowthFeedback.EvolutionScale(evolutionT);
                view.Root.transform.localScale=new Vector3(scale,scale,scale);
                view.Root.transform.position=view.Position+Vector3.up*(GrowthFeedback.EvolutionLift(evolutionT)*cellSize*.4f);
                if(before<.77f&&view.Age>=.77f)SpawnEvolutionRing(view.Position,cellSize*(view.Lot.Cells.Count==4?1.5f:1.05f));
                return;
            }
            if(view.Age>=.65f)return;view.Age+=dt;float t=Mathf.Clamp01(view.Age/.65f);
            float overshoot=1+2.70158f*Mathf.Pow(t-1,3)+1.70158f*Mathf.Pow(t-1,2);
            view.Root.transform.localScale=Vector3.one*Mathf.Max(.001f,overshoot);
        }
        void SpawnEvolutionRing(Vector3 point,float radius)
        {
            effects.RemoveAll(e=>!e);if(effects.Count>=12){Destroy(effects[0].gameObject);effects.RemoveAt(0);}
            var root=new GameObject("Evolution landing ring");root.transform.SetParent(worldRoot,false);root.transform.position=point+Vector3.up*.16f;
            var effect=root.AddComponent<CityShockwave>();effect.InitializeEvolution(radius,ringMaterial);effects.Add(effect);
        }
        void AnimateDevelopment(float dt)
        {
            developmentAge+=dt;gainAge+=dt;
            float t=Mathf.Clamp01(developmentAge/.65f);
            displayedDevelopment=Mathf.Lerp(developmentFrom,lastDevelopment,1-Mathf.Pow(1-t,3));
            if(developmentText){developmentText.text=Math.Round(displayedDevelopment).ToString("N0");developmentText.rectTransform.localScale=developmentScale*(1+Mathf.Sin(t*Mathf.PI)*.16f);}
            if(developmentGainText){float g=Mathf.Clamp01(gainAge/1.3f);developmentGainText.rectTransform.anchoredPosition=gainOrigin+Vector2.up*(g*34);developmentGainText.canvasRenderer.SetAlpha(g<.65f?1:1-(g-.65f)/.35f);}
        }
        Vector3 World(Cell cell) => new Vector3(cell.X*cellSize,0,cell.Z*cellSize);
        void ReconcileRoads()
        {
            bool changed=false;
            var wanted=new Dictionary<string,Vector4>();var layout=RoadTopology.Build(simulation.Living.Keys,roadBlockSize,simulation.SharedLinks);
            float span=cellSize*roadBlockSize,edgeOffset=-cellSize*.5f;
            foreach(var edge in layout.Horizontal)wanted["H:"+edge]=new Vector4(edge.X*span+edgeOffset+span*.5f,edge.Z*span+edgeOffset,90,0);
            foreach(var edge in layout.Vertical)wanted["V:"+edge]=new Vector4(edge.X*span+edgeOffset,edge.Z*span+edgeOffset+span*.5f,0,0);
            foreach(var node in layout.Junctions)wanted["J:"+node.Key+":"+node.Value.Mask]=new Vector4(node.Key.X*span+edgeOffset,node.Key.Z*span+edgeOffset,node.Value.QuarterTurns*90,node.Value.Mask);
            var obsolete=new List<string>();foreach(var item in roads)if(!wanted.ContainsKey(item.Key)) {Destroy(item.Value);obsolete.Add(item.Key);changed=true;}foreach(var key in obsolete)roads.Remove(key);
            foreach(var item in wanted) {
                if(roads.ContainsKey(item.Key))continue;bool junction=item.Value.w>.5f;changed=true;
                var shape=junction?RoadTopology.Resolve((int)item.Value.w).Shape:RoadShape.Straight;
                var prefab=!junction?roadStraight:shape==RoadShape.Corner?roadCorner:shape==RoadShape.Tee?roadTee:shape==RoadShape.End?roadEnd:shape==RoadShape.Cross?roadIntersection:roadStraight;if(!prefab)prefab=roadIntersection;if(!prefab)continue;
                var root=new GameObject("Street "+item.Key);root.transform.SetParent(worldRoot,false);root.transform.position=new Vector3(item.Value.x,-.04f,item.Value.y);
                var model=Instantiate(prefab,root.transform);PrepareModel(model);var b=BoundsOf(model);
                // A source road tile is 30 units long. Use one shared tile scale at all joins.
                float roadScale=streetWidth/8f,junctionSpan=30*roadScale;
                model.transform.localScale=junction?Vector3.one*roadScale:new Vector3(roadScale,roadScale,(span-junctionSpan)/30f);
                b=BoundsOf(model);model.transform.position+=Vector3.up*(-.025f-b.min.y);
                root.transform.rotation=Quaternion.Euler(0,item.Value.z,0);roads.Add(item.Key,root);
            }
            var liveLinks=new HashSet<string>();foreach(var link in simulation.SharedLinks){if(!link.District)continue;string key=link.A+"/"+link.B;liveLinks.Add(key);if(districtPaths.ContainsKey(key))continue;var a=World(link.A);var b=World(link.B);var plaza=GameObject.CreatePrimitive(PrimitiveType.Cube);plaza.name="Shared commercial promenade";plaza.transform.SetParent(worldRoot,false);plaza.transform.position=(a+b)*.5f+Vector3.up*.005f;plaza.transform.rotation=Quaternion.LookRotation(b-a);plaza.transform.localScale=new Vector3(streetWidth*2,.05f,cellSize);plaza.GetComponent<Renderer>().sharedMaterial=districtMaterial?districtMaterial:lotMaterial;Destroy(plaza.GetComponent<Collider>());districtPaths[key]=plaza;}
            var oldLinks=new List<string>();foreach(var pair in districtPaths)if(!liveLinks.Contains(pair.Key)){Destroy(pair.Value);oldLinks.Add(pair.Key);}foreach(var key in oldLinks)districtPaths.Remove(key);
            if(changed&&roadBatch!=null)roadBatch.Rebuild(roads.Values);
        }
        void ScatterDebris(Vector3 position,Vector3 blast,int style)
        {
            if(maxDebris<=0)return;
            for(int i=0;i<5;i++) {
                while(debris.Count>=maxDebris) {var old=debris.Dequeue();if(old)Destroy(old);}
                var piece=GameObject.CreatePrimitive(PrimitiveType.Cube);piece.name="Debris";piece.transform.SetParent(worldRoot,false);
                float angle=(style+i*73)*Mathf.Deg2Rad;var outward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                piece.transform.position=position+outward*2+Vector3.up*(2+i*.4f);piece.transform.localScale=new Vector3(.55f+(i%3)*.8f,.35f+(i%2)*.6f,.7f+(i%3)*.5f);
                piece.GetComponent<Renderer>().sharedMaterial=rubbleMaterial;
                var body=piece.AddComponent<Rigidbody>();body.mass=1;body.AddForce(outward*cellSize*(.8f+i*.32f)+Vector3.up*cellSize*(.7f+i*.23f),ForceMode.VelocityChange);body.AddTorque(new Vector3(i+1,3,2),ForceMode.VelocityChange);
                Destroy(piece,3.2f+i*.2f);debris.Enqueue(piece);
            }
        }
        void SpawnShockwave(Vector3 point,float radius,bool large)
        {
            effects.RemoveAll(e=>!e);
            if(effects.Count>=12) {Destroy(effects[0].gameObject);effects.RemoveAt(0);}
            var root=new GameObject(large?"Nuclear shockwave":"Blast shockwave");root.transform.SetParent(worldRoot,false);root.transform.position=new Vector3(point.x,.15f,point.z);
            var effect=root.AddComponent<CityShockwave>();effect.Initialize(radius,large,flashMaterial,ringMaterial);effects.Add(effect);
        }
        void LateUpdate()
        {
            if(simulation==null)return;
            if(dragging)return;
            bool any=false;Bounds bounds=default;
            foreach(var view in views.Values) {if(!any){bounds=view.Bounds;any=true;}else bounds.Encapsulate(view.Bounds);foreach(var cell in view.Lot.Cells){bounds.Encapsulate(World(cell)+new Vector3(cellSize*.65f,0,cellSize*.65f));bounds.Encapsulate(World(cell)-new Vector3(cellSize*.65f,0,cellSize*.65f));}}
            if(!any)bounds=new Bounds(focus,new Vector3(cellSize,cellSize,cellSize));
            if(snapCityCamera){focus=bounds.center;focusVelocity=Vector3.zero;}else if(airstrikes.Count==0)focus=Vector3.SmoothDamp(focus,bounds.center,ref focusVelocity,.7f);
            var rotation=Quaternion.Euler(cameraPitch,45,0);var inverse=Quaternion.Inverse(rotation);float targetDistance=cellSize*2.2f;
            float tangent=Mathf.Tan(30*Mathf.Deg2Rad),aspect=Mathf.Max(.2f,cityCamera.aspect);
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2) {var p=inverse*(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z))-focus);targetDistance=Mathf.Max(targetDistance,Mathf.Abs(p.y)*1.15f/tangent-p.z,Mathf.Abs(p.x)*1.15f/(tangent*aspect)-p.z);}
            targetDistance=Mathf.Max(cellSize*2,targetDistance*cameraZoom);
            if(snapCityCamera){cameraDistance=targetDistance;distanceVelocity=0;snapCityCamera=false;}else if(airstrikes.Count==0)cameraDistance=Mathf.SmoothDamp(cameraDistance,targetDistance,ref distanceVelocity,.8f);
            cityCamera.orthographic=false;cityCamera.fieldOfView=60;cityCamera.clearFlags=CameraClearFlags.Skybox;cityCamera.farClipPlane=Mathf.Max(1500,cameraDistance*6);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=Mathf.Max(150,cameraDistance*1.6f);RenderSettings.fogEndDistance=Mathf.Max(500,cameraDistance*4);RenderSettings.fogColor=new Color(.68f,.78f,.82f);
            shake=Mathf.MoveTowards(shake,0,Time.unscaledDeltaTime*2);
            var offset=new Vector3(Mathf.Sin(Time.unscaledTime*77),Mathf.Sin(Time.unscaledTime*93),0)*shake;
            cityCamera.transform.SetPositionAndRotation(focus-rotation*Vector3.forward*cameraDistance+offset,rotation);
            if(ground)ground.localScale=new Vector3(1,1,1)*Mathf.Max(400,(bounds.size.magnitude+cameraDistance*12)/10);
        }
        void UpdateHud()
        {
            if(simulation==null)return;
            if(lastGold!=simulation.Gold){if(goldHud!=null)goldHud.SetTotal(simulation.Gold);else if(scoreText)scoreText.text=simulation.Gold.ToString("N0");lastGold=simulation.Gold;}
            if(lastRadiusLevel!=simulation.Upgrades.RadiusLevel||lastBombLevel!=simulation.Upgrades.BombLevel){
                var upgrades=simulation.Upgrades;
                if(rangeUpgradeInfo)rangeUpgradeInfo.text=L("level")+upgrades.RadiusLevel+"  x"+upgrades.RadiusMultiplier.ToString("0.00")+" / "+(upgrades.RadiusCost>0?upgrades.RadiusCost.ToString("N0")+" G":L("max"));
                if(bombUpgradeInfo)bombUpgradeInfo.text=L("level")+upgrades.BombLevel+"  +"+(upgrades.BombLevel*2)+"/+"+upgrades.BombLevel+" / "+(upgrades.BombCost>0?upgrades.BombCost.ToString("N0")+" G":L("max"));
                lastRadiusLevel=upgrades.RadiusLevel;lastBombLevel=upgrades.BombLevel;
            }
            if(rangeUpgradeButton)rangeUpgradeButton.interactable=simulation.Upgrades.RadiusCost>0&&simulation.Gold>=simulation.Upgrades.RadiusCost;
            if(bombUpgradeButton)bombUpgradeButton.interactable=simulation.Upgrades.BombCost>0&&simulation.Gold>=simulation.Upgrades.BombCost;
            if(lastAircraftLevel!=simulation.Upgrades.AircraftLevel){var u=simulation.Upgrades;
                if(aircraftUpgradeInfo)aircraftUpgradeInfo.text=L("level")+u.AircraftLevel+" "+L("damage")+" x"+u.DamageMultiplier.ToString("0.0")+" / "+(u.AircraftCost>0?u.AircraftCost.ToString("N0")+" G":L("max"));lastAircraftLevel=u.AircraftLevel;
            }
            if(aircraftUpgradeButton)aircraftUpgradeButton.interactable=simulation.Upgrades.AircraftCost>0&&simulation.Gold>=simulation.Upgrades.AircraftCost;
            if(automaticGrowthInfo)automaticGrowthInfo.text=F("autoGrowth",simulation.CurrentGrowthRate(simulationTime).ToString("0.00"));
            if(lastDropRateLevel!=simulation.Upgrades.DropRateLevel||lastFormationLevel!=simulation.Upgrades.FormationLevel){var u=simulation.Upgrades;
                if(dropRateUpgradeInfo)dropRateUpgradeInfo.text=L("level")+u.DropRateLevel+" "+u.DropRate.ToString("0.0")+"/s / "+(u.DropRateCost>0?u.DropRateCost.ToString("N0")+" G":L("max"));
                if(formationUpgradeInfo)formationUpgradeInfo.text=u.AircraftCount+" "+L("aircraft")+" / "+(u.FormationCost>0?u.FormationCost.ToString("N0")+" G":L("max"));
                lastDropRateLevel=u.DropRateLevel;lastFormationLevel=u.FormationLevel;
            }
            if(dropRateUpgradeButton)dropRateUpgradeButton.interactable=simulation.Upgrades.DropRateCost>0&&simulation.Gold>=simulation.Upgrades.DropRateCost;
            if(formationUpgradeButton)formationUpgradeButton.interactable=simulation.Upgrades.FormationCost>0&&simulation.Gold>=simulation.Upgrades.FormationCost;
            if(lastSpeedLevel!=simulation.Upgrades.SpeedLevel||lastArmorLevel!=simulation.Upgrades.ArmorLevel||lastHealthLevel!=simulation.Upgrades.HealthLevel){var u=simulation.Upgrades;
                if(speedUpgradeInfo)speedUpgradeInfo.text=L("level")+u.SpeedLevel+" x"+u.SpeedMultiplier.ToString("0.00")+" / "+(u.SpeedCost>0?u.SpeedCost.ToString("N0")+" G":L("max"));
                if(armorUpgradeInfo)armorUpgradeInfo.text=L("level")+u.ArmorLevel+" "+u.Armor.ToString("0")+" "+L("armorStat")+" / "+(u.ArmorCost>0?u.ArmorCost.ToString("N0")+" G":L("max"));
                if(healthUpgradeInfo)healthUpgradeInfo.text=L("level")+u.HealthLevel+" "+u.AircraftHealth.ToString("0")+" "+L("healthStat")+" / "+(u.HealthCost>0?u.HealthCost.ToString("N0")+" G":L("max"));
                lastSpeedLevel=u.SpeedLevel;lastArmorLevel=u.ArmorLevel;lastHealthLevel=u.HealthLevel;
            }
            if(speedUpgradeButton)speedUpgradeButton.interactable=simulation.Upgrades.SpeedCost>0&&simulation.Gold>=simulation.Upgrades.SpeedCost;
            if(armorUpgradeButton)armorUpgradeButton.interactable=simulation.Upgrades.ArmorCost>0&&simulation.Gold>=simulation.Upgrades.ArmorCost;
            if(healthUpgradeButton)healthUpgradeButton.interactable=simulation.Upgrades.HealthCost>0&&simulation.Gold>=simulation.Upgrades.HealthCost;
            if(sortieStatusText)sortieStatusText.text=(sortieCharge.Ready?F("ready",Math.Floor(sortieCharge.Charge)):F("recharge",sortieCharge.Remaining.ToString("0.0")))+" / "+Math.Round(sortieCharge.Charge*100)+"% / "+F("queue",sortieOrders.Count);
            if(nuclearCooldownText)nuclearCooldownText.text=nuclearCooldown.Ready?L("nuclearReady"):F("nuclearCooldown",Math.Ceiling(nuclearCooldown.Remaining));
            if(weaponText)weaponText.text=L(nuclear?"selectedNuke":"selectedBomb");
            if(speedText)speedText.text=L(speed==0?"resume":"pause");
            if(sortieChargeFill)sortieChargeFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,sortieChargeWidth*(float)Math.Min(1,sortieCharge.Charge));
            if(lastDevelopment!=simulation.DevelopmentScore){
                long delta=lastDevelopment<0?0:simulation.DevelopmentScore-lastDevelopment;
                developmentFrom=lastDevelopment<0?simulation.DevelopmentScore:displayedDevelopment;developmentAge=lastDevelopment<0?1:0;
                if(delta>0&&developmentGainText){developmentGainText.text="+"+delta.ToString("N0");gainAge=0;}
                else if(delta<0){gainAge=2;}
                lastDevelopment=simulation.DevelopmentScore;
                if(developmentText)developmentText.text=simulation.DevelopmentScore.ToString("N0");
            }
            if(lastCount!=views.Count || lastRuins!=scars.Count || lastDistricts!=simulation.DistrictCount) {if(cityText)cityText.text=F("city",views.Count,simulation.DistrictCount,scars.Count);lastCount=views.Count;lastRuins=scars.Count;lastDistricts=simulation.DistrictCount;}
            if(statusText)statusText.text=speed==0?L("paused"):dragging?L("release"):simulationTime<feedbackUntil?feedback:scars.Count>0?L("healing"):L("drag");
        }
        public void SelectBomb(){nuclear=false;if(weaponText)weaponText.text=L("selectedBomb");}
        public void BuyBlastRange(){if(simulation==null)return;bool bought=simulation.BuyRadiusUpgrade();feedback=bought?L("upgraded"):L("cannotBuy");feedbackUntil=simulationTime+3;if(bought&&impactAudio)impactAudio.PlayOneShot(goldAudio,.65f);UpdateHud();}
        public void BuyBombCount(){if(simulation==null)return;bool bought=simulation.BuyBombUpgrade();feedback=bought?L("upgraded"):L("cannotBuy");feedbackUntil=simulationTime+3;if(bought&&impactAudio)impactAudio.PlayOneShot(goldAudio,.65f);UpdateHud();}
        public void BuyAircraftLevel(){if(simulation==null)return;bool bought=simulation.BuyAircraftUpgrade();feedback=bought?L("upgraded"):L("cannotBuy");feedbackUntil=simulationTime+3;if(bought&&impactAudio)impactAudio.PlayOneShot(goldAudio,.65f);UpdateHud();}
        public void BuyDropRate(){if(simulation==null)return;bool bought=simulation.BuyDropRateUpgrade();feedback=bought?L("upgraded"):L("cannotBuy");feedbackUntil=simulationTime+3;if(bought&&impactAudio)impactAudio.PlayOneShot(goldAudio,.65f);UpdateHud();}
        public void BuyFormation(){if(simulation==null)return;bool bought=simulation.BuyFormationUpgrade();feedback=bought?L("upgraded"):L("cannotBuy");feedbackUntil=simulationTime+3;if(bought&&impactAudio)impactAudio.PlayOneShot(goldAudio,.65f);UpdateHud();}
        public void BuyAircraftSpeed(){if(simulation==null)return;PurchaseFeedback(simulation.BuySpeedUpgrade(),L("upgraded"));}
        public void BuyAircraftArmor(){if(simulation==null)return;PurchaseFeedback(simulation.BuyArmorUpgrade(),L("upgraded"));}
        public void BuyAircraftHealth(){if(simulation==null)return;PurchaseFeedback(simulation.BuyHealthUpgrade(),L("upgraded"));}
        void PurchaseFeedback(bool bought,string message){feedback=bought?message:L("cannotBuy");feedbackUntil=simulationTime+3;if(bought&&impactAudio)impactAudio.PlayOneShot(goldAudio,.65f);UpdateHud();}
        public void SelectNuke(){nuclear=true;if(weaponText)weaponText.text=L("selectedNuke");}
        public void TogglePause(){CancelDrag();if(speed==0)speed=resumeSpeed;else{resumeSpeed=speed;speed=0;}if(speedText)speedText.text=L(speed==0?"resume":"pause");}
        public void ToggleSpeed(){speed=speed==3?1:3;resumeSpeed=speed;if(speedText)speedText.text=L("pause");feedback=F("pace",speed);feedbackUntil=simulationTime+3;}
        static AudioClip MakeImpact(string name,float seconds,float pitch)
        {
            const int rate=22050;var samples=new float[(int)(seconds*rate)];var noise=new System.Random(42);float lowPass=0;
            for(int i=0;i<samples.Length;i++){float t=i/(float)rate;lowPass=Mathf.Lerp(lowPass,(float)noise.NextDouble()*2-1,.16f);float envelope=Mathf.Exp(-t*7/seconds)*Mathf.Min(1,t*250);samples[i]=Mathf.Clamp((lowPass*.7f+Mathf.Sin(t*pitch*Mathf.PI*2*Mathf.Exp(-t*2))*.3f)*envelope,-1,1);}
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        static AudioClip MakeGoldChime()
        {
            const int rate=22050;var data=new float[(int)(rate*.2f)];for(int i=0;i<data.Length;i++){float t=i/(float)rate;float pitch=t<.075f?1050:1575;data[i]=Mathf.Sin(t*pitch*Mathf.PI*2)*Mathf.Exp(-t*20)*Mathf.Min(1,t*350)*.35f;}
            var clip=AudioClip.Create("Gold collected",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        void OnDestroy(){defense?.Dispose();sortieOrders.Clear();foreach(var run in airstrikes)run.Dispose();airstrikes.Clear();if(roadBatch!=null)roadBatch.Dispose();if(bombAudio)Destroy(bombAudio);if(nukeAudio)Destroy(nukeAudio);if(goldAudio)Destroy(goldAudio);}
    }
}
