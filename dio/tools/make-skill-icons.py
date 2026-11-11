"""Draw a small, editable geometric icon family matching the DIO package mark."""
from pathlib import Path
import math
import shutil
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
out = root / 'UnityProject/Assets/UI'
out.mkdir(parents=True, exist_ok=True)
S = 4
gold, light, dark, purple = '#e4b94c', '#fff0b1', '#161221', '#9470bc'
def base():
    image = Image.new('RGB', (256*S,256*S), dark)
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((8*S,8*S,248*S,248*S),radius=26*S,fill='#231a35',outline=gold,width=5*S)
    draw.ellipse((25*S,25*S,231*S,231*S),outline='#483359',width=2*S)
    return image, draw
def poly(draw, points, fill, outline=dark, width=4):
    draw.polygon([(int(x*S),int(y*S)) for x,y in points], fill=fill)
    if outline: draw.line([(int(x*S),int(y*S)) for x,y in points+[points[0]]],fill=outline,width=width*S,joint='curve')
def line(draw, pts, color, width=5):
    draw.line([(int(x*S),int(y*S)) for x,y in pts],fill=color,width=width*S,joint='curve')
def ellipse(draw, box, fill=None, outline=None, width=4):
    draw.ellipse(tuple(int(v*S) for v in box),fill=fill,outline=outline,width=width*S)
def fist(draw, cx,cy,scale=1,color=gold):
    shape=[(-49,20),(-52,-21),(-42,-39),(-23,-43),(-6,-38),(12,-42),(29,-31),(44,-29),(53,-10),(44,21),(28,36),(29,61),(-26,61),(-30,39)]
    poly(draw,[(cx+x*scale,cy+y*scale) for x,y in shape],color)
    for x in [-25,-6,14]:line(draw,[(cx+x*scale,cy-32*scale),(cx+(x-4)*scale,cy-9*scale)],dark,max(2,int(4*scale)))
    line(draw,[(cx-40*scale,cy+10*scale),(cx-12*scale,cy-5*scale),(cx+18*scale,cy+5*scale)],light,max(2,int(4*scale)))
    line(draw,[(cx-22*scale,cy+43*scale),(cx+23*scale,cy+43*scale)],dark,3)
def clock(draw,center=(128,128),radius=79):
    x,y=center;ellipse(draw,(x-radius,y-radius,x+radius,y+radius),fill='#24192f',outline=gold,width=7)
    for i in range(12):
        a=i*math.tau/12
        line(draw,[(x+math.sin(a)*(radius-10),y-math.cos(a)*(radius-10)),(x+math.sin(a)*(radius-20),y-math.cos(a)*(radius-20))],light,3)
def save(name,image):
    image.resize((256,256),Image.Resampling.LANCZOS).save(out/(name+'.png'))

im,d=base()
for a in [-140,-110,-75,-40,5,40]:
    r=math.radians(a);line(d,[(128+72*math.cos(r),113+72*math.sin(r)),(128+103*math.cos(r),113+103*math.sin(r))],light,6)
fist(d,126,120,1.2);save('Combo',im)

im,d=base()
for cx,cy,angle in [(71,139,-25),(129,112,0),(185,135,25)]:
    a=math.radians(angle)
    def transform(points):return [(cx+x*math.cos(a)-y*math.sin(a),cy+x*math.sin(a)+y*math.cos(a)) for x,y in points]
    poly(d,transform([(0,-78),(-15,-17),(-11,15),(11,15),(15,-17)]),light)
    line(d,transform([(0,-62),(0,13)]),'#b5c2d5',3)
    poly(d,transform([(-22,15),(22,15),(22,23),(-22,23)]),gold)
    poly(d,transform([(-8,24),(8,24),(8,64),(-8,64)]),purple)
save('Knives',im)

im,d=base()
ellipse(d,(45,39,205,209),outline=purple,width=7)
for x,color in [(73,'#69517e'),(177,light)]:
    ellipse(d,(x-14,69,x+14,97),fill=color)
    poly(d,[(x-14,103),(x+12,103),(x+19,137),(x+10,149),(x+19,184),(x+7,189),(x-6,153),(x-17,186),(x-28,181),(x-17,138)],color,None)
line(d,[(94,126),(147,126)],gold,10)
poly(d,[(142,109),(162,126),(142,143)],gold,None)
for y in [85,163,184]:line(d,[(34,y),(61,y)],purple,5)
save('TimeSkip',im)

