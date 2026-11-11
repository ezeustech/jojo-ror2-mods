from pathlib import Path
r=Path(__file__).resolve().parents[1]
p=r/'src/TemporalController.cs';s=p.read_text()
a=s.index('            foreach (CharacterBody enemy',s.index('public void MarkFuture'))
b=s.index('            ShowMarks(marks);',a)
s=s[:a]+'''            MapZone[] zones = Object.FindObjectsOfType<MapZone>();
            foreach (CharacterBody enemy in CharacterBody.readOnlyInstancesList) {
                if (!IsEnemy(body, enemy) || enemy.isPlayerControlled || Vector3.Distance(body.corePosition, enemy.corePosition) > MarkRadius) continue;
                Quaternion rotation = enemy.characterDirection ? Quaternion.Euler(0, enemy.characterDirection.yaw, 0) : enemy.transform.rotation;
                Vector3 position;
                if (!AccessibleFate(enemy, zones, out position)) continue;
                marks.Add(new FateMark { Body = enemy, Position = position, Rotation = rotation });
            }
'''+s[b:]
a=s.index('        private Vector3 AccessibleFate');b=s.index('        // Called at attack entry',a)
s=s[:a]+'''        private bool AccessibleFate(CharacterBody enemy, MapZone[] zones, out Vector3 result)
        {
            result = enemy.transform.position;
            float radius, height; FateGrounding.Dimensions(enemy, out radius, out height);
            bool flyer = FateGrounding.IsFlyer(enemy);
            Vector3 originFoot = result - Vector3.up * FateGrounding.RootOffset(enemy);
            if (!flyer && !FatePlacementGeometry.Ground(originFoot, 1.5f, Mathf.Max(24, height * 2), LayerIndex.world.mask, out originFoot)) return false;
            // Independent random samples around THIS enemy, never a ring around Diavolo.
            for (int attempt = 0; attempt < 32; attempt++) {
                Vector2 offset = Random.insideUnitCircle * Mathf.Clamp(radius * 2 + 3, 4, 8);
                if (offset.sqrMagnitude < 1f) continue;
                Vector3 foot = originFoot + new Vector3(offset.x, 0, offset.y);
                if (flyer) {
                    result = enemy.transform.position + new Vector3(offset.x, Random.Range(-1f, 1f), offset.y);
                    Vector3 motion = result - enemy.transform.position;
                    if (Physics.CheckSphere(result, radius, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) || Physics.SphereCast(enemy.corePosition, radius, motion.normalized, motion.magnitude, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) continue;
                } else {
                    if (!FatePlacementGeometry.Ground(foot, 1.5f, 2.5f, LayerIndex.world.mask, out foot) || !FatePlacementGeometry.Fits(foot, radius, height, LayerIndex.world.mask) || !FatePlacementGeometry.Connected(originFoot, foot, radius, height, LayerIndex.world.mask)) continue;
                    result = foot + Vector3.up * FateGrounding.RootOffset(enemy);
                }
                if (!InsideMap(zones, enemy.corePosition, result, radius, height)) continue;
                bool occupied = false;
                foreach (FateMark mark in marks) {
                    float otherRadius, otherHeight; FateGrounding.Dimensions(mark.Body, out otherRadius, out otherHeight);
                    if (Vector3.Distance(mark.Position, result) < radius + otherRadius + 2f) { occupied = true; break; }
                }
                if (!occupied) return true;
            }
            // Omitting a mark is safer than teleporting to an unchecked fallback.
            return false;
        }

        private static bool InsideMap(MapZone[] zones, Vector3 original, Vector3 root, float radius, float height)
        {
            foreach (MapZone zone in zones) {
                if (!zone || !zone.isActiveAndEnabled || (zone.zoneType != MapZone.ZoneType.OutOfBounds && zone.zoneType != MapZone.ZoneType.KillEverything)) continue;
                for (int i = 0; i < 5; i++) {
                    Vector3 point = root + (i == 0 ? Vector3.zero : Quaternion.Euler(0, (i-1)*90, 0)*Vector3.forward*radius);
                    bool inside = zone.IsPointInsideMapZone(point);
                    if (zone.triggerType == MapZone.TriggerType.TriggerEnter && inside) return false;
                    if (zone.triggerType == MapZone.TriggerType.TriggerExit && zone.IsPointInsideMapZone(original) && !inside) return false;
                }
            }
            return true;
        }

        private static bool GroundFate(CharacterBody enemy, Vector3 desired, out Vector3 result)
        {
            result = desired;
            float radius, height; FateGrounding.Dimensions(enemy, out radius, out height);
            Vector3 foot;
            if (!FatePlacementGeometry.Ground(desired - Vector3.up * FateGrounding.RootOffset(enemy), .5f, 1f, LayerIndex.world.mask, out foot) || !FatePlacementGeometry.Fits(foot, radius, height, LayerIndex.world.mask)) return false;
            result = foot + Vector3.up * FateGrounding.RootOffset(enemy);
            return true;
        }

'''+s[b:]
s=s.replace('TeleportHelper.TeleportGameObject(mark.Body.gameObject, resolved);','TeleportHelper.TeleportGameObject(mark.Body.gameObject, resolved, mark.Rotation, true);\n                    if(mark.Body.characterMotor && mark.Body.characterMotor.Motor)mark.Body.characterMotor.Motor.SetPositionAndRotation(resolved,mark.Rotation);')
p.write_text(s)
