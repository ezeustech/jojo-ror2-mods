"""Compose release icons from extracted original game art; retain provenance."""
from pathlib import Path
import json, shutil
from PIL import Image, ImageOps
root=Path(__file__).resolve().parents[1]
records=json.loads((root/'docs/original-ui-textures.json').read_text())
out=root/'assets/release-icons'
def source(i):
    return Image.open(root/records[i]['png']).convert('RGBA')
def fit(im,size):
    box=im.getbbox()
    if box: im=im.crop(box)
    return ImageOps.contain(im,size,Image.Resampling.LANCZOS)
def compose(i, glyph=None, crop=None, count=1):
    im=Image.new('RGBA',(256,256),(20,12,25,255))
    art=source(i)
    if crop: art=art.crop(crop)
    if art.width > art.height*2: art=ImageOps.fit(art,(232,232),Image.Resampling.LANCZOS)
    art=fit(art,(232,232))
    im.alpha_composite(art,((256-art.width)//2,(256-art.height)//2))
    if glyph is not None:
        g=fit(source(glyph),(96,96))
        for n in range(count):
            im.alpha_composite(g,(154-n*38,150-n*28))
    border=source(110).resize((256,256),Image.Resampling.LANCZOS)
    alpha=border.getchannel('A')
    from PIL import ImageDraw
    ImageDraw.Draw(alpha).rectangle((27,27,228,228),fill=0)
    border.putalpha(alpha)
    im.alpha_composite(border)
    return im
maps={
 'Diavolo':{
 'CHAIN':(90,112,None,2),'CHOP':(90,134),'SMASH':(90,37),
 'IMPALE':(90,112),'SKIP':(89,99),'ERASE':(170,48,(0,0,768,192)),
 'EPITAPH':(170,None,(0,192,768,384)),'BLOOD':(89,139),
 'BARRAGE':(90,112,None,3),'COUNTER':(89,132),'GRAB':(90,132),
 'CANCEL':(89,120),'ULTIMATE':(170,134,(0,384,768,576)),
 'BLOCK':(90,132,(150,0,600,450)),'JUMP':(90,99,(0,180,688,688))},
 'Dio':{
 'Combo':(88,112,None,2),'Knives':(87,64),'TimeSkip':(169,99,(0,0,768,192)),
 'TimeStop':(169,48,(0,192,768,384)),'Barrage':(88,112,None,3),
 'BloodDrain':(87,139),'StandJump':(88,99),'RoadRoller':(87,37),
 'ExKnives':(169,64,(0,384,768,576)),'StandBlock':(88,132)}
}
provenance={}
for mod,moves in maps.items():
    folder=out/mod;folder.mkdir(parents=True,exist_ok=True)
    for name,args in moves.items():
        compose(*args).save(folder/(name+'.png'))
        provenance[mod+'/'+name]=[records[i] for i in [args[0],args[1],110] if i is not None]
    compose(95 if mod=='Diavolo' else 94).save(folder/'icon.png')
    compose(168 if mod=='Diavolo' else 167).save(folder/'portrait.png')
    compose(89 if mod=='Diavolo' else 87).save(folder/'card.png')
    for name,i in [('icon',95 if mod=='Diavolo' else 94),('portrait',168 if mod=='Diavolo' else 167),('card',89 if mod=='Diavolo' else 87)]:
        provenance[mod+'/'+name]=[records[i],records[110]]
sheet=Image.new('RGB',(8*160,4*180),'#18131e')
from PIL import ImageDraw
d=ImageDraw.Draw(sheet)
for n,p in enumerate(out.glob('*/*.png')):
    x=n%8*160;y=n//8*180
    sheet.paste(Image.open(p).convert('RGB').resize((144,144)),(x,y))
    d.text((x,y+146),p.parent.name+'/'+p.stem,fill='white')
sheet.save(root/'docs/original-release-icons.jpg')
(root/'docs/release-icon-provenance.json').write_text(json.dumps(provenance,indent=2))
for name in maps['Diavolo']:
    target=root/'UnityProject/Assets/Generated/Icons'/f'{name}.png'
    backup=root/'assets/icon-backup/Diavolo'/target.name
    backup.parent.mkdir(parents=True,exist_ok=True)
    if not backup.exists():shutil.copy2(target,backup)
    shutil.copy2(out/'Diavolo'/target.name,target)
shutil.copy2(out/'Diavolo/portrait.png',root/'UnityProject/Assets/Generated/DiavoloPortrait.png')
shutil.copy2(out/'Diavolo/icon.png',root/'thunderstore/icon.png')