im,d=base();clock(d)
poly(d,[(102,91),(119,91),(119,162),(102,162)],light,None)
poly(d,[(137,91),(154,91),(154,162),(137,162)],light,None)
poly(d,[(161,37),(171,23),(180,37),(194,44),(180,52),(171,65),(162,51),(149,44)],gold,None)
save('TimeStop',im)

im,d=base()
fist(d,75,86,.67,'#8a689f');fist(d,178,89,.67,'#bd8f50')
for x in [52,89,169,205]:line(d,[(x,157),(x-14,198)],purple,5)
fist(d,127,143,.98);save('Barrage',im)

im,d=base()
poly(d,[(42,181),(55,126),(70,88),(80,93),(70,132),(87,69),(99,72),(88,128),(108,59),(120,63),(108,132),(132,80),(143,86),(128,147),(150,128),(162,138),(130,182),(114,211),(60,211)],gold)
poly(d,[(185,83),(160,121),(154,141),(162,159),(182,166),(201,159),(211,143),(207,123)],'#d64567',light,3)
line(d,[(181,119),(172,140),(175,148)],'#ffd8cf',4)
save('BloodDrain',im)

im,d=base()
# Armored legs kicking away from fractured ground, with an upward arrow.
for dx in [-38,38]:
    poly(d,[(105+dx,57),(138+dx,57),(133+dx,110),(143+dx,151),(164+dx,164),(160+dx,180),(116+dx,180),(109+dx,120)],gold)
    line(d,[(115+dx,75),(126+dx,75),(122+dx,105)],light,5)
poly(d,[(117,52),(117,89),(139,89),(139,52),(155,52),(128,26),(101,52)],light,None)
line(d,[(36,209),(81,198),(108,218),(127,193),(149,218),(184,199),(221,210)],purple,6)
for x in [51,93,166,207]:line(d,[(x,187),(x-7,169)],light,4)
save('StandJump',im)

im,d=base()
ellipse(d,(39,139,113,213),fill=purple,outline=light,width=5)
ellipse(d,(157,145,211,201),fill=purple,outline=light,width=5)
poly(d,[(47,139),(82,110),(132,110),(145,132),(207,132),(216,164),(174,164),(151,188),(107,188),(98,142)],gold)
poly(d,[(93,107),(93,71),(153,71),(158,117),(137,117),(131,87),(108,87),(108,107)],light)
line(d,[(51,57),(51,105)],light,7)
poly(d,[(35,93),(51,113),(67,93)],light,None)
save('RoadRoller',im)
im,d=base()
ellipse(d,(99,100,157,166),fill=purple)
for i in range(8):
 a=i*math.tau/8; cx=128+81*math.cos(a); cy=130+81*math.sin(a)
 tip=(128+45*math.cos(a),130+45*math.sin(a))
 poly(d,[tip,(cx-9*math.sin(a),cy+9*math.cos(a)),(cx+9*math.sin(a),cy-9*math.cos(a))],light,None)
line(d,[(49,65),(105,36),(165,45)],gold,5)
save('ExKnives',im)

im,d=base()
poly(d,[(128,34),(207,69),(196,151),(170,192),(128,222),(86,192),(60,151),(49,69)],purple,light,5)
fist(d,100,112,.55,gold);fist(d,160,112,.55,light)
line(d,[(68,172),(128,194),(188,172)],gold,5)
save('StandBlock',im)

# Original EOH portrait textures are copied without repainting the source art.
shutil.copyfile(root/'assets/ui-source/face_player_3dio01_0.png',out/'DioPortrait.png')
shutil.copyfile(root/'assets/ui-source/charaicon_face_card_3dio01_0.png',out/'DioCard.png')
sheet=Image.new('RGB',(256*10,288),'#161221')
for i,name in enumerate(['Combo','Knives','TimeSkip','TimeStop','Barrage','BloodDrain','StandJump','RoadRoller','ExKnives','StandBlock']):
    sheet.paste(Image.open(out/(name+'.png')),(i*256,0))
    ImageDraw.Draw(sheet).text((i*256+12,267),name,fill='white')
sheet.save(root/'docs/dio-skill-icons.png')
print('Wrote ten skill icons and two original EOH portraits.')
