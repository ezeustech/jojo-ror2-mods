using RoR2;
using UnityEngine;

namespace LocalStandCombat
{
    // Shared source in the two independent mods; no dependency on JJBA_Core.
    // Position is a world-space chest anchor, independent of the owner's transform.
    internal sealed class StandPrimaryController : MonoBehaviour
    {
        private CharacterBody body;
        private readonly BullseyeSearch search = new BullseyeSearch();
        private HurtBox target;
        private Vector3 approach;
        private Vector3? pointOverride;
        private float travelSpeed, returnStarts, nextSearch, retainUntil;
        private HurtBox retainedTarget;
        private Vector3 previousPosition;
        private Vector3 renderDirection = Vector3.forward;
        private Indicator indicator;
        private HurtBox tracking;
        private bool heldLock, continuous;
        private Vector3 lastTargetPoint;
        private float nextRetarget;
        internal bool Continuous { get { return continuous; } }
        private bool PrimaryHeld { get { return body && body.inputBank && body.inputBank.skill1.down; } }
        internal bool Active { get; private set; }
        internal bool TargetedAction { get; private set; }
        internal bool Returning { get; private set; }
        internal bool Visible { get { return Active || Returning; } }
        internal Vector3 Position { get; private set; }
        internal Vector3 RenderPosition { get { return Vector3.Lerp(previousPosition,Position,Mathf.Clamp01((Time.time-Time.fixedTime)/Mathf.Max(.001f,Time.fixedDeltaTime))); } }
        internal Vector3 Direction { get; private set; }
        internal Vector3 RenderDirection { get { return renderDirection; } }
        internal bool ReturningHome { get { return Returning && Time.time >= returnStarts; } }
        internal HurtBox Target { get { return target; } }

