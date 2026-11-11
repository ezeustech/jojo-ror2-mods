import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
rows = {'5dvl01': [], '5kcr01': []}
for line in (root/'docs/unity-assets.txt').read_text().splitlines():
    match = re.fullmatch(r'(5dvl01|5kcr01)bod1\|\1_(\S+) (\S+)', line)
    if match:
        code, clip, duration = match.groups()
        rows[code].append((clip, float(duration)))

phases = {'st':'start', 'lp':'loop', 'ed':'end', 'hit':'hit/reaction', 'str':'action phase (exact meaning unconfirmed)', 'nut':'neutral', 'atk':'attack', 'air_atk':'aerial attack', 'walk':'walk', 'run':'run', 'jump':'jump', 'jump_lp':'airborne loop', 'nut_jump':'jump transition', 'land':'landing'}
def describe(code, clip):
    if clip.startswith('atk_'):
        name=clip[4:]
        if name=='charge': return 'Charge attack clip; original named move not verified.'
        if name=='dash': return 'Dash attack clip; original named move not verified.'
        air='Aerial ' if name.startswith('air_') else 'Ground '
        name=name.removeprefix('air_')
        result=air+('light' if name[0]=='l' else 'heavy')+' attack '+name[1:]+'. Naming-based family.'
        if code=='5kcr01':
            result += {'h1':' Inspected: lunging straight punch. Current mod: impale and grab punch.', 'h2':' Inspected: overhead downward strike. Current mod: chop.', 'h3':' Inspected: turning follow-through strike.', 'l1':' Inspected: rising/forward arm strike.', 'l2':' Inspected: forward punch; current barrage alternation.', 'l3':' Inspected: opposite-side follow-up punch; current barrage alternation.', 'l4':' Inspected: rising/turning finisher.'}.get(clip[4:],'')
        else:
            result += ' Diavolo actor motion, not necessarily a punch by Diavolo himself.'
        return result
    if clip.startswith('spa_'):
        if clip=='spa_finish': return 'Special finish clip; exact named EOH skill/cinematic unverified.'
        parts=clip.split('_'); return 'Special sequence '+parts[1]+': '+phases.get('_'.join(parts[2:]),'_'.join(parts[2:]).replace('_',' '))+'. Exact named EOH skill unverified; sequence numbers are not move names.'
    if clip.startswith('gmc_'):
        parts=clip.split('_'); return 'GMC sequence '+parts[1]+': '+phases.get('_'.join(parts[2:]),'base action')+'. Internal mode/gimmick sequence; exact gameplay use unverified.'
    if clip=='sty_01': return 'Style/action 01; exact EOH move unverified.'
    if clip.startswith('bas_jojo_'):
        parts=clip.split('_'); return 'JoJo pose '+parts[2]+': '+phases.get(parts[3],parts[3])+'. Current counter uses pose 02 loop.'
    if clip.startswith('bas_s_burst_01_'): return 'Stand burst sequence 01: '+phases[clip.rsplit('_',1)[1]]+'. Exact gameplay trigger unverified.'
    basics={'nut':'standing idle','s_nut':'stand idle','dash_st':'dash start','dash_lp':'dash loop','dash_ed':'dash stop','step_front':'forward step','step_back':'backward step','step_left':'left step','step_right':'right step','jump':'jump','jump_lp':'airborne loop','nut_jump':'standing-to-jump transition','land':'landing','run_front':'forward run','run_front_st':'run start','walk_front':'forward walk','air_recovery':'air recovery','recovery':'recovery/get-up','down':'going down','down_nut':'downed idle'}
    if clip.startswith('bas_'): return basics.get(clip[4:],clip[4:]).capitalize()+'. Naming-based interpretation.'
    if clip.startswith('grd_'):
        name=clip[4:].removeprefix('s_')
        if name=='crash': return 'Guard break reaction (naming-based).'
        group, phase=name.split('_',1)
        return ('Aerial' if group=='air' else 'Standing')+' guard '+phases.get(phase,phase)+' (naming-based).'
    reactions={'air':'air hit','air_lp':'air-hit loop','back':'hit from behind','batsu':'special damage reaction; exact meaning unverified','bind':'restrained/bound reaction','blow_front':'forward knockback','blow_front_lp':'forward knockback loop','blow_up':'upward launch','blow_up_lp':'upward launch loop','body':'body hit','bound':'bounce reaction','down':'downed damage reaction','front_l':'front hit, left variant','front_r':'front hit, right variant','land':'impact landing','neck':'neck hit','rush':'rapid-hit reaction','slow':'slow/stagger reaction','spin':'spinning knockback','spin_lp':'spinning knockback loop','wall_st':'wall impact start','wall_lp':'wall-impact loop','wobble_back':'backward stagger','wobble_front':'forward stagger'}
    if clip.startswith('dmg_'): return reactions.get(clip[4:].removeprefix('s_'),clip[4:]).capitalize()+' (naming-based; victim reaction, not an attack).'
    faces={'atk_h_01':'heavy-attack expression','atk_l_01':'light-attack expression','atk_m_01':'medium-attack expression','dead_01':'death expression','dmg_h_01':'heavy-damage expression','dmg_l_01':'light-damage expression','nut_01':'neutral expression','odoroki_01':'surprised expression','weak_01':'weakened expression'}
    if clip.startswith('fce_'): return faces.get(clip[4:],clip[4:]).capitalize()+' (naming-based facial animation).'
    if clip.startswith('lip_'): return 'Lip-sync mouth shape '+clip.rsplit('_',1)[1]+', '+clip.split('_')[1]+' set. Exact no/up distinction unverified; not a combat move.'
    if clip=='stg_hang_03': return 'Stage hanging animation, variant 03 (naming-based).'
    return 'Unmapped; requires inspection.'

out=['# Extracted Eyes of Heaven animation catalog','', 'Source: the local mot1/mot2 conversion and Unity import report. This lists all 172 clips currently imported: 115 Diavolo, 57 King Crimson. It is not a claim that these banks contain every cinematic in the entire game.', '', 'Full imported names have the form `5dvl01bod1|5dvl01_<clip>` or `5kcr01bod1|5kcr01_<clip>`. Durations are source clip lengths before mod playback speed adjustments.', '', 'Mappings distinguish inspected motion, current mod use, and naming-based inference. The special/GMC sequence numbers have not been matched to official EOH move titles. Reusing a clip for impale, barrage, blood throw, or counter in this mod does not establish its original EOH move identity.', '']
for code,title in [('5dvl01','Diavolo'),('5kcr01','King Crimson')]:
    out += ['## '+title+' — '+str(len(rows[code]))+' clips','', '| Clip suffix | Seconds | Motion / move correspondence |', '|---|---:|---|']
    out += [f'| `{clip}` | {duration:.3f} | {describe(code,clip)} |' for clip,duration in sorted(rows[code])]
    out += ['']
(root/'docs/animation-catalog.md').write_text('\n'.join(out),encoding='utf-8')
assert len(rows['5dvl01'])==115 and len(rows['5kcr01'])==57
print('Catalog written: all 172 imported clips, with durations and mapping confidence.')