        private void Awake() { body = GetComponent<CharacterBody>(); }
        internal static StandPrimaryController Of(GameObject owner)
        {
            return owner.GetComponent<StandPrimaryController>() ?? owner.AddComponent<StandPrimaryController>();
        }
        internal bool ValidTarget(HurtBox candidate)
        {
            if (!body || !body.healthComponent || !body.healthComponent.alive || !candidate) return false;
            HealthComponent health = candidate.healthComponent;
            if (!health || !health.alive || health == body.healthComponent || !health.body || !health.body.teamComponent || !body.teamComponent) return false;
            TeamIndex team = health.body.teamComponent.teamIndex;
            if (team == body.teamComponent.teamIndex || team == TeamIndex.Neutral || team == TeamIndex.None) return false;
            Vector3 point = candidate.transform.position;
            return StandPrimaryRules.InRange((point - body.corePosition).sqrMagnitude) &&
                !Physics.Linecast(body.corePosition, point, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
        }
        internal HurtBox Acquire(Ray aim)
        {
            if (!body || !body.teamComponent) return null;
            // A held attack owns its target. Looking away is not a release.
            HurtBox retained = Active ? target : Time.time < retainUntil ? retainedTarget : null;
            if (ValidTarget(retained) && ((heldLock && (PrimaryHeld || continuous)) || Vector3.Dot((retained.transform.position - aim.origin).normalized,
                aim.direction.normalized) >= .84f)) return retained;
            bool followUp = heldLock && (PrimaryHeld || continuous);
            TeamMask teams = TeamMask.all;
            teams.RemoveTeam(body.teamComponent.teamIndex);
            teams.RemoveTeam(TeamIndex.Neutral);
            search.teamMaskFilter = teams;
            search.filterByLoS = true;
            search.searchOrigin = followUp ? body.corePosition : aim.origin;
            search.searchDirection = aim.direction;
            search.sortMode = BullseyeSearch.SortMode.Angle;
            search.maxDistanceFilter = StandPrimaryRules.Range + Vector3.Distance(aim.origin, body.corePosition);
            search.maxAngleFilter = followUp ? 180f : StandPrimaryRules.AimAngle;
            search.RefreshCandidates();
            search.FilterOutGameObject(gameObject);
            HurtBox best = null;
            float bestScore = float.PositiveInfinity;
            foreach (HurtBox candidate in search.GetResults()) {
                if (!ValidTarget(candidate)) continue;
                Vector3 point = candidate.transform.position;
                float score = followUp ? (point-lastTargetPoint).sqrMagnitude :
                    (1f-Vector3.Dot((point-aim.origin).normalized,aim.direction.normalized))*400f + (point-body.corePosition).sqrMagnitude*.012f;
                if (score < bestScore) { bestScore=score; best=candidate; }
            }
            return best;
        }
        private Vector3 Forward()
        {
            Vector3 forward = body && body.characterDirection ? body.characterDirection.forward : transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
        }
        private Vector3 Rest()
        {
            Vector3 forward = Forward();
            return body.corePosition + Vector3.Cross(Vector3.up, forward) * .8f - forward * .8f + Vector3.up * .3f;
        }
        internal bool Begin(HurtBox locked, Vector3 aim, float windup)
        {
            TargetedAction = true;
            if (!ValidTarget(locked)) { Release(); return false; }
            if (!Visible) { Position = Rest(); travelSpeed = 0f; }
            previousPosition=Position;
            bool sameTarget = locked == retainedTarget;
            target = locked;
            retainedTarget = locked;
            lastTargetPoint = locked.transform.position;
            heldLock = PrimaryHeld || continuous;
            retainUntil = Time.time + .85f;
            pointOverride = null;
            if (!sameTarget || approach.sqrMagnitude < .001f) {
                approach = locked.transform.position - body.corePosition;
                approach.y = 0f;
                approach = approach.sqrMagnitude > .001f ? approach.normalized : Forward();
            }
            Direction = approach;
            Active = true;
            Returning = false;
            return true;
        }
        internal void SetPoint(Vector3 point) { pointOverride = point; }
        private Vector3 TargetPoint() { return pointOverride.HasValue ? pointOverride.Value : target.transform.position; }
        private Vector3 Destination()
        {
            Vector3 destination = TargetPoint() - approach * StandPrimaryRules.Standoff;
            CharacterBody victim = target ? target.healthComponent.body : null;
            if (!pointOverride.HasValue && victim && victim.characterMotor)
                destination.y = Mathf.Max(destination.y, victim.footPosition.y + 1.4f);
            return destination;
        }
        internal void Tick(float deltaTime)
        {
            if (!Active) return;
            if (!ValidTarget(target)) { Release(); return; }
            lastTargetPoint = target.transform.position;
            Vector3 destination = Destination();
            travelSpeed = Mathf.MoveTowards(travelSpeed, StandPrimaryRules.TravelSpeed(Vector3.Distance(Position, destination)),
                StandPrimaryRules.Acceleration * deltaTime);
            Vector3 wanted = Vector3.MoveTowards(Position, destination, travelSpeed * deltaTime);
            Vector3 delta = wanted - Position;
            RaycastHit hit;
            if (delta.sqrMagnitude > .000001f && Physics.SphereCast(Position, .15f, delta.normalized, out hit,
                delta.magnitude, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                wanted = Position + delta.normalized * Mathf.Max(0f, hit.distance - .03f);
            previousPosition=Position;Position = wanted;
            Vector3 direction = TargetPoint() - Position;
            if (direction.sqrMagnitude > .001f) Direction = direction.normalized;
        }
        internal bool Arrived
        {
            get { return Active && ValidTarget(target) && (Position - Destination()).sqrMagnitude <= .64f; }
        }
        internal bool TryAttackRay(out Ray ray)
        {
            ray = new Ray(Position, Direction);
            return Arrived && !Physics.Linecast(Position, target.transform.position, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
        }
        internal bool CanHit(HealthComponent health)
        {
            if (!health || !health.body) return false;
            HurtBox candidate = target && target.healthComponent == health ? target : health.body.mainHurtBox;
            return Arrived && ValidTarget(candidate) &&
                !Physics.Linecast(Position, candidate.transform.position, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
        }
        internal void Release()
        {
            if (Active) { Returning = true; returnStarts = Time.time + ((PrimaryHeld || continuous) ? StandPrimaryRules.ComboGrace : 0f); retainUntil = Time.time + .85f; }
            Active = false;
            target = null;
            pointOverride = null;
        }
        // Barrages share the same travelling anchor as M1. Empty searches are
        // throttled, and a new target is selected near the last one while held.
        internal void BeginContinuous(Ray aim,bool selectTarget=true)
        {
            continuous=true;
            HurtBox locked=selectTarget?Acquire(aim):null;
            if (locked) Begin(locked,aim.direction,0f);
            nextRetarget=Time.time+.12f;
        }
        internal void TickContinuous(Ray aim,float deltaTime,bool selectTarget=true)
        {
            if (!continuous) return;
            if (!Active || !ValidTarget(target)) {
                Release();
                if (selectTarget && Time.time>=nextRetarget) {
                    nextRetarget=Time.time+.12f;
                    HurtBox locked=Acquire(aim);
                    if (locked) Begin(locked,aim.direction,0f);
                }
            }
            Tick(deltaTime);
        }
        internal void FollowContinuous(HurtBox locked)
        {
            if(!continuous || locked==target)return;
            if(ValidTarget(locked))Begin(locked,Direction,0f);else Release();
        }
        internal void StopContinuous()
        {
            continuous=false; heldLock=false; retainedTarget=null; retainUntil=0;
            Release();returnStarts=Time.time;
        }
        internal void Cancel() { Active = Returning = TargetedAction = continuous = heldLock = false; target = retainedTarget = null; pointOverride = null; travelSpeed = 0f; }
        private bool IsLocal()
        {
            foreach (LocalUser user in LocalUserManager.readOnlyLocalUsersList) if (user.cachedBody == body) return true;
            return false;
        }
        private void FixedUpdate()
        {
            if (!body || !body.healthComponent || !body.healthComponent.alive) { Cancel(); return; }
            if (!PrimaryHeld && !continuous) { heldLock=false; retainedTarget=null; retainUntil=0; }
            else if (heldLock && retainedTarget) retainUntil=Time.time+.85f;
            if (ReturningHome) {
                previousPosition=Position;
                float distance = Vector3.Distance(Position, Rest());
                travelSpeed = Mathf.MoveTowards(travelSpeed, StandPrimaryRules.TravelSpeed(distance) + body.moveSpeed,
                    StandPrimaryRules.Acceleration * Time.fixedDeltaTime);
                Position = Vector3.MoveTowards(Position, Rest(), travelSpeed * Time.fixedDeltaTime);
                Direction = Forward();
                if ((Position - Rest()).sqrMagnitude < .01f) Returning = TargetedAction = false;
            }
            else if (Returning) previousPosition = Position;
        }
        private void Update()
        {
            if (Visible && Direction.sqrMagnitude > .001f)
                renderDirection = Vector3.Lerp(renderDirection, Direction, 1f - Mathf.Exp(-14f * Time.deltaTime)).normalized;
            if (!body || !body.inputBank || !IsLocal()) return;
            if (indicator == null) {
                GameObject prefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/HuntressTrackingIndicator");
                if (prefab) indicator = new Indicator(gameObject, prefab);
            }
            if (Time.time >= nextSearch) {
                nextSearch = Time.time + .1f;
                tracking = Acquire(new Ray(body.inputBank.aimOrigin, body.inputBank.aimDirection));
            }
            if (indicator != null) {
                HurtBox selected = Active ? target : tracking;
                indicator.targetTransform = selected ? selected.transform : null;
                indicator.active = ValidTarget(selected);
            }
        }
        private void OnDisable() { Cancel(); if (indicator != null) indicator.active = false; }
        private void OnDestroy() { if (indicator != null) indicator.active = false; }
    }
}
